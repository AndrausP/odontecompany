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
        // Enum como string — mesma escolha do resto do projeto (Program.cs registra
        // JsonStringEnumConverter globalmente; guardar como int cru quebraria legibilidade direto no banco).
        builder.Property(p => p.TipoContrato).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PercentualComissaoDefault).HasPrecision(5, 2);
        builder.Property(p => p.Email).HasMaxLength(320); // RFC 5321
        builder.Property(p => p.UserId);
        builder.Property(p => p.BranchId);
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.OrganizationId, p.Ativo });
        // Task 042 — lookup do link automático (ListPendentesPorEmailAcrossOrganizationsAsync).
        builder.HasIndex(p => new { p.OrganizationId, p.Email });
    }
}
