using Estoque.Contracts;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Commands.RegistrarEntrada;

public sealed record RegistrarEntradaCommand(Guid ItemId, decimal Quantidade) : IRequest<Result<ItemEstoqueDto>>;
