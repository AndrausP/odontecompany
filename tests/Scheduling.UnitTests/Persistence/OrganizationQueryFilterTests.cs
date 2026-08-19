using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Repositories;

namespace Scheduling.UnitTests.Persistence;

/// <summary>Mesma regressão de Identity/Patients.UnitTests.Persistence.OrganizationQueryFilterTests, aplicada ao módulo Scheduling.</summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);

    private static SchedulingDbContext CreateContext(string dbName, IOrganizationContext organizationContext)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new SchedulingDbContext(options, organizationContext);
    }

    [Test]
    public async Task Should_ReturnOnlyAgendamentosFromCurrentOrganization_When_QueryFilterApplied()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Agendamentos.Add(Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value);
            seedContext.Agendamentos.Add(Agendamento.Criar(organizationB, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value);
            await seedContext.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var agendamentos = await context.Agendamentos.ToListAsync();

        Assert.That(agendamentos, Has.Count.EqualTo(1));
        Assert.That(agendamentos[0].OrganizationId, Is.EqualTo(organizationA));
    }

    [Test]
    public async Task Should_ApplyOrganizationFilter_When_ListingThroughRepository()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Agendamentos.Add(Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value);
            seedContext.Agendamentos.Add(Agendamento.Criar(organizationB, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value);
            await seedContext.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new AgendamentoRepository(context);

        var (items, totalCount) = await repository.ListAsync(null, null, null, null, null, null, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].OrganizationId, Is.EqualTo(organizationA));
    }

    // ── Regra: MarcarConcluido grava a entry de Outbox NA MESMA transação do agregado ────────
    [Test]
    public async Task Should_PersistOutboxMessage_When_AgendamentoIsMarkedAsConcluido()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();
        var organizationContext = new OrganizationContext();
        organizationContext.SetOrganization(organization);

        Guid agendamentoId;
        await using (var context = CreateContext(dbName, organizationContext))
        {
            var agendamento = Agendamento.Criar(organization, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;
            agendamento.Confirmar();
            context.Agendamentos.Add(agendamento);
            await context.SaveChangesAsync();
            agendamentoId = agendamento.Id;
        }

        await using (var context = CreateContext(dbName, organizationContext))
        {
            var agendamento = await context.Agendamentos.FirstAsync(a => a.Id == agendamentoId);
            agendamento.MarcarConcluido(300m);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, organizationContext))
        {
            var outboxMessages = await context.OutboxMessages.ToListAsync();

            Assert.That(outboxMessages, Has.Count.EqualTo(1));
            Assert.That(outboxMessages[0].Type, Is.EqualTo("ConsultaConcluidaEvent"));
            Assert.That(outboxMessages[0].Payload, Does.Contain(agendamentoId.ToString()));
            Assert.That(outboxMessages[0].PublishedOn, Is.Null, "worker de publicação ainda não existe — ver pendência documentada");
        }
    }
}
