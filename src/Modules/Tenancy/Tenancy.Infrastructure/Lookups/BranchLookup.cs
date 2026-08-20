using Microsoft.EntityFrameworkCore;
using Tenancy.Contracts;
using Tenancy.Infrastructure.Persistence;

namespace Tenancy.Infrastructure.Lookups;

public sealed class BranchLookup : IBranchLookup
{
    private readonly TenancyDbContext _context;

    public BranchLookup(TenancyDbContext context) => _context = context;

    public Task<bool> ExistsAsync(Guid organizationId, Guid branchId, CancellationToken ct = default)
        => _context.Branches.AsNoTracking().IgnoreQueryFilters()
            .AnyAsync(u => u.Id == branchId && u.OrganizationId == organizationId && u.Ativo, ct);

    public Task<int> CountAtivasAsync(Guid organizationId, CancellationToken ct = default)
        => _context.Branches.AsNoTracking().IgnoreQueryFilters()
            .CountAsync(b => b.OrganizationId == organizationId && b.Ativo, ct);

    public async Task<IReadOnlyDictionary<Guid, string>> ListarNomesAsync(Guid organizationId, CancellationToken ct = default)
        => await _context.Branches
            .AsNoTracking()
            .IgnoreQueryFilters() // organization vem explícito no parâmetro — mesmo padrão de ExistsAsync
            .Where(b => b.OrganizationId == organizationId) // sem filtro de Ativo — inclui inativas de propósito
            .ToDictionaryAsync(b => b.Id, b => b.Nome, ct);
}
