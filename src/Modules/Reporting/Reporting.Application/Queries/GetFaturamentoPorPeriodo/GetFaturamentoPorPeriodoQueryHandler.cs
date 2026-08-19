using Billing.Contracts;
using MediatR;
using SharedKernel;

namespace Reporting.Application.Queries.GetFaturamentoPorPeriodo;

public sealed class GetFaturamentoPorPeriodoQueryHandler : IRequestHandler<GetFaturamentoPorPeriodoQuery, Result<FaturamentoResumoDto>>
{
    private readonly IFaturamentoSummaryProvider _faturamentoSummaryProvider;

    public GetFaturamentoPorPeriodoQueryHandler(IFaturamentoSummaryProvider faturamentoSummaryProvider)
        => _faturamentoSummaryProvider = faturamentoSummaryProvider;

    public async Task<Result<FaturamentoResumoDto>> Handle(GetFaturamentoPorPeriodoQuery request, CancellationToken cancellationToken)
    {
        var resumo = await _faturamentoSummaryProvider.ObterResumoAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);
        return Result.Success(resumo);
    }
}
