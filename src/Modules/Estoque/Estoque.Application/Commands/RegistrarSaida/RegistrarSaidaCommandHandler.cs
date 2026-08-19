using Estoque.Application.Interfaces;
using Estoque.Application.Mapping;
using Estoque.Contracts;
using Estoque.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Commands.RegistrarSaida;

public sealed class RegistrarSaidaCommandHandler : IRequestHandler<RegistrarSaidaCommand, Result<ItemEstoqueDto>>
{
    private readonly IItemEstoqueRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarSaidaCommandHandler(IItemEstoqueRepository itemRepository, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ItemEstoqueDto>> Handle(RegistrarSaidaCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<ItemEstoqueDto>(DomainErrors.ItemEstoque.NaoEncontrado);

        var result = item.RegistrarSaida(request.Quantidade);
        if (result.IsFailure)
            return Result.Failure<ItemEstoqueDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(item.ToDto());
    }
}
