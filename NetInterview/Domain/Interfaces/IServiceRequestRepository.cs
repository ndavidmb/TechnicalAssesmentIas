using Domain.Entities;

namespace Domain.Interfaces;

public interface IServiceRequestRepository
{
  void CreateServiceRequest(ServiceRequest serviceRequest);

  Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);
}