using Estoque.Application.Interfaces;
using Estoque.Application.Mapping;
using Estoque.Contracts;
using Estoque.Domain.Entities;
using Estoque.Domain.Errors;
using MediatR;
using SharedKernel;
using Tenancy.Contracts;

namespace Estoque.Application.Commands.CreateItemEstoque;

public sealed class CreateItemEstoqueCommandHandler : IRequestHandler<CreateItemEstoqueCommand, Result<ItemEstoqueDto>>
{
    private readonly IItemEstoqueRepository _itemRepository;
    private readonly IBranchLookup _branchLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateItemEstoqueCommandHandler(IItemEstoqueRepository itemRepository, IBranchLookup branchLookup, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _branchLookup = branchLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ItemEstoqueDto>> Handle(CreateItemEstoqueCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId is not null && !await _branchLookup.ExistsAsync(request.OrganizationId, request.BranchId.Value, cancellationToken))
            return Result.Failure<ItemEstoqueDto>(DomainErrors.ItemEstoque.BranchInvalida);

        var itemResult = ItemEstoque.Create(request.OrganizationId, request.BranchId, request.Nome, request.UnidadeMedida, request.QuantidadeMinima);
        if (itemResult.IsFailure)
            return Result.Failure<ItemEstoqueDto>(itemResult.Error);

        var item = itemResult.Value;

        await _itemRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(item.ToDto());
    }
}
