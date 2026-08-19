using Billing.Contracts;
using Identity.Contracts;
using Moq;
using Reporting.Application.Queries.GetComissoesPorPeriodo;
using Scheduling.Contracts;
using Tenancy.Contracts;

namespace Reporting.UnitTests.Queries;

[TestFixture]
public class GetComissoesPorPeriodoQueryHandlerTests
{
    private Mock<IComissaoSummaryProvider> _comissaoSummaryProvider = null!;
    private Mock<IProfissionalLookup> _profissionalLookup = null!;
    private Mock<IMembershipLookup> _membershipLookup = null!;
    private Mock<IBranchLookup> _branchLookup = null!;
    private GetComissoesPorPeriodoQueryHandler _handler = null!;

    private Guid _organizationId;
    private DateTime _inicio;
    private DateTime _fim;

    [SetUp]
    public void Setup()
    {
        _comissaoSummaryProvider = new Mock<IComissaoSummaryProvider>();
        _profissionalLookup = new Mock<IProfissionalLookup>();
        _membershipLookup = new Mock<IMembershipLookup>();
        _branchLookup = new Mock<IBranchLookup>();

        _handler = new GetComissoesPorPeriodoQueryHandler(
            _comissaoSummaryProvider.Object,
            _profissionalLookup.Object,
            _membershipLookup.Object,
            _branchLookup.Object);

        _organizationId = Guid.NewGuid();
        _inicio = new DateTime(2026, 8, 1);
        _fim = new DateTime(2026, 8, 31);
    }

    // ── Happy path: agregado sem filtro ──────────────────────────────────────────────────────
    [Test]
    public async Task Should_ComposeAllFourProviders_When_NoFiltersApplied()
    {
        var branchId = Guid.NewGuid();
        var userIdDentista = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();

        var profissional = new ProfissionalResumoDto(profissionalId, "Dr. João", userIdDentista, branchId, true);

        SetupProviders(
            comissoes: [new ComissaoPorProfissionalDto(profissionalId, 1000m, 300m, 2, 3)],
            profissionais: [profissional],
            memberships: [new MembershipResumoDto(userIdDentista, "Dentista", branchId, true)],
            branchNomes: new Dictionary<Guid, string> { [branchId] = "Filial Centro" });

        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Has.Count.EqualTo(1));
        Assert.That(result.Value.Linhas[0].ProfissionalNome, Is.EqualTo("Dr. João"));
        Assert.That(result.Value.Linhas[0].BranchNome, Is.EqualTo("Filial Centro"));
        Assert.That(result.Value.Linhas[0].Classe, Is.EqualTo("Dentista"));
        Assert.That(result.Value.ValorComissaoTotal, Is.EqualTo(300m));
        Assert.That(result.Value.ValorPagoTotal, Is.EqualTo(1000m));
        Assert.That(result.Value.QuantidadeFaturas, Is.EqualTo(2));
    }

    // ── Filtro por branchId ───────────────────────────────────────────────────────────────────
    [Test]
    public async Task Should_ReturnOnlyProfissionaisFromBranch_When_BranchIdInformado()
    {
        var branchAlvo = Guid.NewGuid();
        var branchOutra = Guid.NewGuid();
        var profissionalDaBranchAlvo = Guid.NewGuid();
        var profissionalDaOutraBranch = Guid.NewGuid();
        var userAlvo = Guid.NewGuid();
        var userOutro = Guid.NewGuid();

        SetupProviders(
            comissoes:
            [
                new ComissaoPorProfissionalDto(profissionalDaBranchAlvo, 500m, 100m, 1, 1),
                new ComissaoPorProfissionalDto(profissionalDaOutraBranch, 800m, 200m, 1, 1)
            ],
            profissionais:
            [
                new ProfissionalResumoDto(profissionalDaBranchAlvo, "Dra. Ana", userAlvo, branchAlvo, true),
                new ProfissionalResumoDto(profissionalDaOutraBranch, "Dr. Bruno", userOutro, branchOutra, true)
            ],
            memberships:
            [
                new MembershipResumoDto(userAlvo, "Dentista", branchAlvo, true),
                new MembershipResumoDto(userOutro, "Dentista", branchOutra, true)
            ],
            branchNomes: new Dictionary<Guid, string> { [branchAlvo] = "Filial A", [branchOutra] = "Filial B" });

        var query = BuildQuery(branchId: branchAlvo);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Has.Count.EqualTo(1));
        Assert.That(result.Value.Linhas[0].ProfissionalId, Is.EqualTo(profissionalDaBranchAlvo));
        Assert.That(result.Value.ValorComissaoTotal, Is.EqualTo(100m));
    }

    // ── Filtro por classe/role ────────────────────────────────────────────────────────────────
    [Test]
    public async Task Should_ReturnOnlyRoleInformado_When_ClasseInformada()
    {
        var profissionalDentista = Guid.NewGuid();
        var profissionalOwner = Guid.NewGuid();
        var userDentista = Guid.NewGuid();
        var userOwner = Guid.NewGuid();

        SetupProviders(
            comissoes:
            [
                new ComissaoPorProfissionalDto(profissionalDentista, 500m, 100m, 1, 1),
                new ComissaoPorProfissionalDto(profissionalOwner, 800m, 200m, 1, 1)
            ],
            profissionais:
            [
                new ProfissionalResumoDto(profissionalDentista, "Dra. Ana", userDentista, null, true),
                new ProfissionalResumoDto(profissionalOwner, "Dr. Bruno", userOwner, null, true)
            ],
            memberships:
            [
                new MembershipResumoDto(userDentista, "Dentista", null, true),
                new MembershipResumoDto(userOwner, "Owner", null, true)
            ],
            branchNomes: new Dictionary<Guid, string>());

        var query = BuildQuery(classe: "Dentista");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Has.Count.EqualTo(1));
        Assert.That(result.Value.Linhas[0].ProfissionalId, Is.EqualTo(profissionalDentista));
        Assert.That(result.Value.Linhas[0].Classe, Is.EqualTo("Dentista"));
    }

    // ── Filtro por profissionalId ─────────────────────────────────────────────────────────────
    [Test]
    public async Task Should_ReturnOnlyOneProfissional_When_ProfissionalIdInformado()
    {
        var profissionalAlvo = Guid.NewGuid();
        var profissionalOutro = Guid.NewGuid();
        var userAlvo = Guid.NewGuid();
        var userOutro = Guid.NewGuid();

        SetupProviders(
            comissoes:
            [
                new ComissaoPorProfissionalDto(profissionalAlvo, 500m, 100m, 1, 1),
                new ComissaoPorProfissionalDto(profissionalOutro, 800m, 200m, 1, 1)
            ],
            profissionais:
            [
                new ProfissionalResumoDto(profissionalAlvo, "Dra. Ana", userAlvo, null, true),
                new ProfissionalResumoDto(profissionalOutro, "Dr. Bruno", userOutro, null, true)
            ],
            memberships:
            [
                new MembershipResumoDto(userAlvo, "Dentista", null, true),
                new MembershipResumoDto(userOutro, "Dentista", null, true)
            ],
            branchNomes: new Dictionary<Guid, string>());

        var query = BuildQuery(profissionalId: profissionalAlvo);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Has.Count.EqualTo(1));
        Assert.That(result.Value.Linhas[0].ProfissionalId, Is.EqualTo(profissionalAlvo));
        Assert.That(result.Value.ValorComissaoTotal, Is.EqualTo(100m));
    }

    // ── Sentinel Guid.Empty: Dentista sem Profissional vinculado ─────────────────────────────
    // MontarLinhaZerada NUNCA dispara pro sentinel: profissionalPorId.TryGetValue(Guid.Empty, ...)
    // falha de propósito (nenhum Profissional.Id real é Guid.Empty). Resultado tem que ser 200
    // com Linhas vazia e totais 0, sem exception e sem confundir com "profissional real zerado".
    [Test]
    public async Task Should_ReturnEmptyListAndZeroedTotals_When_ProfissionalIdIsSentinelAndNotFound()
    {
        SetupProviders(
            comissoes: [],
            profissionais: [],
            memberships: [],
            branchNomes: new Dictionary<Guid, string>());

        var query = BuildQuery(profissionalId: Guid.Empty);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Is.Empty);
        Assert.That(result.Value.ValorComissaoTotal, Is.EqualTo(0m));
        Assert.That(result.Value.ValorPagoTotal, Is.EqualTo(0m));
        Assert.That(result.Value.QuantidadeFaturas, Is.EqualTo(0));
        Assert.That(result.Value.MediaDiariaComissao, Is.EqualTo(0m));
    }

    // ── Profissional real existe, mas sem comissão no período: MontarLinhaZerada dispara ─────
    // Contraste explícito com o sentinel acima: aqui o profissionalId EXISTE em profissionalPorId,
    // então a linha zerada é montada (1 item, valores 0) em vez de lista vazia.
    [Test]
    public async Task Should_ReturnSingleZeroedLine_When_ProfissionalExistsButHasNoCommissionInPeriod()
    {
        var profissionalId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        SetupProviders(
            comissoes: [],
            profissionais: [new ProfissionalResumoDto(profissionalId, "Dra. Ana", userId, null, true)],
            memberships: [new MembershipResumoDto(userId, "Dentista", null, true)],
            branchNomes: new Dictionary<Guid, string>());

        var query = BuildQuery(profissionalId: profissionalId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Linhas, Has.Count.EqualTo(1));
        Assert.That(result.Value.Linhas[0].ProfissionalId, Is.EqualTo(profissionalId));
        Assert.That(result.Value.Linhas[0].ValorComissao, Is.EqualTo(0m));
        Assert.That(result.Value.Linhas[0].ValorPago, Is.EqualTo(0m));
        Assert.That(result.Value.ValorComissaoTotal, Is.EqualTo(0m));
    }

    // ── Período de 1 dia: sem divisão por zero ────────────────────────────────────────────────
    [Test]
    public async Task Should_CalculateOneDayPeriod_When_DataInicioEqualsDataFim()
    {
        var dia = new DateTime(2026, 8, 10);

        SetupProviders(
            comissoes: [],
            profissionais: [],
            memberships: [],
            branchNomes: new Dictionary<Guid, string>());

        var query = new GetComissoesPorPeriodoQuery(_organizationId, dia, dia, null, null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.DiasNoPeriodo, Is.EqualTo(1));
        Assert.That(result.Value.MediaDiariaComissao, Is.EqualTo(0m));
    }

    // ── Média diária: total ÷ dias ────────────────────────────────────────────────────────────
    [Test]
    public async Task Should_CalculateMediaDiaria_When_MultiplosDiasNoPeriodo()
    {
        var profissionalId = Guid.NewGuid();
        var inicio = new DateTime(2026, 8, 1);
        var fim = new DateTime(2026, 8, 10); // 10 dias inclusivo

        SetupProviders(
            comissoes: [new ComissaoPorProfissionalDto(profissionalId, 1000m, 500m, 4, 4)],
            profissionais: [new ProfissionalResumoDto(profissionalId, "Dra. Ana", Guid.NewGuid(), null, true)],
            memberships: [],
            branchNomes: new Dictionary<Guid, string>());

        var query = new GetComissoesPorPeriodoQuery(_organizationId, inicio, fim, null, null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.DiasNoPeriodo, Is.EqualTo(10));
        Assert.That(result.Value.MediaDiariaComissao, Is.EqualTo(50m)); // 500 / 10
        Assert.That(result.Value.MediaDiariaValorPago, Is.EqualTo(100m)); // 1000 / 10
        Assert.That(result.Value.Linhas[0].MediaDiariaComissao, Is.EqualTo(50m));
    }

    // ── Arredondamento: 2 casas, banker's rounding (ToEven) — não AwayFromZero ───────────────
    [Test]
    public async Task Should_RoundMediaDiariaToEven_When_ResultIsExactMidpoint()
    {
        var profissionalId = Guid.NewGuid();
        var inicio = new DateTime(2026, 8, 1);
        var fim = new DateTime(2026, 8, 2); // 2 dias

        // 100.25 / 2 = 50.125 exato — ponto médio entre 50.12 e 50.13.
        // ToEven escolhe 50.12 (2 é par); AwayFromZero escolheria 50.13.
        SetupProviders(
            comissoes: [new ComissaoPorProfissionalDto(profissionalId, 0m, 100.25m, 1, 1)],
            profissionais: [new ProfissionalResumoDto(profissionalId, "Dra. Ana", Guid.NewGuid(), null, true)],
            memberships: [],
            branchNomes: new Dictionary<Guid, string>());

        var query = new GetComissoesPorPeriodoQuery(_organizationId, inicio, fim, null, null, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.MediaDiariaComissao, Is.EqualTo(50.12m));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────────────────

    private GetComissoesPorPeriodoQuery BuildQuery(Guid? branchId = null, string? classe = null, Guid? profissionalId = null)
        => new(_organizationId, _inicio, _fim, branchId, classe, profissionalId);

    private void SetupProviders(
        IReadOnlyList<ComissaoPorProfissionalDto> comissoes,
        IReadOnlyList<ProfissionalResumoDto> profissionais,
        IReadOnlyList<MembershipResumoDto> memberships,
        IReadOnlyDictionary<Guid, string> branchNomes)
    {
        _comissaoSummaryProvider
            .Setup(p => p.ObterComissoesAsync(_organizationId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comissoes);
        _profissionalLookup
            .Setup(p => p.ListarPorOrganizationAsync(_organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissionais);
        _membershipLookup
            .Setup(m => m.ListarPorOrganizationAsync(_organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberships);
        _branchLookup
            .Setup(b => b.ListarNomesAsync(_organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branchNomes);
    }
}
