using Microsoft.EntityFrameworkCore;
using Subscriptions.Application.Interfaces;
using Subscriptions.Domain.Entities;
using Subscriptions.Infrastructure.Persistence;

namespace Subscriptions.Infrastructure.Repositories;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscriptionsDbContext _context;

    public SubscriptionRepository(SubscriptionsDbContext context) => _context = context;

    public Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct = default)
        => _context.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        => await _context.Subscriptions.AddAsync(subscription, ct);
}
