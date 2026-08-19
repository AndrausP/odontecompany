using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.ListConvenios;

public sealed class ListConveniosQueryHandler : IRequestHandler<ListConveniosQuery, Result<IReadOnlyList<ConvenioDto>>>
{
    private readonly IConvenioRepository _convenioRepository;

    public ListConveniosQueryHandler(IConvenioRepository convenioRepository) => _convenioRepository = convenioRepository;

    public async Task<Result<IReadOnlyList<ConvenioDto>>> Handle(ListConveniosQuery request, CancellationToken cancellationToken)
    {
        var convenios = await _convenioRepository.ListAsync(request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<ConvenioDto>>(convenios.Select(c => c.ToDto()).ToList());
    }
}
