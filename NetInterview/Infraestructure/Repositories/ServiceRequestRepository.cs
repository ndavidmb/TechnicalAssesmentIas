using Domain.Entities;
using Domain.Repositories;
namespace Infraestructure.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
  public Task<ServiceRequest> CreateServiceRequest(ServiceRequest serviceRequest)
  {
    throw new NotImplementedException();
  }

  public Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default)
  {
    throw new NotImplementedException();
  }
}