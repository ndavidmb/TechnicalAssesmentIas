using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
  public void Configure(EntityTypeBuilder<ServiceRequest> builder)
  {
    builder.ToTable("ServiceRequests");
    builder.HasKey(s => s.Id);
    builder.HasIndex(s => s.ExternarReference);
    builder.Property(s => s.ServiceCode).ValueGeneratedOnAdd();
    builder.Property(s => s.CustomerId).IsRequired();
    builder.Property(e => e.Id).ValueGeneratedNever();
  }
}