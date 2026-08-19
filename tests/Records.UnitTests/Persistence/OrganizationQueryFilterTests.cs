using Infrastructure.Common.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Records.Domain.Entities;
using Records.Domain.Enums;
using Records.Infrastructure.Persistence;
using Records.Infrastructure.Repositories;
using Records.Infrastructure.Security;

namespace Records.UnitTests.Persistence;

/// <summary>
/// Regressão pra <c>ApplyOrganizationQueryFilters</c> aplicada ao módulo Records — cobre o gap
/// levantado na task 019 (isolamento cross-org em TODOS os módulos de negócio, não só Identity).
/// Reusa o padrão já existente em Identity/Patients/Scheduling/Estoque/Tenancy: DbContext
/// InMemory de verdade, dois organizations diferentes seedados, consulta a partir de um contexto
/// escopado só na primeira organization.
///
/// Especificidade do módulo Records: <c>RecordsDbContext</c> recebe <c>IEncryptionService</c> no
/// construtor pra o ValueConverter de <c>EvolucaoClinica.DescricaoClinica</c>. Reusamos a mesma
/// <see cref="AesEncryptionService"/> real (não fake) com <see cref="RecordsEncryptionOptions"/>
/// de teste — mesmo padrão do <c>AesEncryptionServiceTests</c>, não inventamos stub novo.
/// </summary>
[TestFixture]
public class OrganizationQueryFilterTests
{
    private const string TestEncryptionKey = "records-query-filter-tests-key-32bytes";

    private static AesEncryptionService CreateEncryptionService()
        => new(Options.Create(new RecordsEncryptionOptions { KeyBase64 = TestEncryptionKey }));

    private static RecordsDbContext CreateContext(string dbName, IOrganizationContext organizationContext)
    {
        var options = new DbContextOptionsBuilder<RecordsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RecordsDbContext(options, organizationContext, CreateEncryptionService());
    }

    // ── Prontuário: filtro global aplica organization_id ──────────────────────────────────────

    [Test]
    public async Task Should_ReturnOnlyProntuariosFromCurrentOrganization_When_QueryFilterApplied()
    {
        // Arrange — dois organizations, um prontuário em cada.
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Prontuarios.Add(Prontuario.Create(organizationA, Guid.NewGuid(), Guid.NewGuid()).Value);
            seed.Prontuarios.Add(Prontuario.Create(organizationB, Guid.NewGuid(), Guid.NewGuid()).Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var prontuarios = await context.Prontuarios.ToListAsync();

        // Assert
        Assert.That(prontuarios, Has.Count.EqualTo(1),
            "dentista da organization A não pode ver prontuários da organization B — vazamento clínico é LGPD");
        Assert.That(prontuarios[0].OrganizationId, Is.EqualTo(organizationA));
    }

    [Test]
    public async Task Should_ReturnNoProntuarios_When_OrganizationContextNotResolved()
    {
        // Arrange — fail-closed: sem organization resolvido, o filtro global corta tudo.
        var dbName = Guid.NewGuid().ToString();
        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Prontuarios.Add(Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value);
            await seed.SaveChangesAsync();
        }

        // Act — CurrentOrganizationId == null
        await using var context = CreateContext(dbName, new OrganizationContext());
        var prontuarios = await context.Prontuarios.ToListAsync();

        // Assert
        Assert.That(prontuarios, Is.Empty,
            "sem organization resolvido no contexto, prontuário nunca vaza — fail-closed é obrigatório em módulo clínico");
    }

    // ── EvolucaoClinica: filtro global aplica organization_id (mesmo com DescricaoClinica cifrada) ──

