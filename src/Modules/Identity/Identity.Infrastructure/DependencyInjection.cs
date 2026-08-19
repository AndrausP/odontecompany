using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Contracts;
using Identity.Infrastructure.Lookups;
using Identity.Infrastructure.Notifications;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Security;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>Ponto único de registro do módulo Identity no container de DI do host.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("IdentityDb")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddScoped<IOrganizationContext, OrganizationContext>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        // Porta de leitura de afiliações (task 022) — consumida pelo módulo Reporting
        // (task 023) pra montar o relatório de comissão sem JOIN cruzado.
        services.AddScoped<IMembershipLookup, MembershipLookup>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, Sha256RefreshTokenGenerator>();
        services.AddSingleton<IInviteTokenGenerator, Sha256InviteTokenGenerator>();
        services.AddSingleton<IInviteNotifier, LoggingInviteNotifier>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
