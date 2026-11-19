using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

/// <param name="Created">false cuando se devolvió una solicitud existente con el mismo externalReference.</param>
public record CreateServiceRequestResult(ServiceRequest ServiceRequest, bool Created);

public interface IServiceRequestService
{
  public Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default);
  public Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
  public Task<CreateServiceRequestResult> CreateServiceRequestAsync(Guid externalReference, Guid customerId, string description, CancellationToken ct = default);
}
