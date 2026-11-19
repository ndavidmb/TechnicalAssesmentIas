using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }
}