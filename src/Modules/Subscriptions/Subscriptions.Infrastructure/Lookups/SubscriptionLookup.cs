using Microsoft.EntityFrameworkCore;
using Subscriptions.Contracts;
using Subscriptions.Domain;
using Subscriptions.Domain.Entities;
using Subscriptions.Domain.Enums;
using Subscriptions.Infrastructure.Persistence;

namespace Subscriptions.Infrastructure.Lookups;

public sealed class SubscriptionLookup : ISubscriptionLookup
{
    private readonly SubscriptionsDbContext _context;

    public SubscriptionLookup(SubscriptionsDbContext context) => _context = context;

    public async Task<SubscriptionDto?> ObterAtivaAsync(Guid organizationId, CancellationToken ct = default)
    {
        var subscription = await _context.Subscriptions.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Status == SubscriptionStatus.Ativa, ct);

        return subscription is null ? null : ToDto(subscription);
    }

    public async Task<int> LimiteDeFiliaisAsync(Guid organizationId, CancellationToken ct = default)
    {
        var subscription = await _context.Subscriptions.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Status == SubscriptionStatus.Ativa, ct);

        return subscription is null ? 0 : PlanCatalog.Get(subscription.Tier).LimiteFiliais;
    }

    private static SubscriptionDto ToDto(Subscription s) => new(s.Id, s.OrganizationId, s.Tier.ToString(), s.Status.ToString(), s.CreatedAt);
}
