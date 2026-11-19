using Domain.Interfaces;

namespace Tests.Fakes;

public enum ScriptedResponse { Success, TransientFailure, ApplyThenTimeout }

/// <summary>
/// Proveedor determinista: responde según un guion y, como un proveedor real idempotente,
/// aplica el efecto una sola vez por clave aunque reciba la misma operación varias veces.
/// </summary>
public class FakeProviderClient(params ScriptedResponse[] script) : IProviderClient
{
  private readonly Queue<ScriptedResponse> _script = new(script);
  private readonly Dictionary<Guid, int> _effects = [];

  public List<Guid> Calls { get; } = [];

  public int EffectsFor(Guid key) => _effects.GetValueOrDefault(key);

  /// <summary>Simula que el proveedor ya aplicó el efecto en una llamada anterior.</summary>
  public void MarkAlreadyProcessed(Guid key) => _effects[key] = 1;

  public Task<ProviderResult> ProcessAsync(ProviderRequest request, CancellationToken ct = default)
  {
    Calls.Add(request.IdempotencyKey);

    if (_effects.ContainsKey(request.IdempotencyKey))
      return Task.FromResult(ProviderResult.Success());

    var response = _script.Count > 0 ? _script.Dequeue() : ScriptedResponse.Success;
    switch (response)
    {
      case ScriptedResponse.TransientFailure:
        return Task.FromResult(ProviderResult.TransientFailure("caído"));
      case ScriptedResponse.ApplyThenTimeout:
        _effects[request.IdempotencyKey] = 1;
        throw new TimeoutException("Sin respuesta del proveedor");
      default:
        _effects[request.IdempotencyKey] = 1;
        return Task.FromResult(ProviderResult.Success());
    }
  }
}
