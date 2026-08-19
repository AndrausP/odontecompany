using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Repositories;

public sealed class FaturaRepository : IFaturaRepository
{
    private readonly BillingDbContext _context;

    public FaturaRepository(BillingDbContext context) => _context = context;

    public Task<Fatura?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Faturas.Include(f => f.Parcelas).FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExistsByAgendamentoIdAsync(Guid agendamentoId, CancellationToken ct = default)
        => _context.Faturas.AnyAsync(f => f.AgendamentoId == agendamentoId, ct);

    public async Task AddAsync(Fatura fatura, CancellationToken ct = default)
        => await _context.Faturas.AddAsync(fatura, ct);

    public async Task<(IReadOnlyList<Fatura> Items, int TotalCount)> ListAsync(
        Guid? pacienteId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Faturas.Include(f => f.Parcelas).AsQueryable();

        if (pacienteId is not null)
            query = query.Where(f => f.PacienteId == pacienteId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Enums.StatusFatura>(status, ignoreCase: true, out var statusEnum))
            query = query.Where(f => f.Status == statusEnum);

        query = query.OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
