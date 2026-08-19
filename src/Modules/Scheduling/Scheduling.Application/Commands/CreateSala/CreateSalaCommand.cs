using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Commands.CreateSala;

public sealed record CreateSalaCommand(Guid OrganizationId, string Nome, int? CapacidadeMaxima, Guid? BranchId) : IRequest<Result<SalaDto>>;
