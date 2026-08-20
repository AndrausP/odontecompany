using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public sealed class ProcedimentoConfiguration : IEntityTypeConfiguration<Procedimento>
{
    public void Configure(EntityTypeBuilder<Procedimento> builder)
    {
        builder.ToTable("Procedimentos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ValorPadrao).HasColumnType("numeric(10,2)");
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.OrganizationId, p.Ativo });
    }
}
