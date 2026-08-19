using MediatR;
using SharedKernel;

namespace Tenancy.Application.Commands.DeactivateBranch;

public sealed record DeactivateBranchCommand(Guid Id) : IRequest<Result>;
