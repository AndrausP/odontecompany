using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome).IsRequired().HasMaxLength(150);
        builder.Property(t => t.Cnpj).HasMaxLength(18); // "00.000.000/0000-00" formatado
        builder.Property(t => t.Telefone).HasMaxLength(20);
        builder.Property(t => t.Endereco).HasMaxLength(500);
        builder.Property(t => t.Ativo).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
