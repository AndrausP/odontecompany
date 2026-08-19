using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IInviteRepository
{
    /// <summary>Convite PENDENTE (não expirado) pro mesmo (email, organization) — usado pra decidir se revoga o anterior ao criar um novo (task 016, item 3).</summary>
    Task<Invite?> GetPendingByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Busca convites PENDENTES (e não expirados) pra um email, em QUALQUER organization.
    ///
    /// Deliberadamente ignora o filtro global de organization porque o caso de uso é legítimo:
    /// usuário (recém-criado ou existente) vê convites de organizations onde ainda NÃO é membro.
    ///
    /// REGRA: usar APENAS em contexto de "convites pro email do usuário logado"
    /// (GET /api/me/invites, GET /api/me); nunca em filtro ou query genérica que possa vazar
    /// convite de uma organization pra quem não deveria ver.
    /// </summary>
    Task<List<Invite>> GetPendingByEmailAcrossOrganizationsAsync(string email, CancellationToken ct = default);

    /// <summary>Ignora o filtro global pelo mesmo motivo do refresh token: a organization ainda não é conhecida nesse ponto (accept, antes de saber a qual organization o token pertence).</summary>
    Task<Invite?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Todos os convites da organization (dentro do filtro global normal) — Owner/Admin gerenciando a própria organization.</summary>
    Task<List<Invite>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    Task AddAsync(Invite invite, CancellationToken ct = default);
}
