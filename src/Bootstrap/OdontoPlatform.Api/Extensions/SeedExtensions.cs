using Identity.Application.Commands.SeedFirstAdmin;
using Identity.Application.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace OdontoPlatform.Api.Extensions;

public static class SeedExtensions
{
    /// <summary>
    /// Executa o seed do primeiro organization + admin. Idempotente: se já existe algum organization,
    /// o handler recusa e nada é alterado. Credenciais vêm de configuração (appsettings/secrets)
    /// — os valores default em <see cref="AuthOptions"/> servem só pra ambiente local.
    /// </summary>
    public static async Task RunIdentitySeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var authOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

        var command = new SeedFirstAdminCommand(
            authOptions.DefaultOrganizationName,
            authOptions.DefaultAdminNome,
            authOptions.DefaultAdminEmail,
            authOptions.DefaultAdminPassword);

        var result = await mediator.Send(command);

        if (result.IsSuccess)
        {
            logger.LogWarning(
                "Seed executado: organization '{Organization}' criado, admin '{Email}' — TROQUE a senha padrão antes de ir pra produção.",
                result.Value.OrganizationNome,
                result.Value.AdminEmail);
        }
        else
        {
            logger.LogInformation("Seed não executado: {Erro}", result.Error.Message);
        }
    }
}
