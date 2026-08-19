using Moq;
using Scheduling.Application.Commands.CreateProfissional;
using Scheduling.Application.Interfaces;
using Scheduling.Domain.Entities;
using Tenancy.Contracts;

namespace Scheduling.UnitTests.Commands;

[TestFixture]
public class CreateProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _profissionalRepository = null!;
    private Mock<IBranchLookup> _branchLookup = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateProfissionalCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _profissionalRepository = new Mock<IProfissionalRepository>();
        _branchLookup = new Mock<IBranchLookup>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateProfissionalCommandHandler(_profissionalRepository.Object, _branchLookup.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateProfissional_When_BranchIdIsNull()
    {
        var command = new CreateProfissionalCommand(Guid.NewGuid(), "Dra. Ana", "Ortodontia", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _branchLookup.Verify(u => u.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnFailure_When_BranchIdIsProvided_But_DoesNotExist()
    {
        _branchLookup.Setup(u => u.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateProfissionalCommand(Guid.NewGuid(), "Dra. Ana", "Ortodontia", null, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Profissional.BranchInvalida"));
        _profissionalRepository.Verify(r => r.AddAsync(It.IsAny<Profissional>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CreateProfissional_When_BranchIdExists()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        _branchLookup.Setup(u => u.ExistsAsync(organizationId, branchId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateProfissionalCommand(organizationId, "Dra. Ana", "Ortodontia", null, branchId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.BranchId, Is.EqualTo(branchId));
    }
}
