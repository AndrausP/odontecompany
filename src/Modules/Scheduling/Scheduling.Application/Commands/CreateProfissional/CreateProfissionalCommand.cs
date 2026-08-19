using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateProfissional;

public sealed record CreateProfissionalCommand(
    Guid OrganizationId, string Nome, string Especialidade, Guid? UserId, Guid? BranchId
) : IRequest<Result<ProfissionalDto>>;
