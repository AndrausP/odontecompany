using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Repositories;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Billing.UnitTests.Persistence;

[TestFixture]
public class FaturamentoSummaryProviderTests
{
    private static BillingDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(dbName).Options;
        return new BillingDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_SumFaturado_And_Recebido_ExcludingCanceladas_And_OtherOrganization_When_ObterResumoAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = CreateContext(dbName))
        {
            // Fatura A: 300 total, 1 parcela paga (100), 2 pendentes (100+100).
            var faturaA = Fatura.CreateParticular(organizationA, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value;
            faturaA.RegistrarPagamentoParcela(faturaA.Parcelas[0].Id);

            // Fatura cancelada — não deve entrar em nenhum total.
            var faturaCancelada = Fatura.CreateParticular(organizationA, Guid.NewGuid(), null, null, 500m, 1, FormaPagamento.Pix, null).Value;
            faturaCancelada.Cancelar();

            // Outro organization — não pode vazar pro resumo do organization A.
            var faturaOutroOrganization = Fatura.CreateParticular(organizationB, Guid.NewGuid(), null, null, 1000m, 1, FormaPagamento.Pix, null).Value;

            seedContext.Faturas.AddRange(faturaA, faturaCancelada, faturaOutroOrganization);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new FaturamentoSummaryProvider(context);

        var resumo = await provider.ObterResumoAsync(organizationA, inicio, fim);

        Assert.That(resumo.QuantidadeFaturas, Is.EqualTo(1)); // só a não-cancelada do organization A
        Assert.That(resumo.ValorTotalFaturado, Is.EqualTo(300m));
        Assert.That(resumo.ValorTotalRecebido, Is.EqualTo(100m));
        Assert.That(resumo.ValorTotalPendente, Is.EqualTo(200m));
    }

    [Test]
    public async Task Should_ReturnZeros_When_NoFaturasInPeriod()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var provider = new FaturamentoSummaryProvider(context);

        var resumo = await provider.ObterResumoAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        Assert.That(resumo.QuantidadeFaturas, Is.EqualTo(0));
        Assert.That(resumo.ValorTotalFaturado, Is.EqualTo(0m));
    }
}
