using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IOrganizationMembershipRepository
{
    /// <summary>
    /// Todas as memberships ATIVAS do usuário, em QUALQUER organization — ignora o filtro global
    /// de propósito (mesma exceção documentada em IUserRepository/IRefreshTokenRepository antes da
    /// task 013: no login/refresh ainda não há organization resolvido no contexto da requisição).
    /// Usado pra eleição da organization ativa (a mais antiga, critério do Tech Lead — task 014).
    /// </summary>
    Task<List<OrganizationMembership>> GetActiveMembershipsForUserAcrossOrganizationsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Mesma exceção do método acima — usado no refresh (reeleger a org do token) e no switch-organization.</summary>
    Task<OrganizationMembership?> GetByUserAndOrganizationAcrossOrganizationsAsync(Guid userId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Quantas memberships ATIVAS a organização tem — limite de usuário por plano (auditoria
    /// pré-venda, mesmo raciocínio de <c>IBranchRepository.ListByOrganizationAsync</c> pro limite
    /// de filial). <c>IgnoreQueryFilters</c>: chamado a partir de <c>AcceptInviteCommandHandler</c>,
    /// que roda no contexto do usuário CONVIDADO — a organização do convite pode não ser a
    /// organização ativa do token dele ainda.
    /// </summary>
    Task<int> CountActiveByOrganizationAcrossOrganizationsAsync(Guid organizationId, CancellationToken ct = default);

    Task AddAsync(OrganizationMembership membership, CancellationToken ct = default);
}
