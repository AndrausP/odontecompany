using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>Ignora o filtro global de organization pelo mesmo motivo de IUserRepository — o organization
    /// ainda não é conhecido nesse ponto do fluxo de refresh.</summary>
    Task<RefreshToken?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revoga TODOS os refresh tokens ativos do usuário — usado quando se detecta reuso de um
    /// token já rotacionado (indício de token roubado). Corta a sessão inteira do usuário,
    /// forçando novo login tanto do atacante quanto da vítima.
    /// </summary>
    Task RevokeAllActiveTokensForUserAsync(Guid userId, CancellationToken ct = default);
}
