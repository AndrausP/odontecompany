using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Repositories;

namespace Scheduling.UnitTests.Persistence;

/// <summary>Fase 5: `ListAsync` filtrado por BranchId (via Profissional) — usado pelo controller pra restringir Recepcao à própria branch.</summary>
[TestFixture]
public class AgendamentoListByBranchTests
{
    private static SchedulingDbContext CreateContext(string dbName, Guid organization)
    {
        var organizationContext = new OrganizationContext();
        organizationContext.SetOrganization(organization);
        var options = new DbContextOptionsBuilder<SchedulingDbContext>().UseInMemoryDatabase(dbName).Options;
        return new SchedulingDbContext(options, organizationContext);
    }

    [Test]
    public async Task Should_ReturnOnlyAgendamentosOfProfissionaisFromRequestedBranch_When_BranchIdFilterIsSet()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        var inicio = new DateTime(2026, 9, 1, 9, 0, 0);

        Guid profissionalA, profissionalB, profissionalSemBranch;

        await using (var seedContext = CreateContext(dbName, organization))
        {
            var profA = Profissional.Criar(organization, "Dr. A", "Clínico", TipoContrato.Clt, branchId: branchA).Value;
            var profB = Profissional.Criar(organization, "Dr. B", "Clínico", TipoContrato.Clt, branchId: branchB).Value;
            var profSemBranch = Profissional.Criar(organization, "Dr. C", "Clínico", TipoContrato.Clt).Value; // sem branch

            profissionalA = profA.Id;
            profissionalB = profB.Id;
            profissionalSemBranch = profSemBranch.Id;

            seedContext.Profissionais.AddRange(profA, profB, profSemBranch);

            var salaId = Guid.NewGuid();
            seedContext.Agendamentos.Add(Agendamento.Criar(organization, Guid.NewGuid(), profissionalA, salaId, inicio, inicio.AddMinutes(30)).Value);
            seedContext.Agendamentos.Add(Agendamento.Criar(organization, Guid.NewGuid(), profissionalB, salaId, inicio.AddHours(1), inicio.AddHours(1).AddMinutes(30)).Value);
            seedContext.Agendamentos.Add(Agendamento.Criar(organization, Guid.NewGuid(), profissionalSemBranch, salaId, inicio.AddHours(2), inicio.AddHours(2).AddMinutes(30)).Value);

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, organization);
        var repository = new AgendamentoRepository(context);

        var (items, totalCount) = await repository.ListAsync(
            profissionalId: null, pacienteId: null, status: null, dataInicio: null, dataFim: null,
            branchId: branchA, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].ProfissionalId, Is.EqualTo(profissionalA));
    }

    [Test]
    public async Task Should_ReturnAllAgendamentos_When_BranchIdFilterIsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();
        var inicio = new DateTime(2026, 9, 1, 9, 0, 0);

        await using (var seedContext = CreateContext(dbName, organization))
        {
            var profA = Profissional.Criar(organization, "Dr. A", "Clínico", TipoContrato.Clt, branchId: Guid.NewGuid()).Value;
            var profB = Profissional.Criar(organization, "Dr. B", "Clínico", TipoContrato.Clt, branchId: Guid.NewGuid()).Value;
            seedContext.Profissionais.AddRange(profA, profB);

            var salaId = Guid.NewGuid();
            seedContext.Agendamentos.Add(Agendamento.Criar(organization, Guid.NewGuid(), profA.Id, salaId, inicio, inicio.AddMinutes(30)).Value);
            seedContext.Agendamentos.Add(Agendamento.Criar(organization, Guid.NewGuid(), profB.Id, salaId, inicio.AddHours(1), inicio.AddHours(1).AddMinutes(30)).Value);

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, organization);
        var repository = new AgendamentoRepository(context);

        // Admin (sem BranchId) — vê tudo, filtro null.
        var (items, totalCount) = await repository.ListAsync(
            profissionalId: null, pacienteId: null, status: null, dataInicio: null, dataFim: null,
            branchId: null, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(2));
    }
}
