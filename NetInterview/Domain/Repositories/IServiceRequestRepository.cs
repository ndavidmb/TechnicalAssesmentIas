using Domain.Entities;

namespace Domain.Repositories;

public interface IServiceRequestRepository
{
  void CreateServiceRequest(ServiceRequest serviceRequest);

  Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);
}