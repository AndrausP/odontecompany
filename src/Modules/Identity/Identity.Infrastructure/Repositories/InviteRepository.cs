using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class InviteRepository : IInviteRepository
{
    private readonly IdentityDbContext _context;

    public InviteRepository(IdentityDbContext context) => _context = context;

    public Task<Invite?> GetPendingByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken ct = default)
        => _context.Invites
            .Where(i => i.OrganizationId == organizationId
                && i.Email == email.ToLowerInvariant()
                && i.Status == InviteStatus.Pendente)
            .FirstOrDefaultAsync(ct);

    public Task<List<Invite>> GetPendingByEmailAcrossOrganizationsAsync(string email, CancellationToken ct = default)
        => _context.Invites
            .IgnoreQueryFilters() // organization ainda não é conhecido — ver doc da interface
            .Where(i => i.Email == email.ToLowerInvariant()
                && i.Status == InviteStatus.Pendente
                && i.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public Task<Invite?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash, CancellationToken ct = default)
        => _context.Invites
            .IgnoreQueryFilters() // mesmo motivo do refresh token: organization não é conhecido nesse ponto
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public Task<List<Invite>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => _context.Invites
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Invite invite, CancellationToken ct = default)
        => await _context.Invites.AddAsync(invite, ct);
}
