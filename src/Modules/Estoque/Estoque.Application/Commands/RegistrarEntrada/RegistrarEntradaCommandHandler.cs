using Estoque.Application.Interfaces;
using Estoque.Application.Mapping;
using Estoque.Contracts;
using Estoque.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Estoque.Application.Commands.RegistrarEntrada;

public sealed class RegistrarEntradaCommandHandler : IRequestHandler<RegistrarEntradaCommand, Result<ItemEstoqueDto>>
{
    private readonly IItemEstoqueRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarEntradaCommandHandler(IItemEstoqueRepository itemRepository, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ItemEstoqueDto>> Handle(RegistrarEntradaCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<ItemEstoqueDto>(DomainErrors.ItemEstoque.NaoEncontrado);

        var result = item.RegistrarEntrada(request.Quantidade);
        if (result.IsFailure)
            return Result.Failure<ItemEstoqueDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(item.ToDto());
    }
}
