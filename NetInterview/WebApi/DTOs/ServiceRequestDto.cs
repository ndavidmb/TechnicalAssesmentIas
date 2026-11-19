using Domain.Entities;
using Domain.Enums;

namespace WebApi.DTOs;

public record ServiceRequestDto(
  Guid Id,
  Guid ExternalReference,
  Guid CustomerId,
  int ServiceCode,
  string Description,
  ServiceRequestStatus Status,
  int AttemptCount,
  string? LastError,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt)
{
  public static ServiceRequestDto From(ServiceRequest s) => new(
    s.Id, s.ExternalReference, s.CustomerId, s.ServiceCode, s.Description,
    s.Status, s.AttemptCount, s.LastError, s.CreatedAt, s.UpdatedAt);
}
