using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Patients.Application.Interfaces;
using Patients.Contracts;
using Patients.Infrastructure.Lookups;
using Patients.Infrastructure.Persistence;
using Patients.Infrastructure.Repositories;

namespace Patients.Infrastructure;

/// <summary>Ponto único de registro do módulo Patients no container de DI do host.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPatientsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PatientsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PatientsDb")));

        // IOrganizationContext (Infrastructure.Common.Tenancy) NÃO é registrado aqui de propósito —
        // já é registrado uma vez por AddIdentityInfrastructure (Program.cs chama os dois
        // módulos no mesmo host). Registrar de novo aqui não quebra nada funcionalmente (mesma
        // implementação concreta), mas é registro duplicado sem necessidade — se este módulo um
        // dia rodar sem o Identity no mesmo host, registre IOrganizationContext explicitamente antes
        // de chamar AddPatientsInfrastructure.
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PatientsDbContext>());

        // Porta pública pra outros módulos (ex: Scheduling.Application) checarem paciente sem
        // depender de Patients.Domain/Patients.Infrastructure — ver IPatientLookup.
        services.AddScoped<IPatientLookup, PatientLookup>();

        // Porta de leitura agregada pro módulo Reporting (task 008) compor dashboards.
        services.AddScoped<IPatientSummaryProvider, PatientSummaryProvider>();

        return services;
    }
}
