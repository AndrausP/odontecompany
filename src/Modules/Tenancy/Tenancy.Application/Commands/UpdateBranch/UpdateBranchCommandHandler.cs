using MediatR;
using SharedKernel;
using Tenancy.Application.Interfaces;
using Tenancy.Application.Mapping;
using Tenancy.Contracts;
using Tenancy.Domain.Errors;

namespace Tenancy.Application.Commands.UpdateBranch;

public sealed class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, Result<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BranchDto>> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        // Mesmo retorno de "não encontrada" pra branch inexistente E pra branch de outra
        // organization — não diferenciar os dois evita confirmar pra quem está tentando o IDOR
        // que aquele guid existe em algum lugar (task 040).
        if (branch is null || branch.OrganizationId != request.OrganizationId)
            return Result.Failure<BranchDto>(DomainErrors.Branch.NaoEncontrada);

        var updateResult = branch.AtualizarDados(request.Nome, request.Endereco, request.Telefone);
        if (updateResult.IsFailure)
            return Result.Failure<BranchDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(branch.ToDto());
    }
}
