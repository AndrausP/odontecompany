using Moq;
using Scheduling.Application.Commands.CreateProcedimento;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class CreateProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoRepository> _procedimentoRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateProcedimentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _procedimentoRepository = new Mock<IProcedimentoRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateProcedimentoCommandHandler(_procedimentoRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateProcedimento_When_DataIsValid()
    {
        var result = await _handler.Handle(new CreateProcedimentoCommand(Guid.NewGuid(), "Canal", 800m, 90), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Canal"));
        _procedimentoRepository.Verify(r => r.AddAsync(It.IsAny<Procedimento>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_NomeIsBlank()
    {
        var result = await _handler.Handle(new CreateProcedimentoCommand(Guid.NewGuid(), " ", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Procedimento.NomeObrigatorio"));
        _procedimentoRepository.Verify(r => r.AddAsync(It.IsAny<Procedimento>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
