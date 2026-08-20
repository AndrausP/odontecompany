namespace Scheduling.Contracts;

/// <summary>Catálogo de procedimentos/serviços da clínica (task 044).</summary>
public sealed record ProcedimentoDto(Guid Id, string Nome, decimal? ValorPadrao, int? DuracaoPadraoMinutos, bool Ativo);
