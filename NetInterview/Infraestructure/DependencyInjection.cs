using Domain.Interfaces;
using Infraestructure.Persistence;
using Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure;
public static class DependencyInjection
{
  public static IServiceCollection AddInfraestructure(this IServiceCollection services, string connectionString)
  {
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
    services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
    return services;
  }
}