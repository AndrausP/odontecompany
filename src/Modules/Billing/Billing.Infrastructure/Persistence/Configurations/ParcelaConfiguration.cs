using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Billing.Domain.Entities;

namespace Billing.Infrastructure.Persistence.Configurations;

public sealed class ParcelaConfiguration : IEntityTypeConfiguration<Parcela>
{
    public void Configure(EntityTypeBuilder<Parcela> builder)
    {
        builder.ToTable("Parcelas");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.FaturaId).IsRequired();
        builder.Property(p => p.NumeroParcela).IsRequired();
        builder.Property(p => p.ValorParcela).IsRequired().HasColumnType("numeric(12,2)");
        builder.Property(p => p.DataVencimento).IsRequired();
        builder.Property(p => p.DataPagamento);
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.OrganizationId, p.FaturaId });
    }
}
