using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Repositories;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Billing.UnitTests.Persistence;

[TestFixture]
public class ComissaoSummaryProviderTests
{
    private static BillingDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(dbName).Options;
        return new BillingDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_IgnorarParcelaNaoPaga_When_CalcularComissaoDoPeriodo()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = CreateContext(dbName))
        {
            // 2 parcelas de 150 cada — só a primeira é paga, a segunda fica pendente.
            var fatura = Fatura.CreateParticular(
                organizationId, Guid.NewGuid(), null, profissionalId, 300m, 2, FormaPagamento.Cartao, comissaoDentistaPercentual: 20m).Value;
            fatura.RegistrarPagamentoParcela(fatura.Parcelas[0].Id);

            seedContext.Faturas.Add(fatura);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new ComissaoSummaryProvider(context);

        var resultado = await provider.ObterComissoesAsync(organizationId, inicio, fim);

        Assert.That(resultado, Has.Count.EqualTo(1));
        var linha = resultado.Single();
        Assert.That(linha.ProfissionalId, Is.EqualTo(profissionalId));
        Assert.That(linha.QuantidadeParcelasPagas, Is.EqualTo(1)); // a pendente não conta
        Assert.That(linha.ValorPagoNoPeriodo, Is.EqualTo(150m));
    }

    [Test]
    public async Task Should_UsarValorPagoENaoValorTotal_When_FaturaTemParcelasParciaisPagas()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = CreateContext(dbName))
        {
            // ValorTotal 300, comissão 10% -> CalcularValorComissao() (competência) daria 30.
            // Só 1 das 3 parcelas (100) é paga -> comissão de caixa correta é 10, não 30.
            var fatura = Fatura.CreateParticular(
                organizationId, Guid.NewGuid(), null, profissionalId, 300m, 3, FormaPagamento.Cartao, comissaoDentistaPercentual: 10m).Value;
            fatura.RegistrarPagamentoParcela(fatura.Parcelas[0].Id);

            seedContext.Faturas.Add(fatura);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new ComissaoSummaryProvider(context);

        var resultado = await provider.ObterComissoesAsync(organizationId, inicio, fim);

        var linha = resultado.Single();
        Assert.That(linha.ValorPagoNoPeriodo, Is.EqualTo(100m));
        Assert.That(linha.ValorComissao, Is.EqualTo(10m)); // 100 * 10%, não 300 * 10%
    }

    [Test]
    public async Task Should_RetornarBucketNaoAtribuido_When_FaturaNaoTemProfissional()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = CreateContext(dbName))
        {
            var fatura = Fatura.CreateParticular(
                organizationId, Guid.NewGuid(), null, null, 200m, 1, FormaPagamento.Pix, comissaoDentistaPercentual: 15m).Value;
            fatura.RegistrarPagamentoParcela(fatura.Parcelas[0].Id);

            seedContext.Faturas.Add(fatura);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new ComissaoSummaryProvider(context);

        var resultado = await provider.ObterComissoesAsync(organizationId, inicio, fim);

        var linha = resultado.Single();
        Assert.That(linha.ProfissionalId, Is.Null);
        Assert.That(linha.ValorPagoNoPeriodo, Is.EqualTo(200m));
    }

    [Test]
    public async Task Should_ExcluirFaturaCancelada_When_CalcularComissao()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = CreateContext(dbName))
        {
            // Fatura parcialmente paga (1 de 2 parcelas) e depois cancelada — Status != Paga,
            // então Cancelar() é permitido, mas a comissão da parcela já paga não deve contar.
            var faturaCancelada = Fatura.CreateParticular(
                organizationId, Guid.NewGuid(), null, profissionalId, 300m, 2, FormaPagamento.Cartao, comissaoDentistaPercentual: 20m).Value;
            faturaCancelada.RegistrarPagamentoParcela(faturaCancelada.Parcelas[0].Id);
            faturaCancelada.Cancelar();

            seedContext.Faturas.Add(faturaCancelada);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new ComissaoSummaryProvider(context);

        var resultado = await provider.ObterComissoesAsync(organizationId, inicio, fim);

        Assert.That(resultado, Is.Empty);
    }

    [Test]
    public async Task Should_ReturnEmpty_When_NoParcelaPagaInPeriod()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var provider = new ComissaoSummaryProvider(context);

        var resultado = await provider.ObterComissoesAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        Assert.That(resultado, Is.Empty);
    }
}
