using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Billing.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Billing.Application.Queries.GetFaturaById;

public sealed class GetFaturaByIdQueryHandler : IRequestHandler<GetFaturaByIdQuery, Result<FaturaDto>>
{
    private readonly IFaturaRepository _faturaRepository;

    public GetFaturaByIdQueryHandler(IFaturaRepository faturaRepository) => _faturaRepository = faturaRepository;

    public async Task<Result<FaturaDto>> Handle(GetFaturaByIdQuery request, CancellationToken cancellationToken)
    {
        var fatura = await _faturaRepository.GetByIdAsync(request.Id, cancellationToken);
        return fatura is null
            ? Result.Failure<FaturaDto>(DomainErrors.Fatura.NaoEncontrada)
            : Result.Success(fatura.ToDto());
    }
}
