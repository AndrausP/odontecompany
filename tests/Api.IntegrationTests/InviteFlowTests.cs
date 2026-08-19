using System.Net;
using System.Net.Http.Json;
using Identity.Application.DTOs;
using Identity.Domain.Enums;

namespace Api.IntegrationTests;

/// <summary>
/// Task 021, casos 3, 4 e 5 — fluxo completo de convite fim a fim (signup → login → /api/me) e
/// autorização por role no endpoint de criar convite. Cada teste sobe sua própria
/// <see cref="ApiWebApplicationFactory"/> (banco InMemory isolado) — evita 1 teste contaminar o
/// estado (organizations/convites) que outro depende.
/// </summary>
[TestFixture]
public class InviteFlowTests
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
    public async Task Should_ShowInviteOnMe_When_SigningUpWithInvitedEmail()
    {
        // Caso 3 — convite pra email que ainda não tem conta: cria conta com esse email depois,
        // convite tem que aparecer em /api/me (fluxo E2E signup → login → /api/me).
        var owner = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail("owner"));
        var org = await AuthFlow.CreateOrganizationAsync(_client, owner.AccessToken, "Clínica do Convite");

        var invitedEmail = AuthFlow.UniqueEmail("convidado");
        var inviteResponse = await AuthFlow.CreateInviteAsync(_client, org.AccessToken, org.OrganizationId, invitedEmail, Role.Recepcao);
        inviteResponse.EnsureSuccessStatusCode();

        var invitedUser = await AuthFlow.SignupAsync(_client, invitedEmail);
        AuthFlow.WithBearer(_client, invitedUser.AccessToken);

        var meResponse = await _client.GetAsync("/api/me");
        meResponse.EnsureSuccessStatusCode();
        var me = (await meResponse.Content.ReadFromJsonAsync<MeResultDto>(AuthFlow.JsonOptions))!;

        Assert.That(me.PendingInvites, Has.Count.EqualTo(1));
        Assert.That(me.PendingInvites[0].OrganizationName, Is.EqualTo("Clínica do Convite"));
        Assert.That(me.PendingInvites[0].Role, Is.EqualTo(Role.Recepcao));
    }

    [Test]
    public async Task Should_Allow_Owner_ToCreateInvite()
    {
        // Caso 4 (metade positiva).
        var owner = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail("owner"));
        var org = await AuthFlow.CreateOrganizationAsync(_client, owner.AccessToken);

        var response = await AuthFlow.CreateInviteAsync(_client, org.AccessToken, org.OrganizationId, AuthFlow.UniqueEmail(), Role.Dentista);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [TestCase(Role.Dentista)]
    [TestCase(Role.Recepcao)]
    public async Task Should_Reject_NonOwnerNonAdmin_CreatingInvite(Role role)
    {
        // Caso 4 (metade negativa) + caso 5 (papéis existentes continuam se comportando como
        // antes — Owner é ADITIVO, não deveria ter afrouxado autorização de mais ninguém).
        var owner = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail("owner"));
        var org = await AuthFlow.CreateOrganizationAsync(_client, owner.AccessToken);

        var memberEmail = AuthFlow.UniqueEmail("membro");
        const string memberPassword = "Senha@12345";
        var inviteResponse = await AuthFlow.CreateInviteAsync(_client, org.AccessToken, org.OrganizationId, memberEmail, role);
        inviteResponse.EnsureSuccessStatusCode();
        var invite = (await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResultDto>(AuthFlow.JsonOptions))!;

        var member = await AuthFlow.SignupAsync(_client, memberEmail, memberPassword);
        var acceptResponse = await AuthFlow.AcceptInviteAsync(_client, member.AccessToken, invite.Token);
        acceptResponse.EnsureSuccessStatusCode();

        // Accept não emite token novo (ver AcceptInviteResultDto) — loga de novo pra ganhar um
        // token escopado à organization com o Role real da membership.
        var memberLogin = await AuthFlow.LoginAsync(_client, memberEmail, memberPassword);

        var response = await AuthFlow.CreateInviteAsync(_client, memberLogin.AccessToken, org.OrganizationId, AuthFlow.UniqueEmail(), Role.Dentista);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
            $"{role} não está em [Authorize(Roles = \"Owner,Admin\")] — CreateInvite tem que rejeitar");
    }
}
