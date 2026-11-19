using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace WebApi.DTOs;

public record CreateServiceRequestCommand(
  [Required] Guid? ExternalReference,
  [Required] Guid? CustomerId,
  [Required, MaxLength(ServiceRequest.DescriptionMaxLength)] string Description
);
