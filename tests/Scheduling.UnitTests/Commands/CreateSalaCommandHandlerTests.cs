using Moq;
using Scheduling.Application.Commands.CreateSala;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Tenancy.Contracts;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class CreateSalaCommandHandlerTests
{
    private Mock<ISalaRepository> _salaRepository = null!;
    private Mock<IBranchLookup> _branchLookup = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateSalaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _salaRepository = new Mock<ISalaRepository>();
        _branchLookup = new Mock<IBranchLookup>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateSalaCommandHandler(_salaRepository.Object, _branchLookup.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateSala_When_BranchIdIsNull()
    {
        var result = await _handler.Handle(new CreateSalaCommand(Guid.NewGuid(), "Sala 1", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _salaRepository.Verify(r => r.AddAsync(It.IsAny<Sala>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_BranchIdIsProvided_But_DoesNotExist()
    {
        _branchLookup.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(new CreateSalaCommand(Guid.NewGuid(), "Sala 1", null, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Sala.BranchInvalida"));
    }

    [Test]
    public async Task Should_ReturnFailure_When_CapacidadeMaximaIsZeroOrNegative()
    {
        // Domínio (Sala.Criar) valida — o handler não filtra isso antes, então precisa devolver o erro do domínio corretamente.
        var result = await _handler.Handle(new CreateSalaCommand(Guid.NewGuid(), "Sala 1", 0, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Sala.CapacidadeInvalida"));
    }
}
