namespace Scheduling.Domain.Enums;

/// <summary>
/// Ciclo de vida do agendamento. Transições válidas (impostas pela própria entidade
/// <see cref="Entities.Agendamento"/>, nunca por quem chama): Agendado → Confirmado → Concluido;
/// Agendado/Confirmado → Cancelado. Não existe transição de volta.
/// </summary>
public enum AgendamentoStatus
{
    Agendado = 1,
    Confirmado = 2,
    Cancelado = 3,
    Concluido = 4
}
