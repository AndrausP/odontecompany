namespace Scheduling.Contracts;

/// <summary>Porta de leitura agregada pro módulo Reporting (task 044) — mesmo padrão de <see cref="IAgendaSummaryProvider"/>.</summary>
public interface IReceitaPorProcedimentoProvider
{
    Task<IReadOnlyList<ReceitaProcedimentoDto>> ObterAsync(Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default);
}

/// <summary>
/// Receita de agendamentos CONCLUÍDOS no período, agrupada por procedimento. `ProcedimentoId`
/// nulo agrupa agendamentos sem procedimento escolhido — não descartados, só sem essa dimensão.
/// </summary>
public sealed record ReceitaProcedimentoDto(
    Guid? ProcedimentoId,
    string ProcedimentoNome,
    int Quantidade,
    decimal ValorTotal);
