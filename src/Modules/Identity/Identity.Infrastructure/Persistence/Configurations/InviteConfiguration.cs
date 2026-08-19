using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.ToTable("Invites");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrganizationId).IsRequired();
        builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Hash do token, nunca o valor em claro (critério de aceite da task, mesmo padrão de RefreshToken).
        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(512);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
        builder.Property(i => i.InvitedByUserId).IsRequired();

        builder.HasIndex(i => i.TokenHash).IsUnique();

        // Apoio pro fluxo "convite pendente pro mesmo email+organization" (revogar o anterior ao criar novo).
        builder.HasIndex(i => new { i.OrganizationId, i.Email });

        // Apoio pro GetPendingByEmailAcrossOrganizationsAsync (GET /api/me/invites, cross-organization).
        builder.HasIndex(i => i.Email);
    }
}
