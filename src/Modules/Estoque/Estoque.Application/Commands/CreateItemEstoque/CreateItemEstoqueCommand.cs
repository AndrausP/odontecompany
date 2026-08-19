using Estoque.Contracts;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Commands.CreateItemEstoque;

public sealed record CreateItemEstoqueCommand(
    Guid OrganizationId, Guid? BranchId, string Nome, string UnidadeMedida, decimal QuantidadeMinima
) : IRequest<Result<ItemEstoqueDto>>;
