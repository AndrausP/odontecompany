using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Domain;

[TestFixture]
public class ProcedimentoTests
{
    [Test]
    public void Criar_Should_Succeed_When_OnlyNomeInformed()
    {
        var result = Procedimento.Criar(Guid.NewGuid(), "Limpeza");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.ValorPadrao, Is.Null);
        Assert.That(result.Value.DuracaoPadraoMinutos, Is.Null);
    }

    [Test]
    public void Criar_Should_Fail_When_ValorPadraoIsNegative()
    {
        var result = Procedimento.Criar(Guid.NewGuid(), "Limpeza", valorPadrao: -10m);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Procedimento.ValorInvalido"));
    }

    [Test]
    public void Criar_Should_Fail_When_DuracaoPadraoIsZeroOrLess()
    {
        var result = Procedimento.Criar(Guid.NewGuid(), "Limpeza", duracaoPadraoMinutos: 0);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Procedimento.DuracaoInvalida"));
    }

    [Test]
    public void AtualizarDados_Should_UpdateFields_When_Valid()
    {
        var procedimento = Procedimento.Criar(Guid.NewGuid(), "Limpeza", 150m, 30).Value;

        var result = procedimento.AtualizarDados("Limpeza Completa", 180m, 45);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(procedimento.Nome, Is.EqualTo("Limpeza Completa"));
        Assert.That(procedimento.ValorPadrao, Is.EqualTo(180m));
        Assert.That(procedimento.DuracaoPadraoMinutos, Is.EqualTo(45));
    }

    [Test]
    public void Desativar_Should_SetAtivoFalse()
    {
        var procedimento = Procedimento.Criar(Guid.NewGuid(), "Limpeza").Value;

        procedimento.Desativar();

        Assert.That(procedimento.Ativo, Is.False);
    }
}
