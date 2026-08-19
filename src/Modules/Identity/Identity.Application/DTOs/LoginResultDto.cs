namespace Identity.Application.DTOs;

/// <summary>
/// Par de tokens devolvido por login/refresh/switch-organization. <see cref="RefreshToken"/> é
/// null quando o AccessToken foi emitido SEM organization ativa (usuário com zero memberships —
/// task 014, decisão do Architect): RefreshToken é <see cref="SharedKernel.IMustHaveOrganization"/>,
/// não existe refresh token "sem org" pra persistir. Usuário nessa situação loga de novo, ou
/// aceita um convite primeiro (task 016) — não há renovação de sessão possível ainda.
/// </summary>
public sealed record LoginResultDto(string AccessToken, string? RefreshToken, int ExpiresIn);
