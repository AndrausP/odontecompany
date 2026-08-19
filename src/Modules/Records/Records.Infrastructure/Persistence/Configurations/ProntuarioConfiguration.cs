using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.Infrastructure.Persistence.Configurations;

public sealed class ProntuarioConfiguration : IEntityTypeConfiguration<Prontuario>
{
    public void Configure(EntityTypeBuilder<Prontuario> builder)
    {
        builder.ToTable("Prontuarios");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.PacienteId).IsRequired();
        builder.Property(p => p.CriadoPorUserId).IsRequired();
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        // Odontograma: Dictionary<int(FDI), StatusDente> como JSONB — dado estrutural simples,
        // não é dado clínico em texto livre (não precisa de criptografia de campo como
        // DescricaoClinica). PostgreSQL suporta JSONB nativo pra isso (doc de arquitetura, seção
        // Stack: "Suporte a JSONB (anexos de prontuário)").
        var odontogramaComparer = new ValueComparer<Dictionary<int, StatusDente>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            d => d.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
            d => new Dictionary<int, StatusDente>(d));

        builder.Property<Dictionary<int, StatusDente>>("_odontograma")
            .HasColumnName("Odontograma")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<int, StatusDente>>(v, (JsonSerializerOptions?)null) ?? new())
            .Metadata.SetValueComparer(odontogramaComparer);

        // Um prontuário por paciente por organization.
        builder.HasIndex(p => new { p.OrganizationId, p.PacienteId }).IsUnique();
    }
}
