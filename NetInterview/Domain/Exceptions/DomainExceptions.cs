namespace Domain.Exceptions;

/// <summary>Base de las reglas de negocio violadas. La API las traduce a respuestas 4xx.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>Datos de entrada que no cumplen las invariantes de la entidad.</summary>
public class DomainValidationException(string message) : DomainException(message);

/// <summary>Transición de estado no permitida (p. ej. sacar una solicitud de Completed).</summary>
public class InvalidStatusTransitionException(string message) : DomainException(message);

/// <summary>Ya existe una solicitud con el mismo externalReference pero con datos distintos.</summary>
public class ExternalReferenceConflictException(Guid externalReference)
  : DomainException($"Ya existe una solicitud con externalReference '{externalReference}' y datos distintos.");

/// <summary>Otro proceso modificó la misma fila antes que nosotros (concurrencia optimista).</summary>
public class ConcurrencyConflictException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Se intentó insertar un valor repetido en una columna con índice único.</summary>
public class UniqueConstraintViolationException(string message, Exception? inner = null) : Exception(message, inner);
