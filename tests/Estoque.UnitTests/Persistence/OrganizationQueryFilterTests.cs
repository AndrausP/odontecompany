using Estoque.Domain.Entities;
using Estoque.Infrastructure.Persistence;
using Estoque.Infrastructure.Repositories;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Estoque.UnitTests.Persistence;

/// <summary>Instancia o DbContext de verdade — não só mock — pra garantir que o Model do EF Core constrói sem erro. Ver docs/knowledge/errors-aprendidos.md.</summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private static EstoqueDbContext CreateContext(string dbName, Guid? organization = null)
    {
        var organizationContext = new OrganizationContext();
        if (organization is not null)
            organizationContext.SetOrganization(organization.Value);

        var options = new DbContextOptionsBuilder<EstoqueDbContext>().UseInMemoryDatabase(dbName).Options;
        return new EstoqueDbContext(options, organizationContext);
    }

    [Test]
    public async Task Should_FilterByBranchId_And_ExcludeInactiveByDefault_When_ListAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            var itemBranchA = ItemEstoque.Create(organization, branchA, "Luva", "caixa", 10).Value;
            var itemBranchB = ItemEstoque.Create(organization, branchB, "Gaze", "pacote", 10).Value;
            var itemInativoBranchA = ItemEstoque.Create(organization, branchA, "Item Descontinuado", "un", 0).Value;
            itemInativoBranchA.Desativar();

            seedContext.ItensEstoque.AddRange(itemBranchA, itemBranchB, itemInativoBranchA);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, organization);
        var repository = new ItemEstoqueRepository(context);

        var (items, totalCount) = await repository.ListAsync(branchA, includeInactive: false, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items[0].Nome, Is.EqualTo("Luva"));
    }

    [Test]
    public async Task Should_IncludeInactive_When_IncludeInactiveIsTrue()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            var ativo = ItemEstoque.Create(organization, null, "Ativo", "un", 0).Value;
            var inativo = ItemEstoque.Create(organization, null, "Inativo", "un", 0).Value;
            inativo.Desativar();

            seedContext.ItensEstoque.AddRange(ativo, inativo);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, organization);
        var repository = new ItemEstoqueRepository(context);

        var (items, totalCount) = await repository.ListAsync(branchId: null, includeInactive: true, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(2));
    }
}
