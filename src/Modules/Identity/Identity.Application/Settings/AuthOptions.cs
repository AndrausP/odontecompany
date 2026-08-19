namespace Identity.Application.Settings;

/// <summary>
/// Configuração do módulo Identity não relacionada a segredo de assinatura (isso fica em
/// JwtSettings, na Infrastructure). Valores default abaixo servem só pra ambiente local —
/// em produção, sobrescrever via appsettings/secrets, nunca hardcode em texto puro.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int RefreshTokenExpirationDays { get; init; } = 7;

    /// <summary>Validade padrão de um convite de afiliação (task 016) — configurável, default 7 dias.</summary>
    public int InviteExpirationDays { get; init; } = 7;

    public string DefaultOrganizationName { get; init; } = "Clínica Demo";
    public string DefaultAdminNome { get; init; } = "Administrador";
    public string DefaultAdminEmail { get; init; } = "admin@clinicademo.local";
    public string DefaultAdminPassword { get; init; } = "Admin@123456";
}
