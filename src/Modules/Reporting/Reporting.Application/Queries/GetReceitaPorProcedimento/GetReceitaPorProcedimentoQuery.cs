using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetReceitaPorProcedimento;

public sealed record GetReceitaPorProcedimentoQuery(Guid OrganizationId, DateTime DataInicio, DateTime DataFim)
    : IRequest<Result<IReadOnlyList<ReceitaProcedimentoDto>>>;
