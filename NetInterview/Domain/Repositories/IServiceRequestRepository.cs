using Domain.Entities;

namespace Domain.Repositories;

public interface IServiceRequestRepository
{
  Task<ServiceRequest> CreateServiceRequest(ServiceRequest serviceRequest);
  Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);
}