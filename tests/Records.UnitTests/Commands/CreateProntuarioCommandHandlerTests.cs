using Moq;
using Patients.Contracts;
using Records.Application.Commands.CreateProntuario;
using Records.Application.Exceptions;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Commands;

[TestFixture]
public class CreateProntuarioCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepository = null!;
    private Mock<IPatientLookup> _patientLookup = null!;
    private Mock<IAuditLogWriter> _auditLogWriter = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateProntuarioCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _prontuarioRepository = new Mock<IProntuarioRepository>();
        _patientLookup = new Mock<IPatientLookup>();
        _auditLogWriter = new Mock<IAuditLogWriter>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateProntuarioCommandHandler(
            _prontuarioRepository.Object, _patientLookup.Object, _auditLogWriter.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_ReturnFailure_When_PacienteDoesNotExist()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(new CreateProntuarioCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.PacienteNaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProntuarioAlreadyExistsForPaciente()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _prontuarioRepository.Setup(r => r.ExistsByPacienteIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(new CreateProntuarioCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.JaExistePorPaciente"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CreateProntuario_And_LogCriacao_When_PacienteExistsAndHasNoProntuarioYet()
    {
        var organizationId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _patientLookup.Setup(p => p.ExistsAsync(pacienteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _prontuarioRepository.Setup(r => r.ExistsByPacienteIdAsync(pacienteId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(new CreateProntuarioCommand(organizationId, pacienteId, userId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.PacienteId, Is.EqualTo(pacienteId));

        _prontuarioRepository.Verify(r => r.AddAsync(It.Is<Prontuario>(p => p.PacienteId == pacienteId && p.OrganizationId == organizationId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogWriter.Verify(a => a.LogAsync(organizationId, It.IsAny<Guid>(), pacienteId, userId, AcaoAuditoria.Criacao, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Regressão: TOCTOU entre ExistsByPacienteIdAsync e SaveChangesAsync ───────────────────
    [Test]
    public async Task Should_ReturnJaExistePorPaciente_When_UniqueConstraintViolationHappensOnSave()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _prontuarioRepository.Setup(r => r.ExistsByPacienteIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Prontuarios_OrganizationId_PacienteId", new Exception("unique violation")));

        var result = await _handler.Handle(new CreateProntuarioCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.JaExistePorPaciente"));
        _auditLogWriter.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AcaoAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
