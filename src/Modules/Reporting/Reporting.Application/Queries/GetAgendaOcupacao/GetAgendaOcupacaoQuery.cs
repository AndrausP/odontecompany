using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetAgendaOcupacao;

public sealed record GetAgendaOcupacaoQuery(Guid OrganizationId, DateTime DataInicio, DateTime DataFim) : IRequest<Result<AgendaResumoDto>>;
