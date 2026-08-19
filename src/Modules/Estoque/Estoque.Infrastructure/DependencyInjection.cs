using Estoque.Application.Interfaces;
using Estoque.Infrastructure.Persistence;
using Estoque.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estoque.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEstoqueInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EstoqueDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EstoqueDb")));

        services.AddScoped<IItemEstoqueRepository, ItemEstoqueRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EstoqueDbContext>());

        return services;
    }
}
