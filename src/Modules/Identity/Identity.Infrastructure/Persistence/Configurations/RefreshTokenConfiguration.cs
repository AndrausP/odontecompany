using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();

        // Hash do token, nunca o valor em claro (critério de aceite da task).
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(512);
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.ReplacedByTokenHash).HasMaxLength(512);

        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.OrganizationId);
    }
}
