using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

public sealed class SalaRepository : ISalaRepository
{
    private readonly SchedulingDbContext _context;

    public SalaRepository(SchedulingDbContext context) => _context = context;

    public Task<Sala?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Salas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Sala sala, CancellationToken ct = default)
        => await _context.Salas.AddAsync(sala, ct);

    public async Task<IReadOnlyList<Sala>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Salas.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.Ativa);

        return await query.OrderBy(s => s.Nome).ToListAsync(ct);
    }
}
