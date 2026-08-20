using MediatR;
using SharedKernel;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Errors;

namespace Tenancy.Application.Commands.DeactivateBranch;

public sealed class DeactivateBranchCommandHandler : IRequestHandler<DeactivateBranchCommand, Result>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.Id, cancellationToken);
        // Mesmo retorno pra branch inexistente E pra branch de outra organization (task 040) —
        // não diferenciar os dois evita confirmar pra quem tenta o IDOR que aquele guid existe.
        if (branch is null || branch.OrganizationId != request.OrganizationId)
            return Result.Failure(DomainErrors.Branch.NaoEncontrada);

        branch.Desativar();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
