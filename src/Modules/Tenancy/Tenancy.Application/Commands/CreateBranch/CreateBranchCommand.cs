using MediatR;
using SharedKernel;
using Tenancy.Contracts;

namespace Tenancy.Application.Commands.CreateBranch;

public sealed record CreateBranchCommand(Guid OrganizationId, string Nome, string? Endereco, string? Telefone = null) : IRequest<Result<BranchDto>>;
