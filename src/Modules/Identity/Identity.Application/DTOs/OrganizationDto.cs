namespace Identity.Application.DTOs;

/// <summary>Dados editáveis da organização — tela de configurações (item futuro da task 039).</summary>
public sealed record OrganizationDto(Guid Id, string Nome, string? Cnpj, string? Telefone, string? Endereco, bool Ativo);
