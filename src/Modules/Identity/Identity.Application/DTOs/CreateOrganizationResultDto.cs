namespace Identity.Application.DTOs;

/// <summary>
/// Resultado da criação de organization (task 015): usuário autenticado vira Owner dela
/// automaticamente e recebe um NOVO par access+refresh já escopado à organization criada —
/// decisão do Architect: uma ida a menos pro cliente, não obriga chamar switch-organization
/// logo em seguida.
/// </summary>
public sealed record CreateOrganizationResultDto(Guid OrganizationId, string OrganizationName, string AccessToken, string RefreshToken, int ExpiresIn);
