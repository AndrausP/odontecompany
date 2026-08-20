using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListProcedimentos;

public sealed record ListProcedimentosQuery(bool IncludeInactive) : IRequest<Result<IReadOnlyList<ProcedimentoDto>>>;
