using MediatR;
using SharedKernel;

namespace Scheduling.Application.Commands.DeactivateProcedimento;

public sealed record DeactivateProcedimentoCommand(Guid ProcedimentoId, Guid OrganizationId) : IRequest<Result>;
