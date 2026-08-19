using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Lookups;
using Scheduling.Infrastructure.Persistence;

namespace Scheduling.UnitTests.Persistence;

[TestFixture]
public class ProfissionalLookupTests
{
    private static SchedulingDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>().UseInMemoryDatabase(dbName).Options;
        return new SchedulingDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_IncluirProfissionalInativo_When_ListarPorOrganization()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationId = Guid.NewGuid();
        var outroOrganizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            var ativo = Profissional.Criar(organizationId, "Dra. Ativa", "Ortodontia", userId, branchId).Value;

            var inativo = Profissional.Criar(organizationId, "Dr. Demitido", "Clínico Geral").Value;
            inativo.Desativar();

            var outraOrganization = Profissional.Criar(outroOrganizationId, "Fora", "Endodontia").Value;

            seedContext.Profissionais.AddRange(ativo, inativo, outraOrganization);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var lookup = new ProfissionalLookup(context);

        var resultado = await lookup.ListarPorOrganizationAsync(organizationId);

        Assert.That(resultado, Has.Count.EqualTo(2)); // não vaza outra organization

        var ativoDto = resultado.Single(p => p.Nome == "Dra. Ativa");
        Assert.That(ativoDto.Ativo, Is.True);
        Assert.That(ativoDto.UserId, Is.EqualTo(userId));
        Assert.That(ativoDto.BranchId, Is.EqualTo(branchId));

        // profissional demitido não pode sumir do histórico de comissão (task 022, R2/contexto).
        var inativoDto = resultado.Single(p => p.Nome == "Dr. Demitido");
        Assert.That(inativoDto.Ativo, Is.False);
    }

    [Test]
    public async Task Should_ReturnEmpty_When_OrganizationHasNoProfissionais()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var lookup = new ProfissionalLookup(context);

        var resultado = await lookup.ListarPorOrganizationAsync(Guid.NewGuid());

        Assert.That(resultado, Is.Empty);
    }
}
