using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Tests.Fakes;

/// <summary>
/// Repositorio + unidad de trabajo en memoria. Las altas quedan pendientes hasta SaveChanges,
/// y <see cref="BeforeSave"/> permite simular fallos de la base (concurrencia, índice único).
/// </summary>
public class InMemoryServiceRequestStore : IServiceRequestRepository, IUnitOfWork
{
  private readonly List<ServiceRequest> _stored = [];
  private readonly List<ServiceRequest> _added = [];

  public IReadOnlyList<ServiceRequest> Stored => _stored;
  public int SaveCount { get; private set; }

  /// <summary>Se ejecuta antes de confirmar; si lanza, las altas pendientes se descartan.</summary>
  public Action<int>? BeforeSave { get; set; }

  public void Seed(params ServiceRequest[] requests) => _stored.AddRange(requests);

  public void CreateServiceRequest(ServiceRequest serviceRequest) => _added.Add(serviceRequest);

  public Task<int> SaveChangesAsync(CancellationToken ct = default)
  {
    SaveCount++;
    try
    {
      BeforeSave?.Invoke(SaveCount);
    }
    catch
    {
      _added.Clear();
      throw;
    }
    var count = _added.Count;
    _stored.AddRange(_added);
    _added.Clear();
    return Task.FromResult(count);
  }

  public Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
    Task.FromResult(_stored.FirstOrDefault(s => s.Id == id));

  public Task<ServiceRequest?> GetByExternalReferenceAsync(Guid externalReference, CancellationToken ct = default) =>
    Task.FromResult(_stored.FirstOrDefault(s => s.ExternalReference == externalReference));

  public Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default) =>
    Task.FromResult(_stored.Where(s => status is null || s.Status == status).ToList());

  public Task<List<ServiceRequest>> GetPendingAsync(int take, CancellationToken ct = default) =>
    Task.FromResult(_stored.Where(s => s.Status == ServiceRequestStatus.Pending).OrderBy(s => s.CreatedAt).Take(take).ToList());

  public Task<List<ServiceRequest>> GetProcessingStartedBeforeAsync(DateTimeOffset startedBefore, CancellationToken ct = default) =>
    Task.FromResult(_stored.Where(s => s.Status == ServiceRequestStatus.Processing && s.ProcessingStartedAt <= startedBefore).ToList());
}
