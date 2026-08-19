using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListProfissionais;

public sealed record ListProfissionaisQuery(bool IncludeInactive) : IRequest<Result<IReadOnlyList<ProfissionalDto>>>;
