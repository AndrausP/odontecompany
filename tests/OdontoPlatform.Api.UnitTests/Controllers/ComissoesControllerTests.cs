using Identity.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OdontoPlatform.Api.Controllers;
using Reporting.Application.Queries.GetComissoesPorPeriodo;
using Reporting.Contracts;
using Scheduling.Contracts;
using SharedKernel;

namespace OdontoPlatform.Api.UnitTests.Controllers;

/// <summary>
/// Testa <see cref="ComissoesController"/> isolado (sem WebApplicationFactory, sem host real —
/// instancia a classe direto com mocks, ela é um POCO como qualquer outro). Não há convenção
/// prévia de *ControllerTests neste repo (nenhum outro controller é testado em isolamento hoje);
/// este arquivo cobre exclusivamente o RBAC de ownership do Dentista (D5, docs/tasks/023-*.md),
/// que é lógica de verdade — não delegação pura pro MediatR — e por isso vale o teste dedicado.
/// </summary>
[TestFixture]
public class ComissoesControllerTests
{
    private Mock<IMediator> _mediator = null!;
    private Mock<ICurrentUserAccessor> _currentUser = null!;
    private Mock<IProfissionalLookup> _profissionalLookup = null!;
    private ComissoesController _controller = null!;

    private Guid _organizationId;

    [SetUp]
    public void Setup()
    {
        _mediator = new Mock<IMediator>();
        _currentUser = new Mock<ICurrentUserAccessor>();
        _profissionalLookup = new Mock<IProfissionalLookup>();

        _organizationId = Guid.NewGuid();
        _currentUser.Setup(c => c.OrganizationId).Returns(_organizationId);

        _controller = new ComissoesController(_mediator.Object, _currentUser.Object, _profissionalLookup.Object);
    }

    // ── Dentista: filtros do cliente são ignorados e forçados pro próprio profissional ───────
    [Test]
    public async Task Should_IgnoreClientFiltersAndForceOwnProfissional_When_UsuarioEhDentista()
    {
        var userId = Guid.NewGuid();
        var proprioProfissionalId = Guid.NewGuid();
        var outroProfissionalId = Guid.NewGuid();
        var outroBranchId = Guid.NewGuid();

        _currentUser.Setup(c => c.Role).Returns("Dentista");
        _currentUser.Setup(c => c.UserId).Returns(userId);

        _profissionalLookup
            .Setup(p => p.ListarPorOrganizationAsync(_organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProfissionalResumoDto(proprioProfissionalId, "Dr. João", userId, Guid.NewGuid(), true)]);

        GetComissoesPorPeriodoQuery? queryEnviada = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<GetComissoesPorPeriodoQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<ComissaoResumoDto>>, CancellationToken>((q, _) => queryEnviada = (GetComissoesPorPeriodoQuery)q)
            .ReturnsAsync(Result.Success(BuildDto()));

        var dataInicio = new DateTime(2026, 8, 1);
        var dataFim = new DateTime(2026, 8, 31);

        // Dentista tenta pedir profissionalId/branchId/classe de outra pessoa via query string.
        var response = await _controller.GetComissoes(
            dataInicio, dataFim, branchId: outroBranchId, classe: "Owner", profissionalId: outroProfissionalId, CancellationToken.None);

        Assert.That(response, Is.InstanceOf<OkObjectResult>());
        Assert.That(queryEnviada, Is.Not.Null);
        Assert.That(queryEnviada!.ProfissionalId, Is.EqualTo(proprioProfissionalId), "profissionalId tem que ser forçado pro próprio, nunca o do cliente");
        Assert.That(queryEnviada.BranchId, Is.Null, "branchId do cliente tem que ser descartado pro Dentista");
        Assert.That(queryEnviada.Classe, Is.Null, "classe do cliente tem que ser descartada pro Dentista");
        Assert.That(queryEnviada.OrganizationId, Is.EqualTo(_organizationId));
    }

