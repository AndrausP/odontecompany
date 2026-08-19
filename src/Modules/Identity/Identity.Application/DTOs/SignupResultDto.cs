namespace Identity.Application.DTOs;

/// <summary>
/// Resultado do signup público (task 015). RefreshToken é sempre <c>null</c>: usuário recém-criado
/// não tem nenhuma organization ainda, e <see cref="Identity.Domain.Entities.RefreshToken"/> é
/// <see cref="SharedKernel.IMustHaveOrganization"/> — não existe refresh token "sem org" pra
/// persistir (mesma decisão já aplicada em <see cref="LoginResultDto"/> pra usuário com zero
/// memberships, task 014). Pra ganhar capacidade de refresh, o usuário precisa criar uma
/// organization (<c>POST /api/organizations</c>, que já devolve um par novo escopado) ou aceitar
/// um convite (task 016) e logar de novo.
/// </summary>
public sealed record SignupResultDto(Guid UserId, string Nome, string Email, string AccessToken, string? RefreshToken, int ExpiresIn);
