using MediatR;
using SharedKernel;
using Subscriptions.Application.Interfaces;
using Subscriptions.Application.Mapping;
using Subscriptions.Contracts;

namespace Subscriptions.Application.Queries.GetCurrentSubscription;

public sealed class GetCurrentSubscriptionQueryHandler : IRequestHandler<GetCurrentSubscriptionQuery, Result<SubscriptionDto?>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetCurrentSubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionDto?>> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        return Result.Success(subscription?.ToDto());
    }
}
