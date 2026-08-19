using Moq;
using Scheduling.Application.Commands.CancelarAgendamento;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class CancelarAgendamentoCommandHandlerTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);

    private Mock<IAgendamentoRepository> _agendamentoRepository = null!;
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IAvailabilityCache> _availabilityCache = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CancelarAgendamentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _agendamentoRepository = new Mock<IAgendamentoRepository>();
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _availabilityCache = new Mock<IAvailabilityCache>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CancelarAgendamentoCommandHandler(
            _agendamentoRepository.Object, _profissionalRepository.Object, _availabilityCache.Object, _unitOfWork.Object);
    }

    private static Agendamento CriarAgendamentoValido()
        => Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;

    [Test]
    public async Task Should_Cancel_When_AgendamentoIsAgendado()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(agendamento.Id, "paciente desmarcou"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _availabilityCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoNotFound()
    {
        _agendamentoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Agendamento?)null);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(Guid.NewGuid(), null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoIsAlreadyConcluido()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Confirmar();
        agendamento.MarcarConcluido(100m);

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(agendamento.Id, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoCancelaSeAgendadoOuConfirmado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IMP-2 (QA/jubileu): RBAC de ownership de agenda ───────────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_DentistaTriesToCancelAnotherDentistaAgenda()
    {
        var agendamento = CriarAgendamentoValido();
        var donoDaAgenda = Guid.NewGuid();
        var outroDentista = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", donoDaAgenda).Value);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(agendamento.Id, null, outroDentista, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SemPermissaoAgendaAlheia"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Cancel_When_DentistaCancelsOwnAgenda()
    {
        var agendamento = CriarAgendamentoValido();
        var dentistaUserId = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", dentistaUserId).Value);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(agendamento.Id, null, dentistaUserId, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Should_Cancel_When_RecepcaoCancelsAnyAgenda_RegardlessOfOwnership()
    {
        var agendamento = CriarAgendamentoValido();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);

        var result = await _handler.Handle(new CancelarAgendamentoCommand(agendamento.Id, null, Guid.NewGuid(), "Recepcao"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _profissionalRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never, "Recepcao não precisa de checagem de ownership");
    }
}
