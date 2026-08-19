using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public sealed class SalaConfiguration : IEntityTypeConfiguration<Sala>
{
    public void Configure(EntityTypeBuilder<Sala> builder)
    {
        builder.ToTable("Salas");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrganizationId).IsRequired();
        builder.Property(s => s.Nome).IsRequired().HasMaxLength(200);
        builder.Property(s => s.CapacidadeMaxima);
        builder.Property(s => s.BranchId);
        builder.Property(s => s.Ativa).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => new { s.OrganizationId, s.Ativa });
    }
}
