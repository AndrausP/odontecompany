using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Patients.Domain.Entities;
using Patients.Infrastructure.Lookups;
using Patients.Infrastructure.Persistence;

namespace Patients.UnitTests.Persistence;

[TestFixture]
public class PatientSummaryProviderTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    private static PatientsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PatientsDbContext>().UseInMemoryDatabase(dbName).Options;
        return new PatientsDbContext(options, new OrganizationContext());
    }

    [Test]
    public async Task Should_CountOnlyAtivosFromRequestedOrganization_When_ContarAtivosAsyncIsCalled()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName))
        {
            var ativoOrganizationA = Patient.Create(organizationA, "Ativo A", ValidCpf, ValidDataNascimento, "11999999999", null, null, true).Value;
            var inativoOrganizationA = Patient.Create(organizationA, "Inativo A", "11144477735", ValidDataNascimento, "11999999998", null, null, true).Value;
            inativoOrganizationA.Desativar();
            var ativoOrganizationB = Patient.Create(organizationB, "Ativo B", ValidCpf, ValidDataNascimento, "11988888888", null, null, true).Value;

            seedContext.Patients.AddRange(ativoOrganizationA, inativoOrganizationA, ativoOrganizationB);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var provider = new PatientSummaryProvider(context);

        var count = await provider.ContarAtivosAsync(organizationA);

        // Só o ativo do organization A — o inativo do próprio organization A não conta, e o ativo do
        // organization B não vaza pro resumo do organization A.
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_ReturnZero_When_OrganizationHasNoActivePatients()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var provider = new PatientSummaryProvider(context);

        var count = await provider.ContarAtivosAsync(Guid.NewGuid());

        Assert.That(count, Is.EqualTo(0));
    }
}
