using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Repositories;

namespace Scheduling.UnitTests.Persistence;

[TestFixture]
public class AgendaSummaryProviderTests
{
    private static SchedulingDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>().UseInMemoryDatabase(dbName).Options;
        return new SchedulingDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_GroupByStatus_And_IgnoreOtherOrganization_When_ObterResumoAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var inicio = new DateTime(2026, 8, 1);
        var fim = new DateTime(2026, 8, 31);

        await using (var seedContext = CreateContext(dbName))
        {
            var agendado = Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicio.AddDays(1), inicio.AddDays(1).AddMinutes(30)).Value;

            var confirmado = Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicio.AddDays(2), inicio.AddDays(2).AddMinutes(30)).Value;
            confirmado.Confirmar();

            var concluido = Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicio.AddDays(3), inicio.AddDays(3).AddMinutes(30)).Value;
            concluido.Confirmar();
            concluido.MarcarConcluido(150m);

            var cancelado = Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicio.AddDays(4), inicio.AddDays(4).AddMinutes(30)).Value;
            cancelado.Cancelar("paciente desmarcou");

            // Fora do período (não deve contar) e de outro organization (não deve vazar).
            var foraDoPeriodo = Agendamento.Criar(organizationA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fim.AddDays(10), fim.AddDays(10).AddMinutes(30)).Value;
            var outroOrganization = Agendamento.Criar(organizationB, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicio.AddDays(1), inicio.AddDays(1).AddMinutes(30)).Value;

            seedContext.Agendamentos.AddRange(agendado, confirmado, concluido, cancelado, foraDoPeriodo, outroOrganization);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new AgendaSummaryProvider(context);

        var resumo = await provider.ObterResumoAsync(organizationA, inicio, fim);

        Assert.That(resumo.TotalAgendamentos, Is.EqualTo(4));
        Assert.That(resumo.Agendados, Is.EqualTo(1));
        Assert.That(resumo.Confirmados, Is.EqualTo(1));
        Assert.That(resumo.Concluidos, Is.EqualTo(1));
        Assert.That(resumo.Cancelados, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_ReturnAllZeros_When_NoAgendamentosInPeriod()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var provider = new AgendaSummaryProvider(context);

        var resumo = await provider.ObterResumoAsync(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        Assert.That(resumo.TotalAgendamentos, Is.EqualTo(0));
    }
}
