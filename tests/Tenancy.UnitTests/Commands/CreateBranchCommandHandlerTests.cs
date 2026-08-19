using Moq;
using Tenancy.Application.Commands.CreateBranch;
using Tenancy.Application.Exceptions;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Entities;

namespace Tenancy.UnitTests.Commands;

[TestFixture]
public class CreateBranchCommandHandlerTests
{
    private Mock<IBranchRepository> _branchRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateBranchCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _branchRepository = new Mock<IBranchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateBranchCommandHandler(_branchRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateBranch_When_DataIsValid()
    {
        var result = await _handler.Handle(new CreateBranchCommand(Guid.NewGuid(), "Clínica Centro", null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _branchRepository.Verify(r => r.AddAsync(It.IsAny<Branch>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNomeJaCadastrado_When_UniqueConstraintViolationHappensOnSave()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_Branches_OrganizationId_Nome", new Exception("unique violation")));

        var result = await _handler.Handle(new CreateBranchCommand(Guid.NewGuid(), "Clínica Centro", null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NomeJaCadastrado"));
    }
}
