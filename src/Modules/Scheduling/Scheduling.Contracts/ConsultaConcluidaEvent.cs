using Contracts.Abstractions;

namespace Scheduling.Contracts;

/// <summary>
/// Contrato público do evento de conclusão de consulta — o payload serializado na tabela de
/// Outbox (<c>SchedulingOutboxMessages</c>) e o formato que outro módulo (ex: Billing, task 006)
/// consumiria via MassTransit/RabbitMQ. Distinto de <c>Scheduling.Domain.Events.ConsultaConcluidaEvent</c>
/// (o <c>IDomainEvent</c> interno) — ver XML doc daquele tipo pra racional completo.
/// </summary>
public sealed record ConsultaConcluidaEvent(
    Guid AgendamentoId,
    Guid OrganizationId,
    Guid PacienteId,
    Guid ProfissionalId,
    DateTime DataHoraConclusao,
    decimal Valor
) : IModuleContract;
