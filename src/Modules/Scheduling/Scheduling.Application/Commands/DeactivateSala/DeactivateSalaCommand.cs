using MediatR;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateSala;

public sealed record DeactivateSalaCommand(Guid SalaId, Guid OrganizationId) : IRequest<Result>;
