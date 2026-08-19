using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

/// <summary>
/// Task 021, caso 2 — política de rate limit "signup" (`Program.cs`, `PermitLimit = 5` por IP a
/// cada 1min). Config declarativa (`[EnableRateLimiting("signup")]` + `AddRateLimiter`) só se
/// prova de verdade disparando requisições reais contra o pipeline — teste de handler nunca passa
/// pelo middleware de rate limiting.
/// </summary>
[TestFixture]
public class SignupRateLimitTests
{
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Should_RejectWithTooManyRequests_When_SignupExceedsRateLimit()
    {
        // PermitLimit=5/1min por IP — TestServer não configura RemoteIpAddress por padrão, então
        // todas as chamadas caem na mesma partição (fallback "unknown" em Program.cs), o que é
        // exatamente o que queremos pra provar o mecanismo sem precisar simular IPs diferentes.
        var statusCodes = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/signup", new
            {
                Nome = "Usuário Rate Limit",
                Email = AuthFlow.UniqueEmail("ratelimit"),
                Password = "Senha@12345",
            });
            statusCodes.Add(response.StatusCode);
        }

        Assert.That(statusCodes.Take(5), Has.All.EqualTo(HttpStatusCode.Created), "as 5 primeiras dentro do PermitLimit devem passar");
        Assert.That(statusCodes[5], Is.EqualTo(HttpStatusCode.TooManyRequests), "a 6ª na mesma janela de 1min tem que ser rejeitada (429)");
    }
}
