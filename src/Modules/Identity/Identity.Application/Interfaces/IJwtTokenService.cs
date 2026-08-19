using Identity.Domain.Enums;

namespace Identity.Application.Interfaces;

/// <summary>Porta pra emissão do access token JWT (implementação na Infrastructure).</summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Gera o access token com claim sub sempre presente + organization_id/role/branch_id
    /// OPCIONAIS (task 014: usuário com zero organization memberships recebe token sem essas
    /// claims — não null/vazio, AUSENTE mesmo, pra <c>RequireActiveOrganization</c> falhar fechado).
    /// Retorna o token e o TTL em segundos.
    /// </summary>
    (string Token, int ExpiresInSeconds) GenerateAccessToken(Guid userId, Guid? organizationId, Role? role, Guid? branchId = null);
}
