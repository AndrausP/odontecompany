using Moq;
using Scheduling.Application.Commands.MarcarAgendamentoConcluido;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class MarcarAgendamentoConcluidoCommandHandlerTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);

    private Mock<IAgendamentoRepository> _agendamentoRepository = null!;
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private MarcarAgendamentoConcluidoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _agendamentoRepository = new Mock<IAgendamentoRepository>();
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new MarcarAgendamentoConcluidoCommandHandler(_agendamentoRepository.Object, _profissionalRepository.Object, _unitOfWork.Object);
    }

    private static Agendamento CriarAgendamentoConfirmado()
    {
        var agendamento = Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;
        agendamento.Confirmar();
        return agendamento;
    }

    [Test]
    public async Task Should_MarkAsConcluido_When_AgendamentoIsConfirmado()
    {
        var agendamento = CriarAgendamentoConfirmado();
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);

        var result = await _handler.Handle(new MarcarAgendamentoConcluidoCommand(agendamento.Id, 200m), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Status, Is.EqualTo("Concluido"));
        Assert.That(result.Value.ValorConsulta, Is.EqualTo(200m));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoNotFound()
    {
        _agendamentoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Agendamento?)null);

        var result = await _handler.Handle(new MarcarAgendamentoConcluidoCommand(Guid.NewGuid(), 200m), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.NaoEncontrado"));
    }

    [Test]
    public async Task Should_ReturnFailure_When_AgendamentoIsNotConfirmado()
    {
        var agendamento = Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;
        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);

        var result = await _handler.Handle(new MarcarAgendamentoConcluidoCommand(agendamento.Id, 200m), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoConcluiSeConfirmado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IMP-2 (QA/jubileu): RBAC de ownership de agenda ───────────────────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_DentistaTriesToConcludeAnotherDentistaAgenda()
    {
        var agendamento = CriarAgendamentoConfirmado();
        var donoDaAgenda = Guid.NewGuid();
        var outroDentista = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", donoDaAgenda).Value);

        var result = await _handler.Handle(new MarcarAgendamentoConcluidoCommand(agendamento.Id, 200m, outroDentista, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SemPermissaoAgendaAlheia"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_MarkAsConcluido_When_DentistaConcludesOwnAgenda()
    {
        var agendamento = CriarAgendamentoConfirmado();
        var dentistaUserId = Guid.NewGuid();

        _agendamentoRepository.Setup(r => r.GetByIdAsync(agendamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agendamento);
        _profissionalRepository.Setup(r => r.GetByIdAsync(agendamento.ProfissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", dentistaUserId).Value);

        var result = await _handler.Handle(new MarcarAgendamentoConcluidoCommand(agendamento.Id, 200m, dentistaUserId, "Dentista"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
