using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IOrganizationRepository
{
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Busca em lote (tasks 016/017 — resolver nome de organization pra uma lista de memberships
    /// ou convites sem N idas ao banco). Ids desconhecidos simplesmente não aparecem no resultado.
    /// </summary>
    Task<List<Organization>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task AddAsync(Organization organization, CancellationToken ct = default);
}
