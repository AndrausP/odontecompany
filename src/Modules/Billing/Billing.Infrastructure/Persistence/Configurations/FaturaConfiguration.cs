using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Billing.Domain.Entities;

namespace Billing.Infrastructure.Persistence.Configurations;

public sealed class FaturaConfiguration : IEntityTypeConfiguration<Fatura>
{
    public void Configure(EntityTypeBuilder<Fatura> builder)
    {
        builder.ToTable("Faturas");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OrganizationId).IsRequired();
        builder.Property(f => f.PacienteId).IsRequired();
        builder.Property(f => f.AgendamentoId);
        builder.Property(f => f.ProfissionalId);
        builder.Property(f => f.TipoFatura).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.ConvenioId);
        builder.Property(f => f.ValorTotal).IsRequired().HasColumnType("numeric(12,2)");
        builder.Property(f => f.FormaPagamento).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.ComissaoDentistaPercentual).HasColumnType("numeric(5,2)");
        builder.Property(f => f.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.ProtocoloConvenio).HasMaxLength(100);
        builder.Property(f => f.CreatedAt).IsRequired();

        // Coleção encapsulada — o agregado só expõe IReadOnlyList<Parcela> (sem setter público).
        // Configurar via `HasMany("_parcelas")` (campo) E deixar o EF Core auto-descobrir
        // `Parcelas` (propriedade pública) cria DUAS navegações apontando pro mesmo campo —
        // conflito ("member already used"). A propriedade PÚBLICA é a navegação real;
        // UsePropertyAccessMode(Field) só instrui o EF a ler/escrever via `_parcelas` por baixo
        // (a propriedade não tem setter visível pro EF usar).
        builder.HasMany(f => f.Parcelas)
            .WithOne()
            .HasForeignKey(p => p.FaturaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Parcelas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(f => new { f.OrganizationId, f.PacienteId });

        // Índice único PARCIAL: no máximo uma fatura por agendamento, mas permite múltiplas
        // faturas com AgendamentoId null (faturas particulares avulsas, sem consulta associada).
        // Garante idempotência de CreateFaturaFromConsultaConcluida mesmo sob reentrega de fila.
        builder.HasIndex(f => f.AgendamentoId)
            .IsUnique()
            .HasFilter("\"AgendamentoId\" IS NOT NULL");
    }
}
