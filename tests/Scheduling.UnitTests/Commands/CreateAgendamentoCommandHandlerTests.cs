using Moq;
using Patients.Contracts;
using Scheduling.Application.Commands.CreateAgendamento;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class CreateAgendamentoCommandHandlerTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);
    private const string LockToken = "fake-lock-token";

    private Mock<IAgendamentoRepository> _agendamentoRepository = null!;
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<ISalaRepository> _salaRepository = null!;
    private Mock<IPatientLookup> _patientLookup = null!;
    private Mock<IRedisLockService> _lockService = null!;
    private Mock<IAvailabilityCache> _availabilityCache = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateAgendamentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _agendamentoRepository = new Mock<IAgendamentoRepository>();
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _salaRepository = new Mock<ISalaRepository>();
        _patientLookup = new Mock<IPatientLookup>();
        _lockService = new Mock<IRedisLockService>();
        _availabilityCache = new Mock<IAvailabilityCache>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _lockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockToken);

        _handler = new CreateAgendamentoCommandHandler(
            _agendamentoRepository.Object,
            _profissionalRepository.Object,
            _salaRepository.Object,
            _patientLookup.Object,
            _lockService.Object,
            _availabilityCache.Object,
            _unitOfWork.Object);
    }

    private static CreateAgendamentoCommand ValidCommand(Guid? organizationId = null, Guid? profissionalId = null, Guid? salaId = null) => new(
        organizationId ?? Guid.NewGuid(),
        Guid.NewGuid(),
        profissionalId ?? Guid.NewGuid(),
        salaId ?? Guid.NewGuid(),
        ValidInicio,
        ValidFim);

    private void SetupHappyPath(Guid profissionalId, Guid salaId)
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value);
        _salaRepository.Setup(r => r.GetByIdAsync(salaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sala.Criar(Guid.NewGuid(), "Sala 1").Value);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(profissionalId, ValidInicio, ValidFim, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Test]
    public async Task Should_CreateAgendamento_When_AllValidationsPass()
    {
        var profissionalId = Guid.NewGuid();
        var salaId = Guid.NewGuid();
        SetupHappyPath(profissionalId, salaId);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId, salaId: salaId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Status, Is.EqualTo("Agendado"));

        _agendamentoRepository.Verify(r => r.AddAsync(It.IsAny<Agendamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _availabilityCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Regra: paciente inexistente falha antes de qualquer escrita ──────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_PacienteDoesNotExist()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.PacienteNaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProfissionalDoesNotExist()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Profissional?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.ProfissionalNaoEncontrado"));
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProfissionalIsInativo()
    {
        var profissionalId = Guid.NewGuid();
        var profissionalInativo = Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value;
        profissionalInativo.Desativar();

        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>())).ReturnsAsync(profissionalInativo);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.ProfissionalNaoEncontrado"));
    }

    [Test]
    public async Task Should_ReturnFailure_When_SalaDoesNotExist()
    {
        var profissionalId = Guid.NewGuid();
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value);
        _salaRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Sala?)null);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SalaNaoEncontrada"));
    }

    // ── Regra: sobreposição de horário do mesmo profissional falha ───────────────────────────
    [Test]
    public async Task Should_ReturnFailure_When_HorarioOverlapsExistingAgendamento()
    {
        var profissionalId = Guid.NewGuid();
        var salaId = Guid.NewGuid();

        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value);
        _salaRepository.Setup(r => r.GetByIdAsync(salaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sala.Criar(Guid.NewGuid(), "Sala 1").Value);
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(profissionalId, ValidInicio, ValidFim, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId, salaId: salaId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.HorarioIndisponivel"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IMP-1 (QA/jubileu): lock Redis protege a corrida real entre dois Create concorrentes ─
    [Test]
    public async Task Should_AcquireLock_And_RevalidateOverlapInsideIt_Before_Persisting()
    {
        var profissionalId = Guid.NewGuid();
        var salaId = Guid.NewGuid();
        SetupHappyPath(profissionalId, salaId);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId, salaId: salaId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);

        _lockService.Verify(l => l.AcquireAsync(It.IsAny<string>(), TimeSpan.FromSeconds(5), It.IsAny<CancellationToken>()), Times.Once);
        // ExisteSobreposicaoAsync só pode rodar DEPOIS do lock adquirido — nesse cenário, uma
        // única chamada (a revalidação dentro da seção crítica; não existe mais checagem antes do lock).
        _agendamentoRepository.Verify(r => r.ExisteSobreposicaoAsync(profissionalId, ValidInicio, ValidFim, null, It.IsAny<CancellationToken>()), Times.Once);
        _lockService.Verify(l => l.ReleaseAsync(It.IsAny<string>(), LockToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_And_NeverPersist_When_LockCannotBeAcquired()
    {
        var profissionalId = Guid.NewGuid();
        var salaId = Guid.NewGuid();
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value);
        _salaRepository.Setup(r => r.GetByIdAsync(salaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sala.Criar(Guid.NewGuid(), "Sala 1").Value);
        _lockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId, salaId: salaId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.LockIndisponivel"));
        _agendamentoRepository.Verify(r => r.AddAsync(It.IsAny<Agendamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Regressão da race: sobreposição só aparece DEPOIS do lock adquirido (corrida real) ───
    [Test]
    public async Task Should_ReturnFailure_And_ReleaseLock_When_OverlapAppearsOnlyAfterLockAcquired()
    {
        var profissionalId = Guid.NewGuid();
        var salaId = Guid.NewGuid();
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _profissionalRepository.Setup(r => r.GetByIdAsync(profissionalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profissional.Criar(Guid.NewGuid(), "Dr. João", "Ortodontia", TipoContrato.Clt).Value);
        _salaRepository.Setup(r => r.GetByIdAsync(salaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sala.Criar(Guid.NewGuid(), "Sala 1").Value);
        // Simula outra requisição que venceu a corrida e já persistiu um agendamento sobreposto
        // enquanto esta esperava o lock.
        _agendamentoRepository.Setup(r => r.ExisteSobreposicaoAsync(profissionalId, ValidInicio, ValidFim, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(ValidCommand(profissionalId: profissionalId, salaId: salaId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.HorarioIndisponivel"));
        _agendamentoRepository.Verify(r => r.AddAsync(It.IsAny<Agendamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _lockService.Verify(l => l.ReleaseAsync(It.IsAny<string>(), LockToken, It.IsAny<CancellationToken>()), Times.Once, "lock precisa ser liberado mesmo em caso de falha");
    }
}
