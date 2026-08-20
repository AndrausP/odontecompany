using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Application.DTOs;
using Identity.Domain.Enums;

namespace Api.IntegrationTests;

/// <summary>
/// Passos de autenticação/afiliação reusados por vários testes — sempre via HTTP real contra os
/// endpoints públicos (task 021: teste de integração, não atalho por repositório/handler). Cada
/// método devolve exatamente o DTO que o endpoint real devolve, sem reimplementar a lógica.
/// </summary>
internal static class AuthFlow
{
    /// <summary>
    /// Program.cs registra `JsonStringEnumConverter` globalmente (respostas serializam enum como
    /// string, ex.: `"role":"Dentista"`) — `HttpContent.ReadFromJsonAsync&lt;T&gt;()` SEM options
    /// explícitas usa por baixo dos panos <c>JsonSerializerDefaults.Web</c> (camelCase +
    /// case-insensitive), que já cobre casing mas não converte enum-como-string. Partir daqui
    /// (`new JsonSerializerOptions(JsonSerializerDefaults.Web)`) e só ADICIONAR o conversor —
    /// em vez de `new JsonSerializerOptions()` vazio — preserva o fallback "web" implícito que o
    /// `ReadFromJsonAsync` já dava de graça. Bug real achado sem isso: `new JsonSerializerOptions()`
    /// vazio é case-SENSITIVE por padrão; API serializa `camelCase` (`"accessToken"`), records
    /// daqui são `PascalCase` — nenhuma propriedade batia, todo campo virava o default do tipo
    /// (string → `null`) SEM lançar exceção nenhuma. Ver docs/knowledge/errors-aprendidos.md.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Email aleatório — evita colisão entre testes que rodam na mesma instância de factory/banco.</summary>
    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@teste.local";

    /// <summary>Cria um usuário novo (sem organization) com o email dado e devolve o resultado completo do signup.</summary>
    public static async Task<SignupResultDto> SignupAsync(HttpClient client, string email, string password = "Senha@12345")
    {
        var response = await client.PostAsJsonAsync("/api/auth/signup", new { Nome = "Usuário de Teste", Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignupResultDto>(JsonOptions))!;
    }

    public static async Task<LoginResultDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResultDto>(JsonOptions))!;
    }

    /// <summary>Cria uma organization com o token dado (o usuário do token vira Owner) — devolve o par de token já escopado.</summary>
    public static async Task<CreateOrganizationResultDto> CreateOrganizationAsync(HttpClient client, string accessToken, string? nome = null)
    {
        WithBearer(client, accessToken);
        var response = await client.PostAsJsonAsync("/api/organizations", new { Nome = nome ?? $"Clínica Teste {Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateOrganizationResultDto>(JsonOptions))!;
    }

    /// <summary>
    /// Escolhe um plano pra organization do token (sem cobrança real — ver `SelectPlanCommandHandler`).
    /// Necessário antes de convidar/aceitar convite desde a auditoria pré-venda (task 041+):
    /// `AcceptInviteCommandHandler` passou a checar limite de usuário por plano, e organização sem
    /// plano ativo tem limite 0 (mesmo raciocínio já usado pro limite de filial, task 039) — sem
    /// isso NENHUM convite seria aceito nos testes.
    /// </summary>
    public static async Task SelectPlanAsync(HttpClient client, string accessToken, string tier = "Profissional")
    {
        WithBearer(client, accessToken);
        var response = await client.PostAsJsonAsync("/api/subscriptions", new { Tier = tier });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Owner/Admin (token escopado à organization) convida um email com um papel — resposta crua (o teste decide o que checar: status, ou o token em claro no corpo).</summary>
    public static async Task<HttpResponseMessage> CreateInviteAsync(HttpClient client, string ownerAccessToken, Guid organizationId, string email, Role role)
    {
        WithBearer(client, ownerAccessToken);
        return await client.PostAsJsonAsync($"/api/organizations/{organizationId}/invites", new { Email = email, Role = role });
    }

    public static async Task<HttpResponseMessage> AcceptInviteAsync(HttpClient client, string accessToken, string inviteToken)
    {
        WithBearer(client, accessToken);
        return await client.PostAsync($"/api/invites/{inviteToken}/accept", content: null);
    }

    public static void WithBearer(HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
