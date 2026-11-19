using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure;
public static class DependencyInjection
{
  public static IServiceCollection AddInfraestructure(this IServiceCollection services, string connectionString)
  {
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    return services;
  }
}