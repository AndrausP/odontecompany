using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IdentityDbContext _context;

    public PasswordResetTokenRepository(IdentityDbContext context) => _context = context;

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        => await _context.PasswordResetTokens.AddAsync(token, ct);

    public async Task InvalidateActiveTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.MarkUsed();
    }
}
