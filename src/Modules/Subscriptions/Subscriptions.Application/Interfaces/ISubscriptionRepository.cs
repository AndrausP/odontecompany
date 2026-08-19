using Subscriptions.Domain.Entities;

namespace Subscriptions.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(Subscription subscription, CancellationToken ct = default);
}
