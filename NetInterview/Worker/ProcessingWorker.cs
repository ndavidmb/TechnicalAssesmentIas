using Application.Processing;

namespace Worker;

public class WorkerOptions
{
  public const string SectionName = "Worker";

  /// <summary>Espera entre ejecuciones del caso de uso.</summary>
  public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

  /// <summary>Ejecuta un solo lote y termina (ejecución manual: --Worker:RunOnce=true).</summary>
  public bool RunOnce { get; set; }
}

/// <summary>
/// Solo orquesta: crea un scope por ejecución (el DbContext es scoped), invoca el caso de uso,
/// registra errores sin detener el servicio y espera el intervalo. La lógica vive en Application.
/// </summary>
public class ProcessingWorker(
  IServiceScopeFactory scopeFactory,
  WorkerOptions options,
  IHostApplicationLifetime lifetime,
  ILogger<ProcessingWorker> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    logger.LogInformation("Worker iniciado. Intervalo: {Interval}. RunOnce: {RunOnce}", options.PollingInterval, options.RunOnce);

    using var timer = new PeriodicTimer(options.PollingInterval);
    do
    {
      await RunBatchAsync(stoppingToken);

      if (options.RunOnce)
      {
        lifetime.StopApplication();
        return;
      }
    }
    while (await WaitNextTickAsync(timer, stoppingToken));
  }

  private async Task RunBatchAsync(CancellationToken stoppingToken)
  {
    try
    {
      using var scope = scopeFactory.CreateScope();
      var useCase = scope.ServiceProvider.GetRequiredService<ProcessPendingServiceRequests>();
      var summary = await useCase.ExecuteAsync(stoppingToken);

      if (summary is not { Released: 0, Completed: 0, Retried: 0, Failed: 0, Skipped: 0 })
        logger.LogInformation("Lote procesado: {@Summary}", summary);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Apagado ordenado.
    }
    catch (Exception ex)
    {
      // Un error de un lote (p. ej. base de datos caída) no debe tumbar el servicio.
      logger.LogError(ex, "Error procesando el lote de solicitudes pendientes.");
    }
  }

  private static async Task<bool> WaitNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
  {
    try
    {
      return await timer.WaitForNextTickAsync(stoppingToken);
    }
    catch (OperationCanceledException)
    {
      return false;
    }
  }
}
