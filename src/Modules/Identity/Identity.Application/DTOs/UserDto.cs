namespace Identity.Application.DTOs;

/// <summary>Dados públicos do usuário — task 017. Só o mínimo pedido pelo escopo (sem telefone/avatar/preferências, fora de escopo).</summary>
public sealed record UserDto(Guid Id, string Nome, string Email, bool Ativo, bool OnboardingSkipped);
