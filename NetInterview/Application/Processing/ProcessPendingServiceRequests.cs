using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Processing;

public record ProcessingSummary(int Released, int Completed, int Retried, int Failed, int Skipped);

/// <summary>
/// Caso de uso del worker: libera procesamientos abandonados, toma solicitudes Pending,
/// invoca al proveedor y registra el resultado. Toda regla de estado vive en la entidad.
/// </summary>
public class ProcessPendingServiceRequests(
  IServiceRequestRepository repository,
  IUnitOfWork unitOfWork,
  IProviderClient providerClient,
  ProcessingOptions options,
  TimeProvider timeProvider,
  ILogger<ProcessPendingServiceRequests> logger)
{
  public async Task<ProcessingSummary> ExecuteAsync(CancellationToken ct = default)
  {
    var released = await ReleaseExpiredLeasesAsync(ct);
    int completed = 0, retried = 0, failed = 0, skipped = 0;

    var pending = await repository.GetPendingAsync(options.BatchSize, ct);
    foreach (var serviceRequest in pending)
    {
      ct.ThrowIfCancellationRequested();

      if (!await TryClaimAsync(serviceRequest, ct))
      {
        skipped++;
        continue;
      }

      var result = await CallProviderAsync(serviceRequest, ct);
      var now = timeProvider.GetUtcNow();
      if (result.Outcome == ProviderOutcome.Success)
        serviceRequest.Complete(now);
      else
        serviceRequest.RegisterTransientFailure(result.Error ?? "Fallo transitorio del proveedor.", options.MaxAttempts, now);

      // Si este guardado falla, la solicitud queda en Processing y se recupera al vencer el lease;
      // el reintento no duplica el efecto porque el proveedor es idempotente por externalReference.
      try
      {
        await unitOfWork.SaveChangesAsync(ct);
      }
      catch (ConcurrencyConflictException)
      {
        logger.LogWarning("Solicitud {ExternalReference} modificada por otro proceso durante el procesamiento.", serviceRequest.ExternalReference);
        skipped++;
        continue;
      }

      switch (serviceRequest.Status)
      {
        case ServiceRequestStatus.Completed: completed++; break;
        case ServiceRequestStatus.Failed: failed++; break;
        default: retried++; break;
      }
      logger.LogInformation(
        "Solicitud {ExternalReference} -> {Status} (intento {Attempt}/{MaxAttempts})",
        serviceRequest.ExternalReference, serviceRequest.Status, serviceRequest.AttemptCount, options.MaxAttempts);
    }

    return new ProcessingSummary(released, completed, retried, failed, skipped);
  }

  /// <summary>
  /// Pending → Processing protegido por concurrencia optimista: si otra instancia del worker
  /// la tomó primero, el guardado falla y esta instancia la omite sin llamar al proveedor.
  /// </summary>
  private async Task<bool> TryClaimAsync(ServiceRequest serviceRequest, CancellationToken ct)
  {
    serviceRequest.StartProcessing(timeProvider.GetUtcNow());
    try
    {
      await unitOfWork.SaveChangesAsync(ct);
      return true;
    }
    catch (ConcurrencyConflictException)
    {
      logger.LogInformation("Solicitud {ExternalReference} tomada por otro proceso; se omite.", serviceRequest.ExternalReference);
      return false;
    }
  }

  private async Task<ProviderResult> CallProviderAsync(ServiceRequest serviceRequest, CancellationToken ct)
  {
    var request = new ProviderRequest(
      serviceRequest.ExternalReference, serviceRequest.CustomerId, serviceRequest.ServiceCode, serviceRequest.Description);
    try
    {
      return await providerClient.ProcessAsync(request, ct);
    }
    catch (Exception ex) when (!ct.IsCancellationRequested)
    {
      // Timeout o error de red: no sabemos si el proveedor procesó; se trata como transitorio
      // y el reintento es seguro gracias a la clave de idempotencia.
      logger.LogWarning(ex, "Error llamando al proveedor para {ExternalReference}", serviceRequest.ExternalReference);
      return ProviderResult.TransientFailure($"Error de comunicación con el proveedor: {ex.Message}");
    }
  }

  private async Task<int> ReleaseExpiredLeasesAsync(CancellationToken ct)
  {
    var now = timeProvider.GetUtcNow();
    var stale = await repository.GetProcessingStartedBeforeAsync(now - options.LeaseTimeout, ct);
    var released = 0;
    foreach (var serviceRequest in stale)
    {
      serviceRequest.ReleaseExpiredLease(options.MaxAttempts, now, options.LeaseTimeout);
      try
      {
        await unitOfWork.SaveChangesAsync(ct);
        released++;
        logger.LogWarning("Solicitud {ExternalReference} liberada por lease vencido -> {Status}", serviceRequest.ExternalReference, serviceRequest.Status);
      }
      catch (ConcurrencyConflictException)
      {
        // Otra instancia ya la liberó o la terminó.
      }
    }
    return released;
  }
}
