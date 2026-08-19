namespace Identity.Infrastructure.Security;

/// <summary>
/// Configuração de assinatura/emissão do JWT — fica na Infrastructure porque carrega o
/// segredo (SigningKey), que é detalhe de infraestrutura, não de regra de negócio.
/// Valor default abaixo serve só pra dev local; produção deve sobrescrever via secrets/env.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OdontoPlatform";
    public string Audience { get; init; } = "OdontoPlatform.Clients";
    public string SigningKey { get; init; } = "dev-only-signing-key-troque-em-producao-min-32-chars!!";
    public int AccessTokenExpirationMinutes { get; init; } = 15;
}
