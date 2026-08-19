using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenancy.Application.Interfaces;
using Tenancy.Contracts;
using Tenancy.Infrastructure.Lookups;
using Tenancy.Infrastructure.Persistence;
using Tenancy.Infrastructure.Repositories;

namespace Tenancy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenancyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TenancyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TenancyDb")));

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TenancyDbContext>());
        services.AddScoped<IBranchLookup, BranchLookup>();

        return services;
    }
}
