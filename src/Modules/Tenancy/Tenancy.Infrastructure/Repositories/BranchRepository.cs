using Microsoft.EntityFrameworkCore;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Entities;
using Tenancy.Infrastructure.Persistence;

namespace Tenancy.Infrastructure.Repositories;

public sealed class BranchRepository : IBranchRepository
{
    private readonly TenancyDbContext _context;

    public BranchRepository(TenancyDbContext context) => _context = context;

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Branches.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(Branch branch, CancellationToken ct = default)
        => await _context.Branches.AddAsync(branch, ct);

    public async Task<IReadOnlyList<Branch>> ListByOrganizationAsync(Guid organizationId, bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Branches.AsNoTracking().IgnoreQueryFilters().Where(u => u.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(u => u.Ativo);

        return await query.OrderBy(u => u.Nome).ToListAsync(ct);
    }
}
