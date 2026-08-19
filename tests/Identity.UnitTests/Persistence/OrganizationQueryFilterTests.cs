using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Identity.UnitTests.Persistence;

/// <summary>
/// Regressão pra <c>ApplyOrganizationQueryFilters</c> (Infrastructure.Common) — o método genérico
/// que a task 001 introduziu pra varrer o modelo inteiro via reflection em vez de listar
/// entidade por entidade. Reflection compila fácil e falha calado em runtime se o tipo do
/// builder devolvido não bater. Por isso este teste usa o provider InMemory de verdade em vez de
/// só confiar que compilou.
///
/// Task 013 muda o alvo do filtro: <see cref="OrganizationMembership"/> é quem carrega
/// <c>IMustHaveOrganization</c> agora — <see cref="User"/> virou entidade GLOBAL e NUNCA é
/// filtrado por organization (query direta, sempre enxerga todo mundo, isolamento é
/// responsabilidade de quem consulta via email/id específico, não de listagem).
/// </summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private static IdentityDbContext CreateContext(string dbName, IOrganizationContext organizationContext)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new IdentityDbContext(options, organizationContext);
    }

    [Test]
    public async Task Should_ReturnOnlyMembershipsFromCurrentOrganization_When_QueryFilterApplied()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var userA = User.Create("Usuário A", "a@organization-a.local", "hash").Value;
        var userB = User.Create("Usuário B", "b@organization-b.local", "hash").Value;

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Users.AddRange(userA, userB);
            seedContext.OrganizationMemberships.Add(OrganizationMembership.Create(organizationA, userA.Id, Role.Admin).Value);
            seedContext.OrganizationMemberships.Add(OrganizationMembership.Create(organizationB, userB.Id, Role.Admin).Value);
            await seedContext.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var memberships = await context.OrganizationMemberships.ToListAsync();

        // Assert
        Assert.That(memberships, Has.Count.EqualTo(1),
            "admin da organization A não pode ver memberships da organization B — vazamento de isolamento");
        Assert.That(memberships[0].OrganizationId, Is.EqualTo(organizationA));
        Assert.That(memberships[0].UserId, Is.EqualTo(userA.Id));
    }

    [Test]
    public async Task Should_ReturnNoMemberships_When_OrganizationContextNotResolved()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var user = User.Create("Usuário A", "a@organization.local", "hash").Value;

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Users.Add(user);
            seedContext.OrganizationMemberships.Add(OrganizationMembership.Create(Guid.NewGuid(), user.Id, Role.Admin).Value);
            await seedContext.SaveChangesAsync();
        }

        // Act — nenhum organization resolvido (CurrentOrganizationId == null): filtro fail-closed, não deve vazar nada
        await using var context = CreateContext(dbName, new OrganizationContext());
        var memberships = await context.OrganizationMemberships.ToListAsync();

        // Assert
        Assert.That(memberships, Is.Empty);
    }

    [Test]
    public async Task Should_ReturnAllUsers_Regardless_Of_OrganizationContext_Because_UserIsGlobal()
    {
        // Arrange — User não implementa mais IMustHaveOrganization (task 013): é entidade global,
        // nunca cai no filtro. Isolamento de "quem pode logar em qual organization" é
        // responsabilidade de OrganizationMembership, não de User.
        var dbName = Guid.NewGuid().ToString();
        var userA = User.Create("Usuário A", "a@global.local", "hash").Value;
        var userB = User.Create("Usuário B", "b@global.local", "hash").Value;

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Users.AddRange(userA, userB);
            await seedContext.SaveChangesAsync();
        }

        // Act — mesmo com um organization específico resolvido no contexto (ou nenhum), User
        // continua visível por inteiro: a query filter nem é aplicada a esse DbSet.
        var organizationContext = new OrganizationContext();
        organizationContext.SetOrganization(Guid.NewGuid()); // organization arbitrário, sem relação com userA/userB

        await using var context = CreateContext(dbName, organizationContext);
        var users = await context.Users.ToListAsync();

        // Assert
        Assert.That(users, Has.Count.EqualTo(2), "User é global — filtro de organization não deve tocar nele");
    }
}
