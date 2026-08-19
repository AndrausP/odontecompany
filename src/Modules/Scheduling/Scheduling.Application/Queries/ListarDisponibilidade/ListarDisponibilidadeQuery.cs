using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListarDisponibilidade;

public sealed record ListarDisponibilidadeQuery(
    Guid OrganizationId,
    Guid ProfissionalId,
    DateOnly Data
) : IRequest<Result<IReadOnlyList<AvailabilitySlotDto>>>;
