using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.UpdateSala;

public sealed record UpdateSalaCommand(Guid SalaId, Guid OrganizationId, string Nome, int? CapacidadeMaxima, Guid? BranchId)
    : IRequest<Result<SalaDto>>;
