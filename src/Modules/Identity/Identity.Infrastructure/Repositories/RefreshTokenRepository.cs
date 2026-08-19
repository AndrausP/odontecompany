using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context) => _context = context;

    public Task<RefreshToken?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash, CancellationToken ct = default)
        => _context.RefreshTokens
            .IgnoreQueryFilters() // mesmo motivo do login: organization ainda não é conhecido nesse ponto
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
        => await _context.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task RevokeAllActiveTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
            .IgnoreQueryFilters() // mesmo motivo dos demais métodos deste repositório
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.RevokeForSecurityReasons();
    }
}
