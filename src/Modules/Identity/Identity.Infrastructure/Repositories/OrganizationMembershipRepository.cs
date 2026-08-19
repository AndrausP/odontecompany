using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class OrganizationMembershipRepository : IOrganizationMembershipRepository
{
    private readonly IdentityDbContext _context;

    public OrganizationMembershipRepository(IdentityDbContext context) => _context = context;

    public Task<List<OrganizationMembership>> GetActiveMembershipsForUserAcrossOrganizationsAsync(Guid userId, CancellationToken ct = default)
        => _context.OrganizationMemberships
            .IgnoreQueryFilters() // organization ainda não é conhecido nesse ponto (login/refresh)
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Ativo)
            .ToListAsync(ct);

    public Task<OrganizationMembership?> GetByUserAndOrganizationAcrossOrganizationsAsync(Guid userId, Guid organizationId, CancellationToken ct = default)
        => _context.OrganizationMemberships
            .IgnoreQueryFilters() // mesmo motivo do método acima
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId, ct);

    public async Task AddAsync(OrganizationMembership membership, CancellationToken ct = default)
        => await _context.OrganizationMemberships.AddAsync(membership, ct);
}
