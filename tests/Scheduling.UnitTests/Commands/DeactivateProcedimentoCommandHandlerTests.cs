using Moq;
using Scheduling.Application.Commands.DeactivateProcedimento;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class DeactivateProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoRepository> _procedimentoRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeactivateProcedimentoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _procedimentoRepository = new Mock<IProcedimentoRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeactivateProcedimentoCommandHandler(_procedimentoRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_Deactivate_When_ProcedimentoExists()
    {
        var organizationId = Guid.NewGuid();
        var procedimento = Procedimento.Criar(organizationId, "Limpeza").Value;
        _procedimentoRepository.Setup(r => r.GetByIdAsync(procedimento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(procedimento);

        var result = await _handler.Handle(new DeactivateProcedimentoCommand(procedimento.Id, organizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(procedimento.Ativo, Is.False);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrado_When_ProcedimentoBelongsToDifferentOrganization()
    {
        var procedimento = Procedimento.Criar(Guid.NewGuid(), "Limpeza").Value;
        _procedimentoRepository.Setup(r => r.GetByIdAsync(procedimento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(procedimento);

        var result = await _handler.Handle(new DeactivateProcedimentoCommand(procedimento.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Procedimento.NaoEncontrado"));
        Assert.That(procedimento.Ativo, Is.True);
    }
}
