using Identity.Domain.Entities;

namespace Identity.UnitTests.Domain;

/// <summary>
/// User é entidade GLOBAL desde a task 013 — Create(nome, email, passwordHash) não recebe mais
/// OrganizationId/Role (isso migrou pra OrganizationMembership, ver OrganizationMembershipTests).
/// </summary>
[TestFixture]
public class UserTests
{
    [Test]
    public void Should_CreateUser_When_AllDataIsValid()
    {
        var result = User.Create("Dra. Ana", "ANA@Clinica.com", "hash");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo("ana@clinica.com"), "email deve ser normalizado (trim + lowercase)");
        Assert.That(result.Value.Nome, Is.EqualTo("Dra. Ana"));
        Assert.That(result.Value.Ativo, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Should_ReturnFailure_When_NomeIsEmptyOrWhitespace(string nome)
    {
        var result = User.Create(nome, "fulano@clinica.com", "hash");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.NomeObrigatorio"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Should_ReturnFailure_When_EmailIsEmptyOrWhitespace(string email)
    {
        var result = User.Create("Fulano", email, "hash");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.EmailObrigatorio"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Should_ReturnFailure_When_PasswordHashIsEmptyOrWhitespace(string passwordHash)
    {
        var result = User.Create("Fulano", "fulano@clinica.com", passwordHash);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.SenhaObrigatoria"));
    }

    [Test]
    public void Should_DeactivateUser_When_Desativar_IsCalled()
    {
        var user = User.Create("Fulano", "fulano@clinica.com", "hash").Value;

        user.Desativar();

        Assert.That(user.Ativo, Is.False);
        Assert.That(user.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void Should_UpdatePasswordHash_When_AlterarSenha_IsCalled()
    {
        var user = User.Create("Fulano", "fulano@clinica.com", "hash-antigo").Value;

        user.AlterarSenha("hash-novo");

        Assert.That(user.PasswordHash, Is.EqualTo("hash-novo"));
        Assert.That(user.UpdatedAt, Is.Not.Null);
    }
}
