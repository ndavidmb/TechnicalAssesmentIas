namespace Application.Processing;

public class ProcessingOptions
{
  public const string SectionName = "Processing";

  /// <summary>Máximo de solicitudes que toma cada ejecución.</summary>
  public int BatchSize { get; set; } = 10;

  /// <summary>Intentos totales contra el proveedor antes de dejar la solicitud en Failed.</summary>
  public int MaxAttempts { get; set; } = 3;

  /// <summary>Tiempo tras el cual una solicitud en Processing se considera abandonada.</summary>
  public TimeSpan LeaseTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
