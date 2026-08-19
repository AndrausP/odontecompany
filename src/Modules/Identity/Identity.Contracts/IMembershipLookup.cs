namespace Identity.Contracts;

/// <summary>
/// Porta pública do módulo Identity pra outros módulos (Reporting, task 023) lerem as
/// afiliações (<c>OrganizationMembership</c>) de um organization sem depender de
/// Identity.Domain/Infrastructure — mesmo padrão de <c>Tenancy.Contracts.IBranchLookup</c>.
/// </summary>
public interface IMembershipLookup
{
    /// <summary>Todas as afiliações do organization, batch, sem N+1.</summary>
    Task<IReadOnlyList<MembershipResumoDto>> ListarPorOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}

/// <summary>
/// <see cref="Role"/> como string — mesma fronteira pública de <see cref="ICurrentUserAccessor.Role"/>:
/// o enum do módulo dono nunca cruza a borda de Contracts. <see cref="Ativo"/> deriva de
/// <c>MembershipStatus.Ativo</c>.
/// </summary>
public sealed record MembershipResumoDto(Guid UserId, string Role, Guid? BranchId, bool Ativo);
