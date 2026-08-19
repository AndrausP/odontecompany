using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Domínio novo da task 013 — relação N:N User↔Organization. Sem cobertura de teste até esta
/// sessão (o gap original só tinha OrganizationQueryFilterTests, focado em isolamento via EF,
/// não nas regras de negócio da entidade em si).
/// </summary>
[TestFixture]
public class OrganizationMembershipTests
{
    [Test]
    public void Should_CreateMembership_When_AllDataIsValid()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = OrganizationMembership.Create(organizationId, userId, Role.Dentista);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.OrganizationId, Is.EqualTo(organizationId));
        Assert.That(result.Value.UserId, Is.EqualTo(userId));
        Assert.That(result.Value.Role, Is.EqualTo(Role.Dentista));
        Assert.That(result.Value.BranchId, Is.Null, "BranchId é opcional — null significa irrestrito na organization");
        Assert.That(result.Value.IsAtivo, Is.True);
    }

    [Test]
    public void Should_CreateMembership_When_BranchIdIsProvided()
    {
        var branchId = Guid.NewGuid();

        var result = OrganizationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), Role.Recepcao, branchId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.BranchId, Is.EqualTo(branchId));
    }

    [Test]
    public void Should_ReturnFailure_When_OrganizationIdIsEmpty()
    {
        var result = OrganizationMembership.Create(Guid.Empty, Guid.NewGuid(), Role.Admin);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Membership.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_UserIdIsEmpty()
    {
        var result = OrganizationMembership.Create(Guid.NewGuid(), Guid.Empty, Role.Admin);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Membership.UserInvalido"));
    }

    [Test]
    public void Should_DeactivateMembership_When_Desativar_IsCalled()
    {
        var membership = OrganizationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), Role.Owner).Value;

        membership.Desativar();

        Assert.That(membership.IsAtivo, Is.False);
        Assert.That(membership.UpdatedAt, Is.Not.Null);
    }
}
