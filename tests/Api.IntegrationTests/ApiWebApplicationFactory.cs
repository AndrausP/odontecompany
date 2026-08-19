using Billing.Infrastructure.Persistence;
using Estoque.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Patients.Infrastructure.Persistence;
using Records.Infrastructure.Persistence;
using Scheduling.Infrastructure.Persistence;
using Subscriptions.Infrastructure.Persistence;
using Tenancy.Infrastructure.Persistence;

namespace Api.IntegrationTests;

/// <summary>
/// Task 021 — sobe <c>OdontoPlatform.Api</c> inteira in-process (<see cref="WebApplicationFactory{TEntryPoint}"/>),
/// com os 8 <c>DbContext</c> (um por módulo — Subscriptions somado na sprint-11) trocados de Postgres real pra
/// <c>Microsoft.EntityFrameworkCore.InMemory</c>. Decisão do Architect (docs/tasks/021, docs/decisions.md):
/// InMemory, não Postgres efêmero — o objetivo aqui é exercitar o PIPELINE (auth, policies, rate
/// limiting, routing), não fidelidade de SQL/constraints; Postgres efêmero exigiria infraestrutura
/// de CI que este repo ainda não tem, sem ganho pros 5 casos-alvo desta task.
///
/// Ambiente forçado pra <c>"Testing"</c> (não <c>"Development"</c>): pula o seed automático do
/// admin padrão (`Program.cs`, `if (app.Environment.IsDevelopment())`) — cada teste constrói só o
/// dado que precisa via HTTP real (signup/login/criar organization/convite), sem depender de
/// estado implícito de seed.
///
/// 1 instância de factory = 1 banco InMemory isolado (nome único por instância, sufixado com
/// <see cref="Guid.NewGuid"/>) — testes que compartilham a mesma instância de factory (mesma
/// classe de teste, `[SetUp]`/campo de instância) compartilham dado; instâncias diferentes nunca
/// se veem.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbSuffix = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Reduz ruído no console do `dotnet test` (EF Core loga cada SaveChanges, ASP.NET loga
        // cada request) — Warning só deixa passar o que importa; suba pra Debug na mão se for
        // depurar uma falha de auth/pipeline específica.
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));

        builder.ConfigureServices(services =>
        {
            ReplaceWithInMemory<IdentityDbContext>(services, nameof(IdentityDbContext));
            ReplaceWithInMemory<PatientsDbContext>(services, nameof(PatientsDbContext));
            ReplaceWithInMemory<SchedulingDbContext>(services, nameof(SchedulingDbContext));
            ReplaceWithInMemory<RecordsDbContext>(services, nameof(RecordsDbContext));
            ReplaceWithInMemory<BillingDbContext>(services, nameof(BillingDbContext));
            ReplaceWithInMemory<TenancyDbContext>(services, nameof(TenancyDbContext));
            ReplaceWithInMemory<EstoqueDbContext>(services, nameof(EstoqueDbContext));
            ReplaceWithInMemory<SubscriptionsDbContext>(services, nameof(SubscriptionsDbContext));
        });
    }

    private void ReplaceWithInMemory<TContext>(IServiceCollection services, string contextName)
        where TContext : DbContext
    {
        // Remove TODOS os descritores que `AddDbContext<TContext>` registrou (options + o
        // próprio DbContext) — remover só `DbContextOptions<TContext>` deixa a configuração
        // `UseNpgsql` original resolvendo em paralelo com a nova em alguns cenários de DI.
        var descriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>) || d.ServiceType == typeof(TContext))
            .ToList();
        foreach (var descriptor in descriptors)
            services.Remove(descriptor);

        services.AddDbContext<TContext>(options => options.UseInMemoryDatabase($"{contextName}-{_dbSuffix}"));
    }
}
