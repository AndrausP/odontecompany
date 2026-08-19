using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public sealed class SchedulingOutboxMessageConfiguration : IEntityTypeConfiguration<SchedulingOutboxMessage>
{
    public void Configure(EntityTypeBuilder<SchedulingOutboxMessage> builder)
    {
        builder.ToTable("SchedulingOutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.PublishedOn);

        // Worker (quando existir) faz poll por PublishedOn IS NULL — índice cobre esse acesso.
        builder.HasIndex(m => m.PublishedOn);
    }
}
