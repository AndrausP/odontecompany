using MediatR;
using SharedKernel;
using Tenancy.Application.Exceptions;
using Tenancy.Application.Interfaces;
using Tenancy.Application.Mapping;
using Tenancy.Contracts;
using Tenancy.Domain.Entities;

namespace Tenancy.Application.Commands.CreateBranch;

public sealed class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branchResult = Branch.Create(request.OrganizationId, request.Nome, request.Endereco);
        if (branchResult.IsFailure)
            return Result.Failure<BranchDto>(branchResult.Error);

        var branch = branchResult.Value;

        await _branchRepository.AddAsync(branch, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<BranchDto>(Tenancy.Domain.Errors.DomainErrors.Branch.NomeJaCadastrado);
        }

        return Result.Success(branch.ToDto());
    }
}
