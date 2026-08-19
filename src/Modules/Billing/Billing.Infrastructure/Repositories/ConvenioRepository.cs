using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Repositories;

public sealed class ConvenioRepository : IConvenioRepository
{
    private readonly BillingDbContext _context;

    public ConvenioRepository(BillingDbContext context) => _context = context;

    public Task<Convenio?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Convenios.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Convenio convenio, CancellationToken ct = default)
        => await _context.Convenios.AddAsync(convenio, ct);

    public async Task<IReadOnlyList<Convenio>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Convenios.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.Ativo);

        return await query.OrderBy(c => c.Nome).ToListAsync(ct);
    }
}
