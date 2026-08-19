using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("Agendamentos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OrganizationId).IsRequired();
        builder.Property(a => a.PacienteId).IsRequired();
        builder.Property(a => a.ProfissionalId).IsRequired();
        builder.Property(a => a.SalaId).IsRequired();

        // VO PeriodoHorario mapeado como owned type — Inicio/Fim viram colunas próprias na mesma
        // tabela, sem tabela separada (não é uma entidade filha, é valor). Índice do início do
        // período criado AQUI DENTRO (via OwnedNavigationBuilder), não no builder do owner — EF
        // Core 8 não aceita `owner.HasIndex(a => new { ..., a.Periodo.Inicio })` (lambda
        // atravessando pra dentro de um owned type não é "member access" válido pro HasIndex do
        // tipo pai; só funciona pedindo o índice a partir do próprio builder do owned type).
        builder.OwnsOne(a => a.Periodo, periodo =>
        {
            periodo.Property(p => p.Inicio).HasColumnName("Periodo_Inicio").IsRequired();
            periodo.Property(p => p.Fim).HasColumnName("Periodo_Fim").IsRequired();
            periodo.HasIndex(p => p.Inicio).HasDatabaseName("IX_Agendamentos_PeriodoInicio");
        });
        builder.Navigation(a => a.Periodo).IsRequired();

        builder.Property(a => a.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(a => a.MotivoCancelamento).HasMaxLength(500);
        builder.Property(a => a.ValorConsulta).HasColumnType("numeric(10,2)");
        builder.Property(a => a.CreatedAt).IsRequired();

        // Concorrência otimista via xmin nativo do Postgres — recomendação oficial do provider
        // Npgsql em vez de simular o `rowversion` do SQL Server com um byte[] gerado manualmente.
        // Shadow property: não existe como membro público na entidade de domínio (ver XML doc de
        // Agendamento) — Domain não sabe (e não precisa saber) de detalhe de persistência.
        builder.Property<uint>("xmin").IsRowVersion();

        // Índice (OrganizationId, ProfissionalId) — acelera a checagem de sobreposição
        // (ExisteSobreposicaoAsync) combinado com o índice de Periodo_Inicio acima.
        builder.HasIndex(a => new { a.OrganizationId, a.ProfissionalId })
            .HasDatabaseName("IX_Agendamentos_OrganizationId_ProfissionalId");
    }
}
