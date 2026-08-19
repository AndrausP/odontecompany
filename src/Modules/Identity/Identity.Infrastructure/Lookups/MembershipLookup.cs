using Identity.Contracts;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Lookups;

/// <summary>Implementação de <see cref="IMembershipLookup"/> — mesmo padrão de <c>Tenancy.Infrastructure.Lookups.BranchLookup</c>.</summary>
public sealed class MembershipLookup : IMembershipLookup
{
    private readonly IdentityDbContext _context;

    public MembershipLookup(IdentityDbContext context) => _context = context;

    public async Task<IReadOnlyList<MembershipResumoDto>> ListarPorOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await _context.OrganizationMemberships
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de BranchLookup
            .Where(m => m.OrganizationId == organizationId)
            .Select(m => new MembershipResumoDto(m.UserId, m.Role.ToString(), m.BranchId, m.IsAtivo))
            .ToListAsync(ct);
}
