using Domain.Entities;
using Domain.Interfaces;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public class ServiceRequestRepository(AppDbContext context) : IServiceRequestRepository
{
  public void CreateServiceRequest(ServiceRequest serviceRequest)
  {
    context.ServiceRequests.Add(serviceRequest);
  }

  public async Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default)
  {
    return await context.ServiceRequests.AsNoTracking().Where(s => s.Status == status).ToListAsync(cancellationToken: ct);
  }

  public async Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    return await context.ServiceRequests.FirstOrDefaultAsync(s => s.Id == id, cancellationToken: ct);
  }
}