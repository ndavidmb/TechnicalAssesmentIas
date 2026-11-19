using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.ErrorHandling;

/// <summary>
/// Traduce excepciones a ProblemDetails. Las reglas de negocio devuelven su mensaje;
/// cualquier otro error devuelve un 500 genérico sin detalles internos (se registran en el log).
/// </summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
  {
    var (status, title, detail) = exception switch
    {
      DomainValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida.", exception.Message),
      ExternalReferenceConflictException => (StatusCodes.Status409Conflict, "Conflicto de externalReference.", exception.Message),
      InvalidStatusTransitionException => (StatusCodes.Status409Conflict, "Transición de estado no permitida.", exception.Message),
      _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.", null)
    };

    if (status == StatusCodes.Status500InternalServerError)
      logger.LogError(exception, "Error no controlado procesando {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

    httpContext.Response.StatusCode = status;
    return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = exception,
      ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
    });
  }
}
