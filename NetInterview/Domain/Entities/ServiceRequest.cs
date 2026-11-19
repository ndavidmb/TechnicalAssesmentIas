using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class ServiceRequest
{
  public const int DescriptionMaxLength = 500;
  public const int LastErrorMaxLength = 1000;

  public Guid Id { get; private set; }
  public Guid ExternalReference { get; private set; }
  public Guid CustomerId { get; private set; }
  public int ServiceCode { get; private set; }
  public string Description { get; private set; }
  public ServiceRequestStatus Status { get; private set; }
  public int AttemptCount { get; private set; }
  public string? LastError { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset UpdatedAt { get; private set; }
  public DateTimeOffset? ProcessingStartedAt { get; private set; }

  private ServiceRequest()
  {
    Id = Guid.NewGuid();
    Description = String.Empty;
  }

  public static ServiceRequest Create(Guid externalReference, Guid customerId, string description, DateTimeOffset now)
  {
    if (externalReference == Guid.Empty)
      throw new DomainValidationException("externalReference es requerido y debe ser un GUID distinto de vacío.");
    if (customerId == Guid.Empty)
      throw new DomainValidationException("customerId es requerido y debe ser un GUID distinto de vacío.");

    var normalizedDescription = description?.Trim() ?? string.Empty;
    if (normalizedDescription.Length == 0)
      throw new DomainValidationException("description es requerida.");
    if (normalizedDescription.Length > DescriptionMaxLength)
      throw new DomainValidationException($"description no puede superar {DescriptionMaxLength} caracteres.");

    return new ServiceRequest()
    {
      ExternalReference = externalReference,
      CustomerId = customerId,
      Description = normalizedDescription,
      Status = ServiceRequestStatus.Pending,
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  /// <summary>
  /// Un reintento de la misma operación de negocio debe traer los mismos datos.
  /// Se usa para decidir entre devolver la solicitud existente o responder conflicto.
  /// </summary>
  public bool HasSamePayload(Guid customerId, string description) =>
    CustomerId == customerId && Description == (description?.Trim() ?? string.Empty);

  public void StartProcessing(DateTimeOffset now)
  {
    EnsureNotCompleted();
    if (Status != ServiceRequestStatus.Pending)
      throw new InvalidStatusTransitionException($"Solo una solicitud Pending puede pasar a Processing (actual: {Status}).");

    Status = ServiceRequestStatus.Processing;
    AttemptCount++;
    ProcessingStartedAt = now;
    UpdatedAt = now;
  }

  public void Complete(DateTimeOffset now)
  {
    EnsureNotCompleted();
    if (Status != ServiceRequestStatus.Processing)
      throw new InvalidStatusTransitionException($"Solo una solicitud Processing puede pasar a Completed (actual: {Status}).");

    Status = ServiceRequestStatus.Completed;
    LastError = null;
    ProcessingStartedAt = null;
    UpdatedAt = now;
  }

  /// <summary>
  /// Fallo transitorio del proveedor: vuelve a Pending para reintentarse o,
  /// si ya agotó los intentos, queda en Failed (estado final).
  /// </summary>
  public void RegisterTransientFailure(string error, int maxAttempts, DateTimeOffset now)
  {
    EnsureNotCompleted();
    if (Status != ServiceRequestStatus.Processing)
      throw new InvalidStatusTransitionException($"Solo una solicitud Processing puede registrar un fallo (actual: {Status}).");

    LastError = Truncate(error, LastErrorMaxLength);
    Status = AttemptCount >= maxAttempts ? ServiceRequestStatus.Failed : ServiceRequestStatus.Pending;
    ProcessingStartedAt = null;
    UpdatedAt = now;
  }

  public bool IsProcessingLeaseExpired(DateTimeOffset now, TimeSpan leaseTimeout) =>
    Status == ServiceRequestStatus.Processing
    && ProcessingStartedAt is not null
    && ProcessingStartedAt.Value + leaseTimeout <= now;

  /// <summary>
  /// El proceso que la tomó se cayó sin terminar. Se cuenta como intento fallido;
  /// reintentarla es seguro porque el proveedor recibe el externalReference como clave de idempotencia.
  /// </summary>
  public void ReleaseExpiredLease(int maxAttempts, DateTimeOffset now, TimeSpan leaseTimeout)
  {
    if (!IsProcessingLeaseExpired(now, leaseTimeout))
      throw new InvalidStatusTransitionException("La solicitud no tiene un procesamiento vencido que liberar.");

    RegisterTransientFailure("El procesamiento anterior no terminó (lease vencido).", maxAttempts, now);
  }

  private void EnsureNotCompleted()
  {
    if (Status == ServiceRequestStatus.Completed)
      throw new InvalidStatusTransitionException("Una solicitud Completed no puede cambiar de estado.");
  }

  private static string Truncate(string value, int maxLength) =>
    value.Length <= maxLength ? value : value[..maxLength];
}
