using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Domain;

[TestFixture]
public class ProfissionalTests
{
    [Test]
    public void VincularUsuario_Should_SetUserId_When_NotAlreadyLinked()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dra. Camila", "Ortodontia", TipoContrato.Pj).Value;
        var userId = Guid.NewGuid();

        profissional.VincularUsuario(userId);

        Assert.That(profissional.UserId, Is.EqualTo(userId));
    }

    /// <summary>Auditoria pré-venda (task 042) — um segundo convite aceito pro mesmo email não rouba o vínculo de quem já é o usuário real deste profissional.</summary>
    [Test]
    public void VincularUsuario_Should_NotOverwrite_When_AlreadyLinked()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dra. Camila", "Ortodontia", TipoContrato.Pj).Value;
        var primeiroUserId = Guid.NewGuid();
        profissional.VincularUsuario(primeiroUserId);

        profissional.VincularUsuario(Guid.NewGuid());

        Assert.That(profissional.UserId, Is.EqualTo(primeiroUserId));
    }

    [Test]
    public void Criar_Should_NormalizeEmail_When_Provided()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dra. Camila", "Ortodontia", TipoContrato.Clt, email: "  Camila@Clinica.COM  ").Value;

        Assert.That(profissional.Email, Is.EqualTo("camila@clinica.com"));
    }

    [Test]
    public void Criar_Should_LeaveEmailNull_When_NotProvided()
    {
        var profissional = Profissional.Criar(Guid.NewGuid(), "Dra. Camila", "Ortodontia", TipoContrato.Clt).Value;

        Assert.That(profissional.Email, Is.Null);
    }
}
