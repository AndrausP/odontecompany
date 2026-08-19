using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListSalas;

public sealed record ListSalasQuery(bool IncludeInactive) : IRequest<Result<IReadOnlyList<SalaDto>>>;
