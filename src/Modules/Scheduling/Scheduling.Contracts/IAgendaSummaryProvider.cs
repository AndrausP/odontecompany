namespace Scheduling.Contracts;

/// <summary>Porta de leitura agregada pro módulo Reporting (task 008) — mesmo padrão de <c>Patients.Contracts.IPatientSummaryProvider</c>.</summary>
public interface IAgendaSummaryProvider
{
    Task<AgendaResumoDto> ObterResumoAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default);
}

public sealed record AgendaResumoDto(
    int TotalAgendamentos,
    int Agendados,
    int Confirmados,
    int Concluidos,
    int Cancelados);
