using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subscriptions.Application.Interfaces;
using Subscriptions.Contracts;
using Subscriptions.Infrastructure.Lookups;
using Subscriptions.Infrastructure.Payments;
using Subscriptions.Infrastructure.Persistence;
using Subscriptions.Infrastructure.Repositories;

namespace Subscriptions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSubscriptionsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SubscriptionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SubscriptionsDb")));

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SubscriptionsDbContext>());
        services.AddScoped<ISubscriptionLookup, SubscriptionLookup>();
        services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();

        return services;
    }
}
