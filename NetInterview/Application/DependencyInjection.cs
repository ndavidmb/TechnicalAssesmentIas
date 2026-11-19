using Application.Processing;
using Application.Services;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;
public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.TryAddSingleton(TimeProvider.System);
    services.AddScoped<IServiceRequestService, ServiceRequestService>();
    return services;
  }

  public static IServiceCollection AddServiceRequestProcessing(this IServiceCollection services, ProcessingOptions options)
  {
    services.AddSingleton(options);
    services.AddScoped<ProcessPendingServiceRequests>();
    return services;
  }
}
