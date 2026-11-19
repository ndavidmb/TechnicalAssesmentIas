using Domain.Entities;
using Domain.Enums;
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
    var query = context.ServiceRequests.AsNoTracking();
    if (status is not null)
      query = query.Where(s => s.Status == status);
    return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken: ct);
  }

  public async Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    return await context.ServiceRequests.FirstOrDefaultAsync(s => s.Id == id, cancellationToken: ct);
  }

  public async Task<ServiceRequest?> GetByExternalReferenceAsync(Guid externalReference, CancellationToken ct = default)
  {
    return await context.ServiceRequests.AsNoTracking()
      .FirstOrDefaultAsync(s => s.ExternalReference == externalReference, cancellationToken: ct);
  }

  public async Task<List<ServiceRequest>> GetPendingAsync(int take, CancellationToken ct = default)
  {
    return await context.ServiceRequests
      .Where(s => s.Status == ServiceRequestStatus.Pending)
      .OrderBy(s => s.CreatedAt)
      .Take(take)
      .ToListAsync(cancellationToken: ct);
  }

  public async Task<List<ServiceRequest>> GetProcessingStartedBeforeAsync(DateTimeOffset startedBefore, CancellationToken ct = default)
  {
    return await context.ServiceRequests
      .Where(s => s.Status == ServiceRequestStatus.Processing && s.ProcessingStartedAt <= startedBefore)
      .ToListAsync(cancellationToken: ct);
  }
}
