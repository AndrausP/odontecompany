using MediatR;
using SharedKernel;
using Subscriptions.Application.Interfaces;
using Subscriptions.Application.Mapping;
using Subscriptions.Contracts;
using Subscriptions.Domain.Entities;

namespace Subscriptions.Application.Commands.SelectPlan;

/// <summary>
/// Upsert: primeira escolha de plano (onboarding) cria a Subscription; escolher de novo depois
/// (upgrade/downgrade, tela de configurações) troca o Tier da mesma linha — nunca duas
/// Subscriptions ativas pra mesma organization (índice único em `OrganizationId`, ver
/// `SubscriptionConfiguration`). Sem chamada ao `IPaymentGatewayService` aqui de propósito: esta
/// rodada não cobra de verdade (esqueleto), plano vira ativo direto — checkout real fica no
/// endpoint separado `POST /api/subscriptions/checkout`.
/// </summary>
public sealed class SelectPlanCommandHandler : IRequestHandler<SelectPlanCommand, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SelectPlanCommandHandler(ISubscriptionRepository subscriptionRepository, IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SubscriptionDto>> Handle(SelectPlanCommand request, CancellationToken cancellationToken)
    {
        var existing = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);

        if (existing is not null)
        {
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
