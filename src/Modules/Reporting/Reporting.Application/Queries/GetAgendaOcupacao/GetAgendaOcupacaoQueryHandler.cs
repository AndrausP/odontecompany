using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetAgendaOcupacao;

public sealed class GetAgendaOcupacaoQueryHandler : IRequestHandler<GetAgendaOcupacaoQuery, Result<AgendaResumoDto>>
{
    private readonly IAgendaSummaryProvider _agendaSummaryProvider;

    public GetAgendaOcupacaoQueryHandler(IAgendaSummaryProvider agendaSummaryProvider) => _agendaSummaryProvider = agendaSummaryProvider;

    public async Task<Result<AgendaResumoDto>> Handle(GetAgendaOcupacaoQuery request, CancellationToken cancellationToken)
    {
        var resumo = await _agendaSummaryProvider.ObterResumoAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);
        return Result.Success(resumo);
    }
}
