using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscriptions.Domain.Entities;

namespace Subscriptions.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrganizationId).IsRequired();
        builder.Property(s => s.Tier).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.StripeCustomerId).HasMaxLength(255);
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(s => s.CreatedAt).IsRequired();

        // 1:1 Organization -> Subscription — nunca duas assinaturas ativas pra mesma organização (SelectPlanCommandHandler faz upsert em cima disso).
        builder.HasIndex(s => s.OrganizationId).IsUnique();
    }
}
