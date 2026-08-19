using Billing.Application.Commands.CreateFaturaFromConsultaConcluida;
using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Moq;

namespace Billing.UnitTests.Commands;

[TestFixture]
public class CreateFaturaFromConsultaConcluidaCommandHandlerTests
{
    private Mock<IFaturaRepository> _faturaRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateFaturaFromConsultaConcluidaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _faturaRepository = new Mock<IFaturaRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateFaturaFromConsultaConcluidaCommandHandler(_faturaRepository.Object, _unitOfWork.Object);
    }

    private static CreateFaturaFromConsultaConcluidaCommand ValidCommand(Guid? agendamentoId = null) => new(
        agendamentoId ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 250m);

    [Test]
    public async Task Should_CreateFatura_When_AgendamentoNotYetFaturado()
    {
        _faturaRepository.Setup(r => r.ExistsByAgendamentoIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _faturaRepository.Verify(r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Idempotência: reentrega de fila (at-least-once) não pode duplicar fatura ─────────────
    [Test]
    public async Task Should_ReturnSuccessWithoutCreating_When_AgendamentoAlreadyFaturado()
    {
        var agendamentoId = Guid.NewGuid();
        _faturaRepository.Setup(r => r.ExistsByAgendamentoIdAsync(agendamentoId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(ValidCommand(agendamentoId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(agendamentoId));
        _faturaRepository.Verify(r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Idempotência sob corrida: duas entregas concorrentes da mesma mensagem ───────────────
    [Test]
    public async Task Should_ReturnSuccess_When_UniqueConstraintViolationHappensOnSave_DueToConcurrentDelivery()
    {
        _faturaRepository.Setup(r => r.ExistsByAgendamentoIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Faturas_AgendamentoId", new Exception("unique violation")));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
