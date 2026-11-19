using Application;
using Application.Processing;
using Infraestructure;
using Infraestructure.Provider;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

// Sin efecto al correr como consola; como Windows Service integra el ciclo de vida con el SCM
// y envía los logs al Event Log. En Linux el equivalente es AddSystemd().
builder.Services.AddWindowsService(o => o.ServiceName = "NetInterview.ServiceRequestsWorker");

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
  throw new InvalidOperationException("Falta ConnectionStrings:Default (configúrala con user-secrets o variable de entorno).");

var processingOptions = builder.Configuration.GetSection(ProcessingOptions.SectionName).Get<ProcessingOptions>() ?? new();
var providerOptions = builder.Configuration.GetSection(SimulatedProviderOptions.SectionName).Get<SimulatedProviderOptions>() ?? new();
var workerOptions = builder.Configuration.GetSection(WorkerOptions.SectionName).Get<WorkerOptions>() ?? new();

builder.Services.AddApplication();
builder.Services.AddServiceRequestProcessing(processingOptions);
builder.Services.AddInfraestructure(connectionString);
builder.Services.AddSimulatedProvider(providerOptions);
builder.Services.AddSingleton(workerOptions);
builder.Services.AddHostedService<ProcessingWorker>();

var host = builder.Build();
host.Run();
