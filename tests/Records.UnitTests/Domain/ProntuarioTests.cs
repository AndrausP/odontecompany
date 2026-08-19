using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Domain;

[TestFixture]
public class ProntuarioTests
{
    [Test]
    public void Should_CreateProntuario_When_OrganizationAndPacienteAreValid()
    {
        var result = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Ativo, Is.True);
        Assert.That(result.Value.Odontograma, Is.Empty);
    }

    [Test]
    public void Should_ReturnFailure_When_OrganizationIdIsEmpty()
    {
        var result = Prontuario.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_PacienteIdIsEmpty()
    {
        var result = Prontuario.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.PacienteInvalido"));
    }

    [Test]
    public void Should_UpdateOdontograma_When_AtualizarDenteIsCalled()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;

        prontuario.AtualizarDente(11, StatusDente.Cariado);

        Assert.That(prontuario.Odontograma[11], Is.EqualTo(StatusDente.Cariado));
    }

    [Test]
    public void Should_OverwritePreviousStatus_When_AtualizarDenteIsCalledTwiceForSameTooth()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;

        prontuario.AtualizarDente(11, StatusDente.Cariado);
        prontuario.AtualizarDente(11, StatusDente.Restaurado);

        Assert.That(prontuario.Odontograma[11], Is.EqualTo(StatusDente.Restaurado));
        Assert.That(prontuario.Odontograma, Has.Count.EqualTo(1));
    }

    [Test]
    public void Should_SetAtivoToFalse_But_NeverThrow_When_DesativarIsCalledTwice()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;

        prontuario.Desativar();
        Assert.DoesNotThrow(() => prontuario.Desativar());

        Assert.That(prontuario.Ativo, Is.False);
    }
}
