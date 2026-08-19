using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain.Entities;
using Tenancy.Infrastructure.Lookups;
using Tenancy.Infrastructure.Persistence;

namespace Tenancy.UnitTests.Persistence;

[TestFixture]
public class BranchLookupTests
{
    private static TenancyDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>().UseInMemoryDatabase(dbName).Options;
        return new TenancyDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_IncluirBranchInativa_When_ListarNomesAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var outroOrganizationId = Guid.NewGuid();

        Branch ativa, inativa;
        await using (var seedContext = CreateContext(dbName))
        {
            ativa = Branch.Create(organizationId, "Clínica Centro", "Rua A, 123").Value;

            inativa = Branch.Create(organizationId, "Clínica Antiga", null).Value;
            inativa.Desativar();

            var outraOrganization = Branch.Create(outroOrganizationId, "Fora", null).Value;

            seedContext.Branches.AddRange(ativa, inativa, outraOrganization);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var lookup = new BranchLookup(context);

        var resultado = await lookup.ListarNomesAsync(organizationId);

        Assert.That(resultado, Has.Count.EqualTo(2)); // não vaza outra organization
        Assert.That(resultado[ativa.Id], Is.EqualTo("Clínica Centro"));
        Assert.That(resultado[inativa.Id], Is.EqualTo("Clínica Antiga")); // inativa não some do dicionário
    }

    [Test]
    public async Task Should_ReturnEmpty_When_OrganizationHasNoBranches()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var lookup = new BranchLookup(context);

        var resultado = await lookup.ListarNomesAsync(Guid.NewGuid());

        Assert.That(resultado, Is.Empty);
    }
}
