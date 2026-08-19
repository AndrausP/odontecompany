using Billing.Domain.Entities;
using Billing.Domain.Enums;

namespace Billing.UnitTests.Domain;

[TestFixture]
public class FaturaTests
{
    [Test]
    public void Should_CreateFaturaParticular_When_DataIsValid()
    {
        var result = Fatura.CreateParticular(
            Guid.NewGuid(), Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Parcelas, Has.Count.EqualTo(3));
        Assert.That(result.Value.Status, Is.EqualTo(StatusFatura.Pendente));
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void Should_ReturnFailure_When_ValorTotalIsZeroOrNegative(decimal valor)
    {
        var result = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, valor, 1, FormaPagamento.Pix, null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.ValorInvalido"));
    }

    [TestCase(0)]
    [TestCase(13)]
    public void Should_ReturnFailure_When_NumeroParcelasIsOutOfRange(int numeroParcelas)
    {
        var result = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m, numeroParcelas, FormaPagamento.Pix, null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.NumeroParcelasInvalido"));
    }

    [Test]
    public void Should_SplitValueWithoutLosingCents_When_ValorTotalDoesNotDivideEvenly()
    {
        // 100.00 / 3 = 33.33 (com resto) — a soma das parcelas TEM que bater exatamente com o
        // valor total, o resto de arredondamento vai pra ÚLTIMA parcela.
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m, 3, FormaPagamento.Cartao, null).Value;

        var somaParcelas = fatura.Parcelas.Sum(p => p.ValorParcela);

        Assert.That(somaParcelas, Is.EqualTo(100m));
        Assert.That(fatura.Parcelas[0].ValorParcela, Is.EqualTo(33.33m));
        Assert.That(fatura.Parcelas[1].ValorParcela, Is.EqualTo(33.33m));
        Assert.That(fatura.Parcelas[2].ValorParcela, Is.EqualTo(33.34m)); // resto absorvido na última
    }

    [Test]
    public void Should_UpdateStatusToParcialmentePaga_When_SomeButNotAllParcelasArePaid()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value;
        var primeiraParcelaId = fatura.Parcelas[0].Id;

        var result = fatura.RegistrarPagamentoParcela(primeiraParcelaId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(fatura.Status, Is.EqualTo(StatusFatura.ParcialmentePaga));
    }

    [Test]
    public void Should_UpdateStatusToPaga_When_AllParcelasArePaid()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 200m, 2, FormaPagamento.Pix, null).Value;

        foreach (var parcela in fatura.Parcelas.ToList())
            fatura.RegistrarPagamentoParcela(parcela.Id);

        Assert.That(fatura.Status, Is.EqualTo(StatusFatura.Paga));
    }

    [Test]
    public void Should_ReturnFailure_When_RegistrarPagamentoParcela_ParcelaIdDoesNotExist()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m, 1, FormaPagamento.Pix, null).Value;

        var result = fatura.RegistrarPagamentoParcela(Guid.NewGuid());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Parcela.NaoEncontrada"));
    }

    [Test]
    public void Should_ReturnFailure_When_CancelarIsCalledOnFaturaJaCancelada()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m, 1, FormaPagamento.Pix, null).Value;
        fatura.Cancelar();

        var result = fatura.Cancelar();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.JaCancelada"));
    }

    [Test]
    public void Should_ReturnFailure_When_CancelarIsCalledOnFaturaJaPaga()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m, 1, FormaPagamento.Pix, null).Value;
        fatura.RegistrarPagamentoParcela(fatura.Parcelas[0].Id);

        var result = fatura.Cancelar();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.NaoPodeCancelarFaturaPaga"));
    }

    [Test]
    public void Should_CalculateComissao_When_PercentualIsSet()
    {
        var fatura = Fatura.CreateParticular(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), 1000m, 1, FormaPagamento.Pix, comissaoDentistaPercentual: 30m).Value;

        Assert.That(fatura.CalcularValorComissao(), Is.EqualTo(300m));
    }

    [Test]
    public void Should_ReturnZeroComissao_When_PercentualIsNull()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 1000m, 1, FormaPagamento.Pix, null).Value;

        Assert.That(fatura.CalcularValorComissao(), Is.EqualTo(0m));
    }

    [Test]
    public void Should_CalculateComissaoSobreValorPago_When_PercentualIsSet()
    {
        var fatura = Fatura.CreateParticular(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), 1000m, 1, FormaPagamento.Pix, comissaoDentistaPercentual: 30m).Value;

        Assert.That(fatura.CalcularComissaoSobre(100m), Is.EqualTo(30m));
    }

    [Test]
    public void Should_ReturnZeroComissaoSobreValorPago_When_PercentualIsNull()
    {
        var fatura = Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 1000m, 1, FormaPagamento.Pix, null).Value;

        Assert.That(fatura.CalcularComissaoSobre(500m), Is.EqualTo(0m));
    }

    [Test]
    public void Should_ReturnZeroComissaoSobreValorPago_When_PercentualIsZero()
    {
        var fatura = Fatura.CreateParticular(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), 1000m, 1, FormaPagamento.Pix, comissaoDentistaPercentual: 0m).Value;

        Assert.That(fatura.CalcularComissaoSobre(500m), Is.EqualTo(0m));
    }

    [Test]
    public void Should_ReturnZeroComissaoSobreValorPago_When_ValorPagoIsZero()
    {
        var fatura = Fatura.CreateParticular(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), 1000m, 1, FormaPagamento.Pix, comissaoDentistaPercentual: 30m).Value;

        Assert.That(fatura.CalcularComissaoSobre(0m), Is.EqualTo(0m));
    }

    [Test]
    public void Should_CreateFaturaConvenio_With_SingleParcela_When_DataIsValid()
    {
        var result = Fatura.CreateConvenio(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), 500m, 20m);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Parcelas, Has.Count.EqualTo(1));
        Assert.That(result.Value.TipoFatura, Is.EqualTo(TipoFatura.Convenio));
    }

    [Test]
    public void Should_ReturnFailure_When_CreateConvenio_ConvenioIdIsEmpty()
    {
        var result = Fatura.CreateConvenio(Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.Empty, 500m, null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.ConvenioObrigatorioParaFaturaDeConvenio"));
    }

    [Test]
    public void Should_RegisterProtocolo_When_RegistrarProtocoloConvenioIsCalled()
    {
        var fatura = Fatura.CreateConvenio(Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid(), 500m, null).Value;

        fatura.RegistrarProtocoloConvenio("PROTO-123");

        Assert.That(fatura.ProtocoloConvenio, Is.EqualTo("PROTO-123"));
    }
}
