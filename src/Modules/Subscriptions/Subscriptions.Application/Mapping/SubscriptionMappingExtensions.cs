using Subscriptions.Contracts;
using Subscriptions.Domain.Entities;

namespace Subscriptions.Application.Mapping;

public static class SubscriptionMappingExtensions
{
    public static SubscriptionDto ToDto(this Subscription subscription) => new(
        subscription.Id,
        subscription.OrganizationId,
        subscription.Tier.ToString(),
        subscription.Status.ToString(),
        subscription.CreatedAt);
}
