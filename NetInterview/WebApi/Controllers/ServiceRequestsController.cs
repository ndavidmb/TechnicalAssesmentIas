using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;

namespace WebApi.Controllers;

[ApiController]
[Route("api/service-requests")]
public class ServiceRequestsController(IServiceRequestService service) : ControllerBase
{
  [HttpGet]
  [ProducesResponseType<List<ServiceRequestDto>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> GetAll([FromQuery] ServiceRequestStatus? status, CancellationToken ct)
  {
    if (status is not null && !Enum.IsDefined(status.Value))
    {
      ModelState.AddModelError(nameof(status), $"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<ServiceRequestStatus>())}.");
      return ValidationProblem(ModelState);
    }

    var result = await service.GetAllAsync(status, ct);
    return Ok(result.Select(ServiceRequestDto.From));
  }

  [HttpGet("{id:guid}")]
  [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
  {
    var result = await service.GetByIdAsync(id, ct);
    return result is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Solicitud no encontrada.") : Ok(ServiceRequestDto.From(result));
  }

  /// <summary>
  /// Idempotente por externalReference: un reintento con los mismos datos devuelve 200 con la solicitud existente;
  /// con datos distintos devuelve 409.
  /// </summary>
  [HttpPost]
  [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status201Created)]
  [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> CreateServiceRequest([FromBody] CreateServiceRequestCommand request, CancellationToken ct)
  {
    var result = await service.CreateServiceRequestAsync(
      request.ExternalReference!.Value,
      request.CustomerId!.Value,
      request.Description,
      ct
    );
    var dto = ServiceRequestDto.From(result.ServiceRequest);
    return result.Created ? CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto) : Ok(dto);
  }
}
