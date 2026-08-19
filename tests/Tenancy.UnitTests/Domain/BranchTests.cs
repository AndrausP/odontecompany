using Tenancy.Domain.Entities;

namespace Tenancy.UnitTests.Domain;

[TestFixture]
public class BranchTests
{
    [Test]
    public void Should_CreateBranch_When_DataIsValid()
    {
        var result = Branch.Create(Guid.NewGuid(), "Clínica Centro", "Rua A, 123");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Ativo, Is.True);
    }

    [Test]
    public void Should_ReturnFailure_When_OrganizationIdIsEmpty()
    {
        var result = Branch.Create(Guid.Empty, "Clínica Centro", null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_NomeIsEmpty()
    {
        var result = Branch.Create(Guid.NewGuid(), "   ", null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NomeObrigatorio"));
    }

    [Test]
    public void Should_SetAtivoToFalse_When_DesativarIsCalled()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Clínica Centro", null).Value;

        branch.Desativar();

        Assert.That(branch.Ativo, Is.False);
    }
}
