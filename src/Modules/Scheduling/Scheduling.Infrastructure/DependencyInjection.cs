using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Interfaces;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Caching;
using Scheduling.Infrastructure.Lookups;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Scheduling.Infrastructure;

/// <summary>Ponto único de registro do módulo Scheduling no container de DI do host.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SchedulingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SchedulingDb")));

        // IOrganizationContext (Infrastructure.Common.Tenancy) NÃO é registrado aqui de propósito —
        // já é registrado uma vez por AddIdentityInfrastructure (mesmo racional documentado em
        // Patients.Infrastructure.DependencyInjection).

        // IConnectionMultiplexer singleton — AbortOnConnectFail=false é o que garante que a API
        // sobe mesmo sem Redis disponível (dev local sem o container, CI sem infra externa):
        // a conexão só é de fato usada (e só pode falhar) em runtime, dentro de
        // RedisLockService/RedisAvailabilityCache — nunca no startup do host.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Redis") ?? "localhost:6379";
            var redisOptions = ConfigurationOptions.Parse(connectionString);
            redisOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisOptions);
        });

        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IProfissionalRepository, ProfissionalRepository>();
        services.AddScoped<ISalaRepository, SalaRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SchedulingDbContext>());

        services.AddScoped<IRedisLockService, RedisLockService>();
        services.AddScoped<IAvailabilityCache, RedisAvailabilityCache>();

        // Porta de leitura agregada pro módulo Reporting (task 008) compor dashboards.
        services.AddScoped<IAgendaSummaryProvider, AgendaSummaryProvider>();

        // Porta de leitura de profissionais (task 022) — consumida pelo módulo Reporting
        // (task 023) pra montar o relatório de comissão sem JOIN cruzado.
        services.AddScoped<IProfissionalLookup, ProfissionalLookup>();

        return services;
    }
}
