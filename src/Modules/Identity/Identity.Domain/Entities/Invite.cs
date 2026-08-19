using Identity.Domain.Enums;
using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>
/// Convite de afiliação a uma <see cref="Organization"/> (task 016). Tenant-scoped
/// (<see cref="IMustHaveOrganization"/>) — cai no filtro global de organization por padrão. O
/// fluxo "meus convites em qualquer organization" (<c>GET /api/me/invites</c>) é uma exceção
/// deliberada e documentada em <c>IInviteRepository.GetPendingByEmailAcrossOrganizationsAsync</c>,
/// nunca um <c>IgnoreQueryFilters()</c> solto ad-hoc.
///
/// <see cref="TokenHash"/> guarda só o HASH do token (mesmo padrão de <see cref="RefreshToken"/>)
/// — o valor em claro é gerado e hasheado na Infrastructure (<c>IInviteTokenGenerator</c>, fora
/// do Domain, mesma fronteira já usada por <c>IRefreshTokenGenerator</c>/<c>RefreshToken.Create</c>)
/// e só existe no momento da criação: devolvido na resposta HTTP e logado pelo
/// <c>IInviteNotifier</c> no-op (sem provider de email real nesta sprint — débito nomeado).
/// </summary>
public class Invite : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty; // Normalizado lowercase
    public Role Role { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public InviteStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid InvitedByUserId { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    private Invite() { } // EF Core

    private Invite(Guid organizationId, string email, Role role, string tokenHash, Guid invitedByUserId, TimeSpan lifetime)
    {
        OrganizationId = organizationId;
        Email = email;
        Role = role;
        TokenHash = tokenHash;
        Status = InviteStatus.Pendente;
        ExpiresAt = DateTime.UtcNow.Add(lifetime);
        InvitedByUserId = invitedByUserId;
    }

    /// <summary>
    /// <paramref name="tokenHash"/> já vem calculado pelo chamador (Application, via
    /// <c>IInviteTokenGenerator</c>) — o Domain nunca conhece o valor em claro do token, só
    /// guarda o resultado do hash, mesma fronteira de <see cref="RefreshToken.Create"/>.
    /// </summary>
    public static Result<Invite> Create(Guid organizationId, string email, Role role, string tokenHash, Guid invitedByUserId, TimeSpan lifetime)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Invite>(DomainErrors.Invite.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Invite>(DomainErrors.Invite.EmailObrigatorio);

        if (string.IsNullOrWhiteSpace(tokenHash))
            return Result.Failure<Invite>(DomainErrors.Invite.TokenInvalido);

        return Result.Success(new Invite(organizationId, email.Trim().ToLowerInvariant(), role, tokenHash, invitedByUserId, lifetime));
    }

    public void Accept()
    {
        Status = InviteStatus.Aceito;
        SetUpdatedAt();
    }

    public void Revoke()
    {
        Status = InviteStatus.Revogado;
        SetUpdatedAt();
    }

    /// <summary>
    /// Expiração é LAZY (task 016, item 7 — sem job de background): chamado só quando alguém
    /// tenta aceitar um convite cujo <see cref="ExpiresAt"/> já passou, pra deixar o status
    /// persistido consistente com a realidade (Pendente "zumbi" nunca fica pra trás).
    /// </summary>
    public void MarkExpired()
    {
        if (Status != InviteStatus.Pendente)
            return;

        Status = InviteStatus.Expirado;
        SetUpdatedAt();
    }
}
