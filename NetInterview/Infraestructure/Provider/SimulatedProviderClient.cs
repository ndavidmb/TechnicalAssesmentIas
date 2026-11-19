using System.Collections.Concurrent;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Provider;

public class SimulatedProviderOptions
{
  public const string SectionName = "SimulatedProvider";

  /// <summary>Probabilidad (0..1) de responder un fallo transitorio en una operación nueva.</summary>
  public double TransientFailureRate { get; set; } = 0.3;

  /// <summary>Latencia simulada de cada llamada.</summary>
  public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(300);
}

/// <summary>
/// Proveedor externo simulado. Recuerda las claves de idempotencia ya procesadas con éxito y,
/// ante una repetida, devuelve el mismo resultado sin volver a producir el efecto.
/// Se registra como singleton: la memoria vive mientras viva el proceso (ver supuestos en README).
/// </summary>
public class SimulatedProviderClient(SimulatedProviderOptions options, ILogger<SimulatedProviderClient> logger) : IProviderClient
{
  private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processed = new();

  public async Task<ProviderResult> ProcessAsync(ProviderRequest request, CancellationToken ct = default)
  {
    await Task.Delay(options.Latency, ct);

    if (_processed.ContainsKey(request.IdempotencyKey))
    {
      logger.LogInformation("Proveedor: {Key} ya procesada, se devuelve el resultado previo sin repetir el efecto.", request.IdempotencyKey);
      return ProviderResult.Success();
    }

    if (Random.Shared.NextDouble() < options.TransientFailureRate)
      return ProviderResult.TransientFailure("Proveedor no disponible temporalmente (simulado).");

    if (_processed.TryAdd(request.IdempotencyKey, DateTimeOffset.UtcNow))
      logger.LogInformation("Proveedor: efecto aplicado para {Key} (servicio {ServiceCode}).", request.IdempotencyKey, request.ServiceCode);

    return ProviderResult.Success();
  }
}
