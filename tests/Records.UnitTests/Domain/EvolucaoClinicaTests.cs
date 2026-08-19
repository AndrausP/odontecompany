using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Domain;

[TestFixture]
public class EvolucaoClinicaTests
{
    [Test]
    public void Should_CreateEvolucao_When_DataIsValid()
    {
        var result = EvolucaoClinica.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoProcedimento.Restauracao, "Restauração no dente 26.");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.DescricaoClinica, Is.EqualTo("Restauração no dente 26."));
        Assert.That(result.Value.TipoProcedimento, Is.EqualTo(TipoProcedimento.Restauracao));
    }

    [Test]
    public void Should_ReturnFailure_When_DescricaoClinicaIsEmpty()
    {
        var result = EvolucaoClinica.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoProcedimento.Consulta, "   ");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Evolucao.DescricaoObrigatoria"));
    }

    [Test]
    public void Should_ReturnFailure_When_ProfissionalUserIdIsEmpty()
    {
        var result = EvolucaoClinica.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TipoProcedimento.Consulta, "Avaliação inicial.");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Evolucao.ProfissionalInvalido"));
    }
}
