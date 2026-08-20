using Moq;
using Scheduling.Application.Commands.UpdateProcedimento;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class UpdateProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoRepository> _procedimentoRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateProcedimentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _procedimentoRepository = new Mock<IProcedimentoRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateProcedimentoCommandHandler(_procedimentoRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_UpdateFields_When_ProcedimentoExists()
    {
        var organizationId = Guid.NewGuid();
        var procedimento = Procedimento.Criar(organizationId, "Limpeza", 150m, 30).Value;
        _procedimentoRepository.Setup(r => r.GetByIdAsync(procedimento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(procedimento);

        var result = await _handler.Handle(new UpdateProcedimentoCommand(procedimento.Id, organizationId, "Limpeza Completa", 200m, 40), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Limpeza Completa"));
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_ProcedimentoBelongsToDifferentOrganization()
    {
        var procedimento = Procedimento.Criar(Guid.NewGuid(), "Limpeza").Value;
        _procedimentoRepository.Setup(r => r.GetByIdAsync(procedimento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(procedimento);

        var result = await _handler.Handle(new UpdateProcedimentoCommand(procedimento.Id, Guid.NewGuid(), "Limpeza", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Procedimento.NaoEncontrado"));
    }
}
