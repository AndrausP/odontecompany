using MediatR;
using SharedKernel;
using Subscriptions.Contracts;
using Tenancy.Application.Exceptions;
using Tenancy.Application.Interfaces;
using Tenancy.Application.Mapping;
using Tenancy.Contracts;
using Tenancy.Domain.Entities;
using Tenancy.Domain.Errors;

namespace Tenancy.Application.Commands.CreateBranch;

/// <summary>
/// Limite de filial por plano (sprint-11) — conta branches ATIVAS antes de criar mais uma;
/// `ISubscriptionLookup.LimiteDeFiliaisAsync` devolve 0 quando a organização não tem assinatura
/// ativa (bloqueia igual a estourar o limite — sem plano, zero filial nova). Cross-module só via
/// Subscriptions.Contracts, nunca Subscriptions.Domain/Infrastructure (mesma fronteira de
/// IBranchLookup/IPatientLookup).
/// </summary>
public sealed class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly ISubscriptionLookup _subscriptionLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(IBranchRepository branchRepository, ISubscriptionLookup subscriptionLookup, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _subscriptionLookup = subscriptionLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branchResult = Branch.Create(request.OrganizationId, request.Nome, request.Endereco, request.Telefone);
        if (branchResult.IsFailure)
            return Result.Failure<BranchDto>(branchResult.Error);

        var limiteFiliais = await _subscriptionLookup.LimiteDeFiliaisAsync(request.OrganizationId, cancellationToken);
        var filiaisAtivas = await _branchRepository.ListByOrganizationAsync(request.OrganizationId, includeInactive: false, cancellationToken);
        if (filiaisAtivas.Count >= limiteFiliais)
            return Result.Failure<BranchDto>(DomainErrors.Branch.LimiteDoPlanoAtingido);

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
