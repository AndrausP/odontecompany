using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("OrganizationMemberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.OrganizationId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.BranchId);
        builder.Property(m => m.CreatedAt).IsRequired();

        // Um mesmo User não pode ter duas memberships pra mesma Organization (índice único).
        builder.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();

        // Índice de apoio pra "todas as memberships do usuário" (login/switch-organization,
        // consultas que ignoram o filtro global de organization de propósito).
        builder.HasIndex(m => m.UserId);
    }
}
