using Billing.Application.Interfaces;
using Billing.Contracts;
using Billing.Infrastructure.Adapters;
using Billing.Infrastructure.Messaging;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BillingDb")));

        services.AddScoped<IFaturaRepository, FaturaRepository>();
        services.AddScoped<IConvenioRepository, ConvenioRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BillingDbContext>());

        // Adapter de referência (sem integração real) — trocar por adaptador por convênio
        // quando houver integração de verdade, sem tocar Application/Domain.
        services.AddScoped<IConvenioAdapter, ManualConvenioAdapter>();

        // Tradutor evento→comando, pronto pra ser chamado por um consumidor MassTransit real
        // quando o worker de Outbox existir (ver Messaging/ConsultaConcluidaEventHandler.cs).
        services.AddScoped<ConsultaConcluidaEventHandler>();

        // Porta de leitura agregada pro módulo Reporting (task 008) compor dashboards.
        services.AddScoped<IFaturamentoSummaryProvider, FaturamentoSummaryProvider>();

        // Porta de leitura de comissão por profissional (task 022) — consumida pelo módulo
        // Reporting (task 023) pra montar o relatório de comissão sem JOIN cruzado.
        services.AddScoped<IComissaoSummaryProvider, ComissaoSummaryProvider>();

        return services;
    }
}
