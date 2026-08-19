using Billing.Application.Commands.CreateFaturaParticular;
using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Moq;
using Patients.Contracts;

namespace Billing.UnitTests.Commands;

[TestFixture]
public class CreateFaturaParticularCommandHandlerTests
{
    private Mock<IFaturaRepository> _faturaRepository = null!;
    private Mock<IPatientLookup> _patientLookup = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateFaturaParticularCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _faturaRepository = new Mock<IFaturaRepository>();
        _patientLookup = new Mock<IPatientLookup>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateFaturaParticularCommandHandler(_faturaRepository.Object, _patientLookup.Object, _unitOfWork.Object);
    }

    private static CreateFaturaParticularCommand ValidCommand(Guid? pacienteId = null) => new(
        Guid.NewGuid(), pacienteId ?? Guid.NewGuid(), null, null, 300m, 3, FormaPagamento.Cartao, null);

    [Test]
    public async Task Should_ReturnFailure_When_PacienteDoesNotExist()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.PacienteInvalido"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CreateFatura_When_PacienteExistsAndDataIsValid()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Parcelas, Has.Count.EqualTo(3));
        _faturaRepository.Verify(r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Regressão: TOCTOU no índice único parcial de AgendamentoId ───────────────────────────
    [Test]
    public async Task Should_ReturnJaExistePorAgendamento_When_UniqueConstraintViolationHappensOnSave()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Faturas_AgendamentoId", new Exception("unique violation")));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.JaExistePorAgendamento"));
    }
}
