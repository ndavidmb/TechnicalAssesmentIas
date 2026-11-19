using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IServiceRequestRepository
{
  void CreateServiceRequest(ServiceRequest serviceRequest);

  Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task<ServiceRequest?> GetByExternalReferenceAsync(Guid externalReference, CancellationToken ct = default);
  Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);

  /// <summary>Solicitudes Pending más antiguas primero, como entidades rastreadas para poder modificarlas.</summary>
  Task<List<ServiceRequest>> GetPendingAsync(int take, CancellationToken ct = default);

  /// <summary>Solicitudes en Processing cuyo procesamiento empezó antes de <paramref name="startedBefore"/>.</summary>
  Task<List<ServiceRequest>> GetProcessingStartedBeforeAsync(DateTimeOffset startedBefore, CancellationToken ct = default);
}
