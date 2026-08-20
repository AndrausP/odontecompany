using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>
/// Token de redefinição de senha (auditoria pré-venda — sem isso, usuário que esquece a senha
/// fica travado pra sempre, sem recuperação nenhuma). GLOBAL, não <see cref="SharedKernel.IMustHaveOrganization"/>
/// — mesmo raciocínio de <see cref="User"/> (task 013): senha é da conta, a conta não pertence a
/// uma organization específica.
///
/// Mesmo padrão de <see cref="Invite"/>/<see cref="RefreshToken"/>: <see cref="TokenHash"/> guarda
/// só o HASH (gerado/hasheado na Infrastructure via <c>IPasswordResetTokenGenerator</c>) — o valor
/// em claro só existe no instante da criação, devolvido pro <c>IPasswordResetNotifier</c>.
/// </summary>
public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;

    private PasswordResetToken() { } // EF Core

    private PasswordResetToken(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = DateTime.UtcNow.Add(lifetime);
    }

    public static Result<PasswordResetToken> Create(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            return Result.Failure<PasswordResetToken>(DomainErrors.PasswordReset.UserInvalido);

        if (string.IsNullOrWhiteSpace(tokenHash))
            return Result.Failure<PasswordResetToken>(DomainErrors.PasswordReset.TokenInvalido);

        return Result.Success(new PasswordResetToken(userId, tokenHash, lifetime));
    }

    /// <summary>Uso único — chamado só depois de validar que ainda não estava usado/expirado.</summary>
    public void MarkUsed()
    {
        UsedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
