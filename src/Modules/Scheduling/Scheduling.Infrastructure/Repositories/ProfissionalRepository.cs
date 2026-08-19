using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

public sealed class ProfissionalRepository : IProfissionalRepository
{
    private readonly SchedulingDbContext _context;

    public ProfissionalRepository(SchedulingDbContext context) => _context = context;

    public Task<Profissional?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Profissionais.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Profissional profissional, CancellationToken ct = default)
        => await _context.Profissionais.AddAsync(profissional, ct);

    public async Task<IReadOnlyList<Profissional>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Profissionais.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.Ativo);

        return await query.OrderBy(p => p.Nome).ToListAsync(ct);
    }
}
