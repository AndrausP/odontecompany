using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Lookups;
using Identity.Infrastructure.Persistence;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Identity.UnitTests.Persistence;

[TestFixture]
public class MembershipLookupTests
{
    private static IdentityDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(dbName).Options;
        return new IdentityDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_ListarTodasAsAfiliacoes_When_ListarPorOrganizationAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var outroOrganizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var userAdmin = User.Create("Admin", "admin@clinica.local", "hash").Value;
        var userDentista = User.Create("Dentista", "dentista@clinica.local", "hash").Value;
        var userOutraOrg = User.Create("Fora", "fora@outra.local", "hash").Value;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Users.AddRange(userAdmin, userDentista, userOutraOrg);

            var membershipAdmin = OrganizationMembership.Create(organizationId, userAdmin.Id, Role.Admin).Value;
            var membershipDentista = OrganizationMembership.Create(organizationId, userDentista.Id, Role.Dentista, branchId).Value;
            var membershipOutraOrg = OrganizationMembership.Create(outroOrganizationId, userOutraOrg.Id, Role.Recepcao).Value;

            seedContext.OrganizationMemberships.AddRange(membershipAdmin, membershipDentista, membershipOutraOrg);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var lookup = new MembershipLookup(context);

        var resultado = await lookup.ListarPorOrganizationAsync(organizationId);

        Assert.That(resultado, Has.Count.EqualTo(2)); // não vaza a outra organization

        var dentista = resultado.Single(m => m.UserId == userDentista.Id);
        Assert.That(dentista.Role, Is.EqualTo("Dentista")); // enum como string — fronteira pública
        Assert.That(dentista.BranchId, Is.EqualTo(branchId));
        Assert.That(dentista.Ativo, Is.True);
    }

    [Test]
    public async Task Should_MarcarComoNaoAtivo_When_MembershipFoiDesativada()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var user = User.Create("Demitido", "demitido@clinica.local", "hash").Value;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Users.Add(user);

            var membership = OrganizationMembership.Create(organizationId, user.Id, Role.Dentista).Value;
            membership.Desativar();

            seedContext.OrganizationMemberships.Add(membership);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var lookup = new MembershipLookup(context);

        var resultado = await lookup.ListarPorOrganizationAsync(organizationId);

        // INCLUI a afiliação desativada — não some da listagem, só marca Ativo=false.
        Assert.That(resultado, Has.Count.EqualTo(1));
        Assert.That(resultado.Single().Ativo, Is.False);
    }

    [Test]
    public async Task Should_ReturnEmpty_When_OrganizationHasNoMemberships()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var lookup = new MembershipLookup(context);

        var resultado = await lookup.ListarPorOrganizationAsync(Guid.NewGuid());

        Assert.That(resultado, Is.Empty);
    }
}
