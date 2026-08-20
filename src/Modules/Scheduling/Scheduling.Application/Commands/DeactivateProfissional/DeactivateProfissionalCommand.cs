using MediatR;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateProfissional;

public sealed record DeactivateProfissionalCommand(Guid ProfissionalId, Guid OrganizationId) : IRequest<Result>;
