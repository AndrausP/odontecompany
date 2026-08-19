using Estoque.Application.Interfaces;
using Estoque.Domain.Entities;
using Estoque.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Infrastructure.Repositories;

public sealed class ItemEstoqueRepository : IItemEstoqueRepository
{
    private readonly EstoqueDbContext _context;

    public ItemEstoqueRepository(EstoqueDbContext context) => _context = context;

    public Task<ItemEstoque?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.ItensEstoque.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(ItemEstoque item, CancellationToken ct = default)
        => await _context.ItensEstoque.AddAsync(item, ct);

    public async Task<(IReadOnlyList<ItemEstoque> Items, int TotalCount)> ListAsync(
        Guid? branchId, bool includeInactive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ItensEstoque.AsNoTracking().AsQueryable();

        if (branchId is not null)
            query = query.Where(i => i.BranchId == branchId.Value);

        if (!includeInactive)
            query = query.Where(i => i.Ativo);

        query = query.OrderBy(i => i.Nome);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
