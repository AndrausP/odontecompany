using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetReceitaPorProcedimento;

public sealed class GetReceitaPorProcedimentoQueryHandler : IRequestHandler<GetReceitaPorProcedimentoQuery, Result<IReadOnlyList<ReceitaProcedimentoDto>>>
{
    private readonly IReceitaPorProcedimentoProvider _receitaPorProcedimentoProvider;

    public GetReceitaPorProcedimentoQueryHandler(IReceitaPorProcedimentoProvider receitaPorProcedimentoProvider)
        => _receitaPorProcedimentoProvider = receitaPorProcedimentoProvider;

    public async Task<Result<IReadOnlyList<ReceitaProcedimentoDto>>> Handle(GetReceitaPorProcedimentoQuery request, CancellationToken cancellationToken)
    {
        var receita = await _receitaPorProcedimentoProvider.ObterAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);
        return Result.Success(receita);
    }
}
