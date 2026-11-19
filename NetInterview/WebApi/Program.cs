using Application;
using Infraestructure;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WebApi.ErrorHandling;
var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión no vive en el repo: user-secrets en desarrollo, variable de entorno
// ConnectionStrings__Default en otros entornos.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Falta ConnectionStrings:Default (configúrala con user-secrets o variable de entorno).");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfraestructure(connectionString);
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Inicialización reproducible de la base en desarrollo: aplica las migraciones pendientes.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapControllers();
app.Run();
