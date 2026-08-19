using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    // User é entidade GLOBAL (task 013) — sem filtro de organization, não precisa de
    // IgnoreQueryFilters() (não há filtro nenhum a ignorar).
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);
}
