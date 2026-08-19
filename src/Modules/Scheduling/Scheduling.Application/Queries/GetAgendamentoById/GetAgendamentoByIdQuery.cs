using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.GetAgendamentoById;

public sealed record GetAgendamentoByIdQuery(Guid Id) : IRequest<Result<AgendamentoDto>>;
