using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public sealed class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        builder.ToTable("Profissionais");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Especialidade).IsRequired().HasMaxLength(200);
        builder.Property(p => p.UserId);
        builder.Property(p => p.BranchId);
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.OrganizationId, p.Ativo });
    }
}
