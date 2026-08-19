namespace Scheduling.Contracts;

public sealed record ProfissionalDto(Guid Id, string Nome, string Especialidade, Guid? UserId, Guid? BranchId, bool Ativo);

public sealed record SalaDto(Guid Id, string Nome, int? CapacidadeMaxima, Guid? BranchId, bool Ativa);
