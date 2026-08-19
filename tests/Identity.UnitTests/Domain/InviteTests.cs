using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.UnitTests.Domain;

[TestFixture]
public class InviteTests
{
    [Test]
    public void Should_CreatePendingInvite_When_InputIsValid()
    {
        var organizationId = Guid.NewGuid();
        var invitedBy = Guid.NewGuid();

        var result = Invite.Create(organizationId, "Convidado@Clinica.com", Role.Dentista, "hash-token", invitedBy, TimeSpan.FromDays(7));

        Assert.That(result.IsSuccess, Is.True);
        var invite = result.Value;
        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Pendente));
        Assert.That(invite.Email, Is.EqualTo("convidado@clinica.com"), "email sempre normalizado lowercase");
        Assert.That(invite.IsExpired, Is.False);
    }

    [Test]
    public void Should_ReturnOrganizationInvalido_When_OrganizationIdIsEmpty()
    {
        var result = Invite.Create(Guid.Empty, "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnEmailObrigatorio_When_EmailIsEmpty()
    {
        var result = Invite.Create(Guid.NewGuid(), "  ", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.EmailObrigatorio"));
    }

    [Test]
    public void Should_ReturnTokenInvalido_When_TokenHashIsEmpty()
    {
        var result = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "  ", Guid.NewGuid(), TimeSpan.FromDays(7));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Invite.TokenInvalido"));
    }

    [Test]
    public void Should_BeExpired_When_LifetimeIsInThePast()
    {
        var invite = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromSeconds(-1)).Value;

        Assert.That(invite.IsExpired, Is.True);
    }

    [Test]
    public void Should_SetStatusAceito_When_Accept()
    {
        var invite = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        invite.Accept();

        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Aceito));
    }

    [Test]
    public void Should_SetStatusRevogado_When_Revoke()
    {
        var invite = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        invite.Revoke();

        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Revogado));
    }

    [Test]
    public void Should_SetStatusExpirado_When_MarkExpiredOnPendingInvite()
    {
        var invite = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        invite.MarkExpired();

        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Expirado));
    }

    [Test]
    public void Should_NotOverwriteStatus_When_MarkExpiredOnAlreadyAcceptedInvite()
    {
        // Convite já aceito não pode "voltar" pra Expirado por uma checagem de expiração tardia.
        var invite = Invite.Create(Guid.NewGuid(), "a@b.com", Role.Dentista, "hash", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;
        invite.Accept();

        invite.MarkExpired();

        Assert.That(invite.Status, Is.EqualTo(InviteStatus.Aceito));
    }
}
