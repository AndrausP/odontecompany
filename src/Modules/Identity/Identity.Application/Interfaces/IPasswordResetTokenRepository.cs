using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);

    /// <summary>
    /// Marca como usado qualquer token ainda válido do usuário — chamado ao gerar um novo pedido
    /// de redefinição, pra nunca deixar 2 links válidos ao mesmo tempo (mesmo raciocínio de
    /// <c>IRefreshTokenRepository.RevokeAllActiveTokensForUserAsync</c>).
    /// </summary>
    Task InvalidateActiveTokensForUserAsync(Guid userId, CancellationToken ct = default);
}