    // ── Dentista sem Profissional vinculado: sentinel Guid.Empty, sem erro ────────────────────
    [Test]
    public async Task Should_UseSentinel_When_DentistaHasNoProfissionalVinculado()
    {
        var userId = Guid.NewGuid();

        _currentUser.Setup(c => c.Role).Returns("Dentista");
        _currentUser.Setup(c => c.UserId).Returns(userId);

        // ListarPorOrganizationAsync não devolve nenhum profissional com este UserId.
        _profissionalLookup
            .Setup(p => p.ListarPorOrganizationAsync(_organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProfissionalResumoDto(Guid.NewGuid(), "Outro Dentista", Guid.NewGuid(), null, true)]);

        GetComissoesPorPeriodoQuery? queryEnviada = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<GetComissoesPorPeriodoQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<ComissaoResumoDto>>, CancellationToken>((q, _) => queryEnviada = (GetComissoesPorPeriodoQuery)q)
            .ReturnsAsync(Result.Success(BuildDto()));

        var response = await _controller.GetComissoes(
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), null, null, null, CancellationToken.None);

        Assert.That(response, Is.InstanceOf<OkObjectResult>());
        Assert.That(queryEnviada, Is.Not.Null);
        Assert.That(queryEnviada!.ProfissionalId, Is.EqualTo(Guid.Empty), "sentinel — Dentista sem Profissional vinculado, sem erro e sem dado de terceiro");
    }

    // ── Admin/Owner: filtros do cliente passam direto, sem sobrescrever ───────────────────────
    [Test]
    public async Task Should_PassThroughClientFilters_When_UsuarioEhAdmin()
    {
        var branchId = Guid.NewGuid();
        var profissionalId = Guid.NewGuid();

        _currentUser.Setup(c => c.Role).Returns("Admin");

        GetComissoesPorPeriodoQuery? queryEnviada = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<GetComissoesPorPeriodoQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<ComissaoResumoDto>>, CancellationToken>((q, _) => queryEnviada = (GetComissoesPorPeriodoQuery)q)
            .ReturnsAsync(Result.Success(BuildDto()));

        var response = await _controller.GetComissoes(
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), branchId, "Dentista", profissionalId, CancellationToken.None);

        Assert.That(response, Is.InstanceOf<OkObjectResult>());
        Assert.That(queryEnviada, Is.Not.Null);
        Assert.That(queryEnviada!.BranchId, Is.EqualTo(branchId));
        Assert.That(queryEnviada.Classe, Is.EqualTo("Dentista"));
        Assert.That(queryEnviada.ProfissionalId, Is.EqualTo(profissionalId));

        // Admin não força ProfissionalId a partir do UserId — não deve nem consultar o lookup.
        _profissionalLookup.Verify(
            p => p.ListarPorOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Sem organization resolvido no token → 401, nunca chega no mediator ────────────────────
    [Test]
    public async Task Should_ReturnUnauthorized_When_OrganizationIdIsNull()
    {
        _currentUser.Setup(c => c.OrganizationId).Returns((Guid?)null);

        var response = await _controller.GetComissoes(
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), null, null, null, CancellationToken.None);

        Assert.That(response, Is.InstanceOf<UnauthorizedObjectResult>());
        _mediator.Verify(m => m.Send(It.IsAny<GetComissoesPorPeriodoQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Handler falhou (ex: período inválido) → BadRequest, não exceção ──────────────────────
    [Test]
    public async Task Should_ReturnBadRequest_When_HandlerReturnsFailure()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");

        _mediator
            .Setup(m => m.Send(It.IsAny<GetComissoesPorPeriodoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ComissaoResumoDto>(new Error("Validation.Failure", "Data fim não pode ser anterior à data início.")));

        var response = await _controller.GetComissoes(
            new DateTime(2026, 8, 31), new DateTime(2026, 8, 1), null, null, null, CancellationToken.None);

        Assert.That(response, Is.InstanceOf<BadRequestObjectResult>());
    }

    private static ComissaoResumoDto BuildDto()
        => new(Guid.NewGuid(), new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), 31, 0m, 0m, 0, 0m, 0m, []);
}
