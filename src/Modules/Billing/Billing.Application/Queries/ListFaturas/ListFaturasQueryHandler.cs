using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Contracts.Abstractions.Pagination;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.ListFaturas;

public sealed class ListFaturasQueryHandler : IRequestHandler<ListFaturasQuery, Result<PagedResult<FaturaDto>>>
{
    private readonly IFaturaRepository _faturaRepository;

    public ListFaturasQueryHandler(IFaturaRepository faturaRepository) => _faturaRepository = faturaRepository;

    public async Task<Result<PagedResult<FaturaDto>>> Handle(ListFaturasQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _faturaRepository.ListAsync(
            request.PacienteId, request.Status, request.Pagination.Page, request.Pagination.PageSize, cancellationToken);

        var dtos = items.Select(f => f.ToDto()).ToList();

        return Result.Success(PagedResult<FaturaDto>.Create(dtos, totalCount, request.Pagination.Page, request.Pagination.PageSize));
    }
}
