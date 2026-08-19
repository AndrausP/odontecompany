using Scheduling.Domain.Enums;
using Scheduling.Domain.Errors;
using Scheduling.Domain.Events;
using Scheduling.Domain.ValueObjects;
using SharedKernel;

namespace Scheduling.Domain.Entities;

/// <summary>
/// Agregado raiz da agenda: um horário reservado para um paciente com um profissional numa sala.
/// Concorrência otimista é resolvida na Infrastructure via <c>xmin</c> nativo do Postgres (shadow
/// property mapeada em <c>AgendamentoConfiguration</c>) — não existe propriedade pública de
/// RowVersion aqui de propósito: é detalhe de persistência, o Domain não sabe (nem precisa saber)
/// que isso existe.
/// </summary>
public class Agendamento : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid ProfissionalId { get; private set; }
    public Guid SalaId { get; private set; }
    public PeriodoHorario Periodo { get; private set; } = null!;
    public AgendamentoStatus Status { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public decimal? ValorConsulta { get; private set; }

    private Agendamento() { } // EF Core

    private Agendamento(Guid organizationId, Guid pacienteId, Guid profissionalId, Guid salaId, PeriodoHorario periodo)
    {
        OrganizationId = organizationId;
        PacienteId = pacienteId;
        ProfissionalId = profissionalId;
        SalaId = salaId;
        Periodo = periodo;
        Status = AgendamentoStatus.Agendado;
    }

    /// <summary>
    /// Cria um agendamento com status inicial Agendado. Não checa sobreposição de horário aqui —
    /// isso depende de consulta ao repositório (I/O), responsabilidade do Use Case
    /// (CreateAgendamentoCommandHandler), nunca do agregado em memória.
    /// </summary>
    public static Result<Agendamento> Criar(
        Guid organizationId, Guid pacienteId, Guid profissionalId, Guid salaId, DateTime inicio, DateTime fim)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Agendamento>(DomainErrors.Agendamento.OrganizationInvalido);

        if (pacienteId == Guid.Empty)
            return Result.Failure<Agendamento>(DomainErrors.Agendamento.PacienteInvalido);

        if (profissionalId == Guid.Empty)
            return Result.Failure<Agendamento>(DomainErrors.Agendamento.ProfissionalInvalido);

        if (salaId == Guid.Empty)
            return Result.Failure<Agendamento>(DomainErrors.Agendamento.SalaInvalida);

        var periodoResult = PeriodoHorario.Create(inicio, fim);
        if (periodoResult.IsFailure)
            return Result.Failure<Agendamento>(periodoResult.Error);

        return Result.Success(new Agendamento(organizationId, pacienteId, profissionalId, salaId, periodoResult.Value));
    }

    /// <summary>Agendado → Confirmado. Qualquer outro status de origem falha.</summary>
    public Result Confirmar()
    {
        if (Status != AgendamentoStatus.Agendado)
            return Result.Failure(DomainErrors.Agendamento.SoConfirmaSeAgendado);

        Status = AgendamentoStatus.Confirmado;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Agendado/Confirmado → Cancelado. Concluido/Cancelado não podem ser cancelados de novo.</summary>
    public Result Cancelar(string? motivo = null)
    {
        if (Status is not (AgendamentoStatus.Agendado or AgendamentoStatus.Confirmado))
            return Result.Failure(DomainErrors.Agendamento.SoCancelaSeAgendadoOuConfirmado);

        Status = AgendamentoStatus.Cancelado;
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Confirmado → Concluido. Dispara <see cref="ConsultaConcluidaEvent"/> (consumido via Outbox, ex: Billing).</summary>
    public Result MarcarConcluido(decimal valor)
    {
        if (Status != AgendamentoStatus.Confirmado)
            return Result.Failure(DomainErrors.Agendamento.SoConcluiSeConfirmado);

        if (valor <= 0)
            return Result.Failure(DomainErrors.Agendamento.ValorInvalido);

        Status = AgendamentoStatus.Concluido;
        ValorConsulta = valor;
        SetUpdatedAt();

        AddDomainEvent(new ConsultaConcluidaEvent(Id, OrganizationId, PacienteId, ProfissionalId, UpdatedAt!.Value, valor));

        return Result.Success();
    }
}
