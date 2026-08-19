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
        builder.Property(t => t.Ativo).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
