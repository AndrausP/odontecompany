namespace Tenancy.Contracts;

public sealed record BranchDto(Guid Id, Guid OrganizationId, string Nome, string? Endereco, string? Telefone, bool Ativo);
