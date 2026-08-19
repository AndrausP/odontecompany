using Contracts.Abstractions.Pagination;
using Estoque.Application.Interfaces;
using Estoque.Application.Mapping;
using Estoque.Contracts;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Queries.ListItensEstoque;

public sealed class ListItensEstoqueQueryHandler : IRequestHandler<ListItensEstoqueQuery, Result<PagedResult<ItemEstoqueDto>>>
{
    private readonly IItemEstoqueRepository _itemRepository;

    public ListItensEstoqueQueryHandler(IItemEstoqueRepository itemRepository) => _itemRepository = itemRepository;

    public async Task<Result<PagedResult<ItemEstoqueDto>>> Handle(ListItensEstoqueQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _itemRepository.ListAsync(
            request.BranchId, request.IncludeInactive, request.Pagination.Page, request.Pagination.PageSize, cancellationToken);

        var dtos = items.Select(i => i.ToDto()).ToList();

        return Result.Success(PagedResult<ItemEstoqueDto>.Create(dtos, totalCount, request.Pagination.Page, request.Pagination.PageSize));
    }
}
