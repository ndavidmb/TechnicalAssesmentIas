using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
  public void Configure(EntityTypeBuilder<ServiceRequest> builder)
  {
    builder.ToTable("ServiceRequests");
    builder.HasKey(s => s.Id);
    builder.Property(e => e.Id).ValueGeneratedNever();

    // La validación en código no resiste dos POST concurrentes; el índice único sí.
    builder.HasIndex(s => s.ExternalReference).IsUnique();
    builder.HasIndex(s => new { s.Status, s.CreatedAt });

    builder.Property(s => s.ServiceCode).ValueGeneratedOnAdd();
    builder.Property(s => s.CustomerId).IsRequired();
    builder.Property(s => s.Description).IsRequired().HasMaxLength(ServiceRequest.DescriptionMaxLength);
    builder.Property(s => s.LastError).HasMaxLength(ServiceRequest.LastErrorMaxLength);

    // Concurrencia optimista sobre la columna de sistema xmin de Postgres: cada UPDATE incluye
    // "WHERE xmin = @original", así dos workers no pueden pasar la misma fila a Processing.
    builder.Property<uint>("Version").IsRowVersion();
  }
}
