using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Records.Application.Interfaces;
using Records.Infrastructure.Audit;
using Records.Infrastructure.Persistence;
using Records.Infrastructure.Repositories;
using Records.Infrastructure.Security;
using Records.Infrastructure.Storage;

namespace Records.Infrastructure;

/// <summary>Ponto único de registro do módulo Records no container de DI do host.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRecordsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RecordsEncryptionOptions>(configuration.GetSection(RecordsEncryptionOptions.SectionName));
        services.Configure<LocalDiskStorageOptions>(configuration.GetSection(LocalDiskStorageOptions.SectionName));

        // IEncryptionService é Singleton (stateless, só deriva a chave uma vez) — é usado dentro
        // de RecordsDbContext.OnModelCreating, que roda uma vez por processo pro EF Core compilar
        // o Model, não por request. Ver comentário em AesEncryptionService.
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Fallback de dev — trocar por implementação de S3/Azure Blob em produção (ports-and-adapters).
        services.AddScoped<IObjectStorageService, LocalDiskObjectStorageService>();

        services.AddDbContext<RecordsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RecordsDb")));

        // IOrganizationContext (Infrastructure.Common.Tenancy) já é registrado uma vez por
        // AddIdentityInfrastructure — não repetido aqui, mesmo padrão de Patients/Scheduling.
        services.AddScoped<IProntuarioRepository, ProntuarioRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RecordsDbContext>());

        services.AddScoped<AuditLogWriter>();
        services.AddScoped<IAuditLogWriter>(sp => sp.GetRequiredService<AuditLogWriter>());
        services.AddScoped<IAuditLogReader>(sp => sp.GetRequiredService<AuditLogWriter>());

        return services;
    }
}
