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
        if (branch is null)
            return Result.Failure(DomainErrors.Branch.NaoEncontrada);

        branch.Desativar();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
