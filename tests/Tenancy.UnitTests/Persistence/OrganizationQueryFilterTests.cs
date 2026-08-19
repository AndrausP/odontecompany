using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain.Entities;
using Tenancy.Infrastructure.Lookups;
using Tenancy.Infrastructure.Persistence;
using Tenancy.Infrastructure.Repositories;

namespace Tenancy.UnitTests.Persistence;

/// <summary>Instancia o DbContext de verdade (UseInMemoryDatabase) — não só mock — pra garantir que o Model do EF Core constrói sem erro. Ver docs/knowledge/errors-aprendidos.md.</summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private static TenancyDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>().UseInMemoryDatabase(dbName).Options;
        return new TenancyDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_ReturnOnlyBranchesFromRequestedOrganization_When_ListByOrganizationAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Branches.Add(Branch.Create(organizationA, "Branch A1", null).Value);
            seedContext.Branches.Add(Branch.Create(organizationB, "Branch B1", null).Value);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = new BranchRepository(context);

        var branches = await repository.ListByOrganizationAsync(organizationA, includeInactive: false);

        Assert.That(branches, Has.Count.EqualTo(1));
        Assert.That(branches[0].Nome, Is.EqualTo("Branch A1"));
    }

    [Test]
    public async Task Should_ExcludeInactiveByDefault_When_ListByOrganizationAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            var ativa = Branch.Create(organization, "Ativa", null).Value;
            var inativa = Branch.Create(organization, "Inativa", null).Value;
            inativa.Desativar();

            seedContext.Branches.AddRange(ativa, inativa);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = new BranchRepository(context);

        var soAtivas = await repository.ListByOrganizationAsync(organization, includeInactive: false);
        var todas = await repository.ListByOrganizationAsync(organization, includeInactive: true);

        Assert.That(soAtivas, Has.Count.EqualTo(1));
        Assert.That(todas, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Should_ReturnFalse_When_BranchBelongsToDifferentOrganization()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        Guid branchId;

        await using (var seedContext = CreateContext(dbName))
        {
            var branch = Branch.Create(organizationA, "Branch A1", null).Value;
            branchId = branch.Id;
            seedContext.Branches.Add(branch);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var lookup = new BranchLookup(context);

        // Branch existe, mas pertence ao organization A — checar contra o organization B não pode vazar.
        var existsInWrongOrganization = await lookup.ExistsAsync(organizationB, branchId);
        var existsInRightOrganization = await lookup.ExistsAsync(organizationA, branchId);

        Assert.That(existsInWrongOrganization, Is.False);
        Assert.That(existsInRightOrganization, Is.True);
    }
}
