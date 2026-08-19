using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenancy.Domain.Entities;

namespace Tenancy.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.OrganizationId).IsRequired();
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Endereco).HasMaxLength(500);
        builder.Property(u => u.Telefone).HasMaxLength(20);
        builder.Property(u => u.Ativo).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => new { u.OrganizationId, u.Nome }).IsUnique();
    }
}
