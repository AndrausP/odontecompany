using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Repositories;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Billing.UnitTests.Persistence;

/// <summary>
/// Regressão pra <c>ApplyOrganizationQueryFilters</c> aplicada ao módulo Billing — cobre o gap
/// levantado na task 019 (isolamento cross-org em TODOS os módulos de negócio). O módulo já tinha
/// <see cref="FaturamentoSummaryProviderTests"/> cobrindo indiretamente o filtro via sumarização,
/// mas não havia teste focado só no filtro global do DbContext (padrão dos demais módulos).
/// </summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private static BillingDbContext CreateContext(string dbName, IOrganizationContext organizationContext)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new BillingDbContext(options, organizationContext);
    }

    // ── Fatura: filtro global aplica organization_id ─────────────────────────────────────────

    [Test]
    public async Task Should_ReturnOnlyFaturasFromCurrentOrganization_When_QueryFilterApplied()
    {
        // Arrange — mesma decisão de valor/parcelas nos dois organizations pra provar que só o
        // organization diferencia (não algum campo acessório).
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Faturas.Add(Fatura.CreateParticular(organizationA, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            seed.Faturas.Add(Fatura.CreateParticular(organizationB, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var faturas = await context.Faturas.ToListAsync();

        // Assert
        Assert.That(faturas, Has.Count.EqualTo(1),
            "recepção da organization A não pode ver faturas da organization B — vazamento financeiro é sensível");
        Assert.That(faturas[0].OrganizationId, Is.EqualTo(organizationA));
    }

    [Test]
    public async Task Should_ReturnNoFaturas_When_OrganizationContextNotResolved()
    {
        // Arrange — fail-closed: sem organization no contexto, nada vaza.
        var dbName = Guid.NewGuid().ToString();
        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Faturas.Add(Fatura.CreateParticular(Guid.NewGuid(), Guid.NewGuid(), null, null, 500m, 1, FormaPagamento.Pix, null).Value);
            await seed.SaveChangesAsync();
        }

        // Act
        await using var context = CreateContext(dbName, new OrganizationContext());
        var faturas = await context.Faturas.ToListAsync();

        // Assert
        Assert.That(faturas, Is.Empty,
            "sem organization resolvido, nada de fatura sai — fail-closed também em Billing");
    }

    // ── Parcela: filtro global aplica organization_id (Parcela é IMustHaveOrganization própria) ──

    [Test]
    public async Task Should_ReturnOnlyParcelasFromCurrentOrganization_When_QueryFilterApplied()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            // 3 parcelas em cada organization (300/3).
            seed.Faturas.Add(Fatura.CreateParticular(organizationA, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            seed.Faturas.Add(Fatura.CreateParticular(organizationB, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var parcelas = await context.Parcelas.ToListAsync();

        Assert.That(parcelas, Has.Count.EqualTo(3), "só as 3 parcelas do organization A ficam visíveis");
        Assert.That(parcelas.All(p => p.OrganizationId == organizationA), Is.True);
    }

    // ── Convenio: filtro global aplica organization_id ─────────────────────────────────────

    [Test]
    public async Task Should_ReturnOnlyConveniosFromCurrentOrganization_When_QueryFilterApplied()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Convenios.Add(Convenio.Create(organizationA, "Unimed", "unimed-a").Value);
            seed.Convenios.Add(Convenio.Create(organizationB, "Unimed", "unimed-b").Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var convenios = await context.Convenios.ToListAsync();

        Assert.That(convenios, Has.Count.EqualTo(1));
        Assert.That(convenios[0].OrganizationId, Is.EqualTo(organizationA));
    }

    // ── Repositório: ListAsync respeita o filtro global ────────────────────────────────────

    [Test]
    public async Task Should_ApplyOrganizationFilter_When_ListingThroughRepository()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Faturas.Add(Fatura.CreateParticular(organizationA, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            seed.Faturas.Add(Fatura.CreateParticular(organizationB, Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null).Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new FaturaRepository(context);

        var (items, totalCount) = await repository.ListAsync(pacienteId: null, status: null, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items[0].OrganizationId, Is.EqualTo(organizationA));
    }

    // ── Cross-org: repositório NÃO enxerga fatura da outra organization nem por id direto ──

    [Test]
    public async Task Should_ReturnNull_When_GettingFaturaByIdFromDifferentOrganization()
    {
        // Arrange — IDOR clássico: token de organization A pede fatura da organization B só sabendo o guid.
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var faturaB = Fatura.CreateParticular(organizationB, Guid.NewGuid(), null, null, 500m, 1, FormaPagamento.Pix, null).Value;

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Faturas.Add(faturaB);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new FaturaRepository(context);

        var found = await repository.GetByIdAsync(faturaB.Id);

        // Assert
        Assert.That(found, Is.Null,
            "GetByIdAsync com contexto de outro organization tem que devolver null — filtro global corta antes do Where");
    }
}
