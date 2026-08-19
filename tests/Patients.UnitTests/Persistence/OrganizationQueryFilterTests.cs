using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Patients.Domain.Entities;
using Patients.Infrastructure.Persistence;
using Patients.Infrastructure.Repositories;

namespace Patients.UnitTests.Persistence;

/// <summary>
/// Mesma regressão de <c>Identity.UnitTests.Persistence.OrganizationQueryFilterTests</c> — garante
/// que ApplyOrganizationQueryFilters (Infrastructure.Common) funciona de verdade pra este módulo
/// também, com provider InMemory de verdade em vez de só confiar que compilou.
/// </summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    private static PatientsDbContext CreateContext(string dbName, IOrganizationContext organizationContext)
    {
        var options = new DbContextOptionsBuilder<PatientsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new PatientsDbContext(options, organizationContext);
    }

    [Test]
    public async Task Should_ReturnOnlyPatientsFromCurrentOrganization_When_QueryFilterApplied()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Patients.Add(Patient.Create(organizationA, "Paciente A", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value);
            // Mesmo CPF no organization B — não é conflito, cada clínica é cliente independente.
            seedContext.Patients.Add(Patient.Create(organizationB, "Paciente B", ValidCpf, ValidDataNascimento, "11988888888", null, null, true).Value);
            await seedContext.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var patients = await context.Patients.ToListAsync();

        Assert.That(patients, Has.Count.EqualTo(1));
        Assert.That(patients[0].OrganizationId, Is.EqualTo(organizationA));
        Assert.That(patients[0].NomeCompleto, Is.EqualTo("Paciente A"));
    }

    [Test]
    public async Task Should_ReturnNoPatients_When_OrganizationContextNotResolved()
    {
        var dbName = Guid.NewGuid().ToString();
        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Patients.Add(Patient.Create(Guid.NewGuid(), "Paciente A", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, new OrganizationContext());
        var patients = await context.Patients.ToListAsync();

        Assert.That(patients, Is.Empty);
    }

    // ── Regra: listagem paginada (via repositório) também respeita o filtro de organization ────────
    [Test]
    public async Task Should_ApplyOrganizationFilter_When_ListingThroughRepository()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedOrganizationContext = new OrganizationContext();
        await using (var seedContext = CreateContext(dbName, seedOrganizationContext))
        {
            seedContext.Patients.Add(Patient.Create(organizationA, "Ana Organization A", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value);
            seedContext.Patients.Add(Patient.Create(organizationB, "Bruno Organization B", ValidCpf, ValidDataNascimento, "11988888888", null, null, true).Value);
            await seedContext.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new PatientRepository(context);

        var (items, totalCount) = await repository.ListAsync(nome: null, cpf: null, includeInactive: false, page: 1, pageSize: 20);

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].NomeCompleto, Is.EqualTo("Ana Organization A"));
    }

    // ── Regra: soft delete não remove a linha, só o Ativo=false ──────────────────────────────
    [Test]
    public async Task Should_KeepRowInDatabase_When_PatientIsDeactivated()
    {
        var dbName = Guid.NewGuid().ToString();
        var organization = Guid.NewGuid();
        Guid patientId;

        var organizationContext = new OrganizationContext();
        organizationContext.SetOrganization(organization);

        await using (var context = CreateContext(dbName, organizationContext))
        {
            var patient = Patient.Create(organization, "Paciente X", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value;
            patientId = patient.Id;
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, organizationContext))
        {
            var patient = await context.Patients.FirstAsync(p => p.Id == patientId);
            patient.Desativar();
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, organizationContext))
        {
            // IncludeInactive via query filter global (organization) continua deixando a linha visível
            // — o soft delete é só a flag Ativo, não IsDeleted global filter.
            var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);

            Assert.That(patient, Is.Not.Null, "a linha nunca é removida fisicamente do banco");
            Assert.That(patient!.Ativo, Is.False);
        }
    }
}
