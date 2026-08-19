using System.Net;
using System.Net.Http.Json;
using Identity.Application.DTOs;
using Identity.Domain.Enums;

namespace Api.IntegrationTests;

/// <summary>
/// Task 021, caso 1 — token de usuário SEM organization ativa (zero memberships, ex: acabou de
/// fazer signup). A policy fail-closed <c>RequireActiveOrganization</c> (`Program.cs`) tem que
/// rejeitar esse token num endpoint de negócio, mas 4 endpoints têm que continuar aceitando —
/// são exatamente os que existem PRA tirar o usuário desse estado. Só teste de integração real
/// (pipeline de auth/policy) cobre isso; teste de handler nunca passa pela policy.
/// </summary>
[TestFixture]
public class TokenWithoutOrganizationTests
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
    public async Task Should_Reject_BusinessEndpoint_When_TokenHasNoOrganization()
    {
        var signup = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail());
        AuthFlow.WithBearer(_client, signup.AccessToken);

        var response = await _client.GetAsync("/api/patients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
            "PatientsController é [Authorize(Policy = \"RequireActiveOrganization\")] — token sem organization_id tem que cair aqui, não passar");
    }

    [Test]
    public async Task Should_Allow_Me_When_TokenHasNoOrganization()
    {
        var signup = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail());
        AuthFlow.WithBearer(_client, signup.AccessToken);

        var response = await _client.GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "MeController é [Authorize] simples, sem RequireActiveOrganization — tem que funcionar sem org (é o endpoint que monta o boot do frontend antes de escolher/criar organization)");
    }

    [Test]
    public async Task Should_Allow_CreateOrganization_When_TokenHasNoOrganization()
    {
        var signup = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail());
        AuthFlow.WithBearer(_client, signup.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/organizations", new { Nome = "Clínica Sem Org Ainda" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            "é justamente o endpoint que dá a 1ª organization a quem não tem nenhuma — não pode exigir RequireActiveOrganization");
    }

    [Test]
    public async Task Should_Allow_AcceptInvite_When_TokenHasNoOrganization()
    {
        // Owner com organization própria convida o email do usuário sem-org.
        var owner = await AuthFlow.SignupAsync(_client, AuthFlow.UniqueEmail("owner"));
        var org = await AuthFlow.CreateOrganizationAsync(_client, owner.AccessToken);

        var invitedEmail = AuthFlow.UniqueEmail("convidado");
        var inviteResponse = await AuthFlow.CreateInviteAsync(_client, org.AccessToken, org.OrganizationId, invitedEmail, Role.Dentista);
        inviteResponse.EnsureSuccessStatusCode();
        var invite = (await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResultDto>(AuthFlow.JsonOptions))!;

        // Usuário convidado faz signup (token SEM organization) e aceita o convite com esse mesmo token.
        var invitedUser = await AuthFlow.SignupAsync(_client, invitedEmail);
        var acceptResponse = await AuthFlow.AcceptInviteAsync(_client, invitedUser.AccessToken, invite.Token);

        Assert.That(acceptResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "InvitesController.Accept não usa RequireActiveOrganization de propósito — é o endpoint que tira o usuário do estado \"sem org\"");
    }
}
