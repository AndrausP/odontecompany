using Estoque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoque.Infrastructure.Persistence.Configurations;

public sealed class ItemEstoqueConfiguration : IEntityTypeConfiguration<ItemEstoque>
{
    public void Configure(EntityTypeBuilder<ItemEstoque> builder)
    {
        builder.ToTable("ItensEstoque");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrganizationId).IsRequired();
        builder.Property(i => i.BranchId);
        builder.Property(i => i.Nome).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnidadeMedida).IsRequired().HasMaxLength(20);
        builder.Property(i => i.QuantidadeAtual).IsRequired().HasColumnType("numeric(12,3)");
        builder.Property(i => i.QuantidadeMinima).IsRequired().HasColumnType("numeric(12,3)");
        builder.Property(i => i.Ativo).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        // EstoqueBaixo é derivado (QuantidadeAtual < QuantidadeMinima) — nunca persistido, sempre recalculado no mapeamento pra DTO.
        builder.Ignore(i => i.EstoqueBaixo);

        builder.HasIndex(i => new { i.OrganizationId, i.BranchId, i.Nome }).IsUnique();
    }
}
