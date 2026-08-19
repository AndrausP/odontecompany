using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Billing.Domain.Entities;

namespace Billing.Infrastructure.Persistence.Configurations;

public sealed class ConvenioConfiguration : IEntityTypeConfiguration<Convenio>
{
    public void Configure(EntityTypeBuilder<Convenio> builder)
    {
        builder.ToTable("Convenios");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.OrganizationId).IsRequired();
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.CodigoExterno).HasMaxLength(100);
        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => new { c.OrganizationId, c.Nome }).IsUnique();
    }
}
