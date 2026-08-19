using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>
/// Refresh token rotativo: armazena o HASH do token, nunca o valor em claro. Uso único —
/// ao ser trocado por um novo par, é marcado como revogado e aponta pro hash do substituto,
/// o que permite detectar reuso (indício de token roubado).
/// </summary>
public class RefreshToken : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { } // EF Core

    private RefreshToken(Guid organizationId, Guid userId, string tokenHash, DateTime expiresAt)
    {
        OrganizationId = organizationId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid organizationId, Guid userId, string tokenHash, TimeSpan lifetime)
        => new(organizationId, userId, tokenHash, DateTime.UtcNow.Add(lifetime));

    /// <summary>Revoga este token (uso único) registrando o hash do token que o substituiu na rotação.</summary>
    public void Revoke(string replacedByTokenHash)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    /// <summary>
    /// Revoga sem indicar substituto — usado quando se detecta REUSO de um token já rotacionado
    /// (apresentar de novo um token com <see cref="ReplacedByTokenHash"/> preenchido é indício de
    /// token roubado). Idempotente: chamar em token já revogado não sobrescreve o RevokedAt original.
    /// </summary>
    public void RevokeForSecurityReasons()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
    }
}
