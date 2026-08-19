using Estoque.Contracts;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Commands.RegistrarSaida;

public sealed record RegistrarSaidaCommand(Guid ItemId, decimal Quantidade) : IRequest<Result<ItemEstoqueDto>>;
