using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infraestructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
  public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }

  // Traduce los errores de EF/Postgres a excepciones que Application entiende sin depender de EF.
  // Las entradas que fallaron se desacoplan para que un guardado posterior en el mismo scope no las reintente.
  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      return await base.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException ex)
    {
      Detach(ex);
      throw new ConcurrencyConflictException("El registro fue modificado por otro proceso.", ex);
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
    {
      Detach(ex);
      throw new UniqueConstraintViolationException("Valor duplicado en una columna única.", ex);
    }
  }

  private static void Detach(DbUpdateException ex)
  {
    foreach (var entry in ex.Entries)
      entry.State = EntityState.Detached;
  }
}
