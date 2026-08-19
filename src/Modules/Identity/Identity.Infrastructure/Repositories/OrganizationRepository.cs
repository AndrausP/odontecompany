using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly IdentityDbContext _context;

    public OrganizationRepository(IdentityDbContext context) => _context = context;

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => _context.Organizations.AnyAsync(ct);

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Organizations.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Organization>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => _context.Organizations.Where(o => ids.Contains(o.Id)).ToListAsync(ct);

    public async Task AddAsync(Organization organization, CancellationToken ct = default)
        => await _context.Organizations.AddAsync(organization, ct);
}
