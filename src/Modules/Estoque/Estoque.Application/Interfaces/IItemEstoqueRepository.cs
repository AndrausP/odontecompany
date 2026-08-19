using Estoque.Domain.Entities;

namespace Estoque.Application.Interfaces;

public interface IItemEstoqueRepository
{
    Task<ItemEstoque?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ItemEstoque item, CancellationToken ct = default);
    Task<(IReadOnlyList<ItemEstoque> Items, int TotalCount)> ListAsync(
        Guid? branchId, bool includeInactive, int page, int pageSize, CancellationToken ct = default);
}