    [Test]
    public async Task Should_ReturnOnlyEvolucoesFromCurrentOrganization_When_QueryFilterApplied()
    {
        // Arrange — evolução clínica passa por ValueConverter (encrypt/decrypt) NO CAMPO
        // DescricaoClinica. Este teste garante que o filtro global de organization funciona MESMO
        // com o ValueConverter no meio — regressão pro caso do filtro ser aplicado antes/depois da
        // conversão gerar comportamento errado.
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var prontuarioA = Prontuario.Create(organizationA, Guid.NewGuid(), Guid.NewGuid()).Value;
        var prontuarioB = Prontuario.Create(organizationB, Guid.NewGuid(), Guid.NewGuid()).Value;

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Prontuarios.AddRange(prontuarioA, prontuarioB);
            seed.EvolucoesClinicas.Add(EvolucaoClinica.Create(
                organizationA, prontuarioA.Id, Guid.NewGuid(), TipoProcedimento.Consulta, "Dor no dente 26 (organization A).").Value);
            seed.EvolucoesClinicas.Add(EvolucaoClinica.Create(
                organizationB, prontuarioB.Id, Guid.NewGuid(), TipoProcedimento.Avaliacao, "Dor no dente 11 (organization B).").Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var evolucoes = await context.EvolucoesClinicas.ToListAsync();

        // Assert
        Assert.That(evolucoes, Has.Count.EqualTo(1));
        Assert.That(evolucoes[0].OrganizationId, Is.EqualTo(organizationA));
        Assert.That(evolucoes[0].DescricaoClinica, Does.Contain("organization A"),
            "ValueConverter decripta ao ler — texto puro no Domain, ciphertext só no banco");
    }

    // ── AnexoMetadata: filtro global aplica organization_id ─────────────────────────────────

    [Test]
    public async Task Should_ReturnOnlyAnexosFromCurrentOrganization_When_QueryFilterApplied()
    {
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var prontuarioAId = Guid.NewGuid();
        var prontuarioBId = Guid.NewGuid();

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.AnexosMetadata.Add(AnexoMetadata.Create(
                organizationA, prontuarioAId, Guid.NewGuid(), "raio-x-a.jpg", "image/jpeg", 1024, "storage/a/raio-x-a.jpg").Value);
            seed.AnexosMetadata.Add(AnexoMetadata.Create(
                organizationB, prontuarioBId, Guid.NewGuid(), "raio-x-b.jpg", "image/jpeg", 1024, "storage/b/raio-x-b.jpg").Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        await using var context = CreateContext(dbName, organizationAContext);
        var anexos = await context.AnexosMetadata.ToListAsync();

        Assert.That(anexos, Has.Count.EqualTo(1));
        Assert.That(anexos[0].OrganizationId, Is.EqualTo(organizationA));
        Assert.That(anexos[0].NomeArquivo, Is.EqualTo("raio-x-a.jpg"));
    }

    // ── Repositório: ListEvolucoes/ListAnexos por prontuarioId ainda respeita o filtro de org ──

    [Test]
    public async Task Should_ApplyOrganizationFilter_When_ListingEvolucoesThroughRepository()
    {
        // Arrange — mesmo pacienteId em dois organizations diferentes (colisão intencional pra
        // provar que o filtro não vaza entre organizations mesmo com identifiers coincidentes).
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var prontuarioA = Prontuario.Create(organizationA, Guid.NewGuid(), Guid.NewGuid()).Value;
        var prontuarioB = Prontuario.Create(organizationB, Guid.NewGuid(), Guid.NewGuid()).Value;

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Prontuarios.AddRange(prontuarioA, prontuarioB);
            seed.EvolucoesClinicas.Add(EvolucaoClinica.Create(
                organizationA, prontuarioA.Id, Guid.NewGuid(), TipoProcedimento.Restauracao, "Restauração dente 16 (org A).").Value);
            seed.EvolucoesClinicas.Add(EvolucaoClinica.Create(
                organizationB, prontuarioB.Id, Guid.NewGuid(), TipoProcedimento.Restauracao, "Restauração dente 16 (org B).").Value);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act — repositório sempre passa pelo DbContext, então herda o filtro global.
        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new ProntuarioRepository(context);

        var evolucoesProntuarioA = await repository.ListEvolucoesAsync(prontuarioA.Id);
        var evolucoesProntuarioB = await repository.ListEvolucoesAsync(prontuarioB.Id);

        // Assert
        Assert.That(evolucoesProntuarioA, Has.Count.EqualTo(1));
        Assert.That(evolucoesProntuarioA[0].OrganizationId, Is.EqualTo(organizationA));
        Assert.That(evolucoesProntuarioB, Is.Empty,
            "consultar prontuário de outro organization pelo id direto NÃO pode retornar nada — filtro global corta antes do Where");
    }

    // ── Cross-org: repositório NÃO enxerga prontuário da outra organization nem por id direto ──

    [Test]
    public async Task Should_ReturnNull_When_GettingProntuarioByIdFromDifferentOrganization()
    {
        // Arrange — bug clássico de "enumeração por id": token de organization A tenta buscar
        // um prontuário do organization B só sabendo o guid. Filtro global tem que cortar antes.
        var dbName = Guid.NewGuid().ToString();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var prontuarioB = Prontuario.Create(organizationB, Guid.NewGuid(), Guid.NewGuid()).Value;

        var seedContext = new OrganizationContext();
        await using (var seed = CreateContext(dbName, seedContext))
        {
            seed.Prontuarios.Add(prontuarioB);
            await seed.SaveChangesAsync();
        }

        var organizationAContext = new OrganizationContext();
        organizationAContext.SetOrganization(organizationA);

        // Act
        await using var context = CreateContext(dbName, organizationAContext);
        var repository = new ProntuarioRepository(context);

        var found = await repository.GetByIdAsync(prontuarioB.Id);

        // Assert
        Assert.That(found, Is.Null,
            "GetByIdAsync a partir de contexto de outro organization tem que devolver null — nunca vazar existência da entidade");
    }
}
