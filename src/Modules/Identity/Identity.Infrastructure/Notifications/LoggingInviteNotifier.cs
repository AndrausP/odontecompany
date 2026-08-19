using Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Notifications;

/// <summary>
/// Implementação de referência de <see cref="IInviteNotifier"/> — NÃO envia email nenhum, só
/// loga o token gerado (task 016, item 6). Provider real (SendGrid/SES/etc) é task futura fora
/// desta sprint; a porta já isola o resto do fluxo (persistência, RBAC, testes) da integração
/// real quando ela existir — trocar a implementação no DI, sem tocar Application/Domain.
///
/// Dívida nomeada: logar o token em CLARO só é aceitável aqui porque é o único canal de entrega
/// disponível neste ambiente (dev/test, sem provider real). Um provider real de produção NUNCA
/// deve logar o valor em claro de um segredo de convite.
/// </summary>
public sealed class LoggingInviteNotifier : IInviteNotifier
{
    private readonly ILogger<LoggingInviteNotifier> _logger;

    public LoggingInviteNotifier(ILogger<LoggingInviteNotifier> logger) => _logger = logger;

    public Task SendInviteAsync(string email, string plainToken, Guid organizationId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Convite gerado para {Email} na organization {OrganizationId} — token (sem provider de email real, ver Identity.Infrastructure.Notifications.LoggingInviteNotifier): {Token}",
            email, organizationId, plainToken);

        return Task.CompletedTask;
    }
}
