using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public class ServiceRequestService(
  IServiceRequestRepository serviceRequestRepository,
  IUnitOfWork unitOfWork,
  TimeProvider timeProvider) : IServiceRequestService
{
  public async Task<CreateServiceRequestResult> CreateServiceRequestAsync(Guid externalReference, Guid customerId, string description, CancellationToken ct = default)
  {
    var existing = await serviceRequestRepository.GetByExternalReferenceAsync(externalReference, ct);
    if (existing is not null)
      return ResolveRetry(existing, customerId, description);

    var service = ServiceRequest.Create(externalReference, customerId, description, timeProvider.GetUtcNow());
    serviceRequestRepository.CreateServiceRequest(service);
    try
    {
      await unitOfWork.SaveChangesAsync(ct);
    }
    catch (UniqueConstraintViolationException)
    {
      // Dos POST concurrentes con el mismo externalReference: el índice único dejó pasar solo uno.
      existing = await serviceRequestRepository.GetByExternalReferenceAsync(externalReference, ct)
        ?? throw new InvalidOperationException("Violación de unicidad sin registro existente.");
      return ResolveRetry(existing, customerId, description);
    }
    return new CreateServiceRequestResult(service, Created: true);
  }

  public async Task<List<ServiceRequest>> GetAllAsync(ServiceRequestStatus? status, CancellationToken ct = default)
  {
    return await serviceRequestRepository.GetAllAsync(status, ct);
  }

  public async Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    return await serviceRequestRepository.GetByIdAsync(id, ct);
  }

  private static CreateServiceRequestResult ResolveRetry(ServiceRequest existing, Guid customerId, string description)
  {
    if (!existing.HasSamePayload(customerId, description))
      throw new ExternalReferenceConflictException(existing.ExternalReference);
    return new CreateServiceRequestResult(existing, Created: false);
  }
}
