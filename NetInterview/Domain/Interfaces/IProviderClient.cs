namespace Domain.Interfaces;

public enum ProviderOutcome { Success = 1, TransientFailure = 2 }

public record ProviderResult(ProviderOutcome Outcome, string? Error = null)
{
  public static ProviderResult Success() => new(ProviderOutcome.Success);
  public static ProviderResult TransientFailure(string error) => new(ProviderOutcome.TransientFailure, error);
}

public record ProviderRequest(Guid IdempotencyKey, Guid CustomerId, int ServiceCode, string Description);

/// <summary>
/// Proveedor externo. <see cref="ProviderRequest.IdempotencyKey"/> (el externalReference) garantiza
/// que una misma operación enviada varias veces produzca un único efecto.
/// </summary>
public interface IProviderClient
{
  Task<ProviderResult> ProcessAsync(ProviderRequest request, CancellationToken ct = default);
}
