using MediatR;
using SharedKernel;
using Subscriptions.Application.Interfaces;
using Subscriptions.Application.Mapping;
using Subscriptions.Contracts;
using Subscriptions.Domain;
using Subscriptions.Domain.Entities;
using Subscriptions.Domain.Errors;
using Tenancy.Contracts;

namespace Subscriptions.Application.Commands.SelectPlan;

/// <summary>
/// Upsert: primeira escolha de plano (onboarding) cria a Subscription; escolher de novo depois
/// (upgrade/downgrade, tela de configurações) troca o Tier da mesma linha — nunca duas
/// Subscriptions ativas pra mesma organization (índice único em `OrganizationId`, ver
/// `SubscriptionConfiguration`). Sem chamada ao `IPaymentGatewayService` aqui de propósito: esta
/// rodada não cobra de verdade (esqueleto), plano vira ativo direto — checkout real fica no
/// endpoint separado `POST /api/subscriptions/checkout`.
///
/// Downgrade valida limite de filial (auditoria pré-venda — fecha a dívida nomeada na task 039):
/// só entra em jogo quando JÁ existe subscription (troca de tier), nunca na primeira escolha
/// (organização recém-criada não tem filial nenhuma ainda).
/// </summary>
public sealed class SelectPlanCommandHandler : IRequestHandler<SelectPlanCommand, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IBranchLookup _branchLookup;
    private readonly IUnitOfWork _unitOfWork;

    public SelectPlanCommandHandler(ISubscriptionRepository subscriptionRepository, IBranchLookup branchLookup, IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _branchLookup = branchLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SubscriptionDto>> Handle(SelectPlanCommand request, CancellationToken cancellationToken)
    {
        var existing = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);

        if (existing is not null)
        {
            var novoLimite = PlanCatalog.Get(request.Tier).LimiteFiliais;
            var filiaisAtivas = await _branchLookup.CountAtivasAsync(request.OrganizationId, cancellationToken);
            if (filiaisAtivas > novoLimite)
                return Result.Failure<SubscriptionDto>(DomainErrors.Subscription.DowngradeExcedeFiliaisAtivas);

            existing.TrocarPlano(request.Tier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto());
        }

        var subscriptionResult = Subscription.Create(request.OrganizationId, request.Tier);
        if (subscriptionResult.IsFailure)
            return Result.Failure<SubscriptionDto>(subscriptionResult.Error);

        var subscription = subscriptionResult.Value;
        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(subscription.ToDto());
    }
}
