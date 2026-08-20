using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Repositories;

public sealed class ProcedimentoRepository : IProcedimentoRepository
{
    private readonly SchedulingDbContext _context;

    public ProcedimentoRepository(SchedulingDbContext context) => _context = context;

    public Task<Procedimento?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Procedimentos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Procedimento procedimento, CancellationToken ct = default)
        => await _context.Procedimentos.AddAsync(procedimento, ct);

    public async Task<IReadOnlyList<Procedimento>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Procedimentos.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.Ativo);

        return await query.OrderBy(p => p.Nome).ToListAsync(ct);
    }
}
