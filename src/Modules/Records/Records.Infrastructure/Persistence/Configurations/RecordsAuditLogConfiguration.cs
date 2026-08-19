using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Domain.Entities;

namespace Records.Infrastructure.Persistence.Configurations;

/// <summary>Tabela append-only — ver docs/knowledge/patterns.md e RecordsAuditLog.cs pro porquê de não ter mapeamento de update/delete.</summary>
public sealed class RecordsAuditLogConfiguration : IEntityTypeConfiguration<RecordsAuditLog>
{
    public void Configure(EntityTypeBuilder<RecordsAuditLog> builder)
    {
        builder.ToTable("RecordsAuditLog");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OrganizationId).IsRequired();
        builder.Property(a => a.ProntuarioId).IsRequired();
        builder.Property(a => a.PacienteId).IsRequired();
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.Acao).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.Timestamp).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.OrganizationId, a.ProntuarioId, a.Timestamp });
    }
}
