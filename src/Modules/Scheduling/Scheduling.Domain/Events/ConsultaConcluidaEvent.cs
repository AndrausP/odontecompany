using SharedKernel;

namespace Scheduling.Domain.Events;

/// <summary>
/// Evento de domínio disparado por <see cref="Entities.Agendamento.MarcarConcluido"/>. Interno
/// ao módulo — o que atravessa a fronteira pra outros módulos (ex: Billing, task 006) é o
/// contrato público equivalente em <c>Scheduling.Contracts.ConsultaConcluidaEvent</c>, serializado
/// no Outbox por <c>SchedulingDbContext.SaveChangesAsync</c>. Os dois tipos existem separados de
/// propósito, mesma razão de <c>UniqueConstraintViolationException</c> ser duplicada por módulo:
/// Contracts não pode depender de SharedKernel/Domain.
/// </summary>
public sealed class ConsultaConcluidaEvent : IDomainEvent
{
    public Guid AgendamentoId { get; }
    public Guid OrganizationId { get; }
    public Guid PacienteId { get; }
    public Guid ProfissionalId { get; }
    public DateTime DataHoraConclusao { get; }
    public decimal Valor { get; }
    public DateTime OccurredOn { get; }

    public ConsultaConcluidaEvent(
        Guid agendamentoId, Guid organizationId, Guid pacienteId, Guid profissionalId, DateTime dataHoraConclusao, decimal valor)
    {
        AgendamentoId = agendamentoId;
        OrganizationId = organizationId;
        PacienteId = pacienteId;
        ProfissionalId = profissionalId;
        DataHoraConclusao = dataHoraConclusao;
        Valor = valor;
        OccurredOn = DateTime.UtcNow;
    }
}
