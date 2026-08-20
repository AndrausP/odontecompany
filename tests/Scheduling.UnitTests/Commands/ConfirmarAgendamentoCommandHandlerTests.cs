using Moq;
using Scheduling.Application.Commands.ConfirmarAgendamento;
using Scheduling.Application.Exceptions;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class ConfirmarAgendamentoCommandHandlerTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);
    private const string LockToken = "fake-lock-token";

    private Mock<IAgendamentoRepository> _agendamentoRepository = null!;
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IRedisLockService> _lockService = null!;
    private Mock<IAvailabilityCache> _availabilityCache = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private ConfirmarAgendamentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _agendamentoRepository = new Mock<IAgendamentoRepository>();
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _lockService = new Mock<IRedisLockService>();
        _availabilityCache = new Mock<IAvailabilityCache>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _handler = new ConfirmarAgendamentoCommandHandler(
            _agendamentoRepository.Object, _profissionalRepository.Object, _lockService.Object, _availabilityCache.Object, _unitOfWork.Object);

        _lockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockToken);
    }

    private static Agendamento CriarAgendamentoValido()
        => Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;

    [Test]
    public async Task Should_Confirm_When_AgendadoAndNoOverlapAndLockAcquired()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, ValidInicio, ValidFim, agendamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Status, Is.EqualTo("Confirmado"));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _availabilityCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _lockService.Verify(l => l.ReleaseAsync(It.IsAny<string>(), LockToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoNotFound()
    {
        _agendamentoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Agendamento?)null);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.NaoEncontrado"));
        _lockService.Verify(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regra: lock indisponível falha sem tentar confirmar ──────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_LockCannotBeAcquired()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _lockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.LockIndisponivel"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regra: sobreposição revalidada depois do lock também falha ───────────────────────────
    [Test]
    public async Task Should_ReturnFailure_And_ReleaseLock_When_OverlapDetectedAfterLockAcquired()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, ValidInicio, ValidFim, agendamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.HorarioIndisponivel"));
        _lockService.Verify(l => l.ReleaseAsync(It.IsAny<string>(), LockToken, It.IsAny<CancellationToken>()), Times.Once, "lock precisa ser liberado mesmo em caso de falha");
    }

    // ── Regressão: conflito de concorrência otimista (xmin) vira Result.Failure, nunca exception vazando ──
    [Test]
    public async Task Should_ReturnConflitoDeConcorrencia_When_SaveChangesThrowsConcurrencyConflictException()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, ValidInicio, ValidFim, agendamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new Exception("xmin mismatch")));

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.ConflitoDeConcorrencia"));
        _lockService.Verify(l => l.ReleaseAsync(It.IsAny<string>(), LockToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoIsNotAgendado()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Cancelar();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoConfirmaSeAgendado"));
    }

    // ── IMP-2 (QA/jubileu): RBAC de ownership de agenda ───────────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_DentistaTriesToManageAnotherDentistaAgenda()
    {
        var agendamento = CriarAgendamentoValido();
        var donoDaAgenda = Guid.NewGuid();
        var outroDentista = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt, userId: donoDaAgenda).Value);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id, outroDentista, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SemPermissaoAgendaAlheia"));
        _lockService.Verify(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Confirm_When_DentistaManagesOwnAgenda()
    {
        var agendamento = CriarAgendamentoValido();
        var dentistaUserId = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt, userId: dentistaUserId).Value);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, ValidInicio, ValidFim, agendamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id, dentistaUserId, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Should_Confirm_When_AdminManagesAnyAgenda_RegardlessOfOwnership()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(
                agendamento.ProfissionalId, ValidInicio, ValidFim, agendamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new ConfirmarAgendamentoCommand(agendamento.Id, Guid.NewGuid(), "Admin"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _profissionalRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never, "Admin não precisa de checagem de ownership");
    }
}
