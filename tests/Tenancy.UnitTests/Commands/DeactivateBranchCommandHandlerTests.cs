using Moq;
using Tenancy.Application.Commands.DeactivateBranch;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Entities;

namespace Tenancy.UnitTests.Commands;

[TestFixture]
public class DeactivateBranchCommandHandlerTests
{
    private Mock<IBranchRepository> _branchRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private DeactivateBranchCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _branchRepository = new Mock<IBranchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeactivateBranchCommandHandler(_branchRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_DeactivateBranch_When_BranchExistsAndBelongsToOrganization()
    {
        var organizationId = Guid.NewGuid();
        var branch = Branch.Create(organizationId, "Unidade Centro", null).Value;
        _branchRepository.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await _handler.Handle(new DeactivateBranchCommand(branch.Id, organizationId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(branch.Ativo, Is.False);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrada_When_BranchDoesNotExist()
    {
        _branchRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Branch?)null);

        var result = await _handler.Handle(new DeactivateBranchCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NaoEncontrada"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Defesa contra IDOR (task 040) — branch existe, mas é de OUTRA organization. Mesmo
    /// erro de "não encontrada" de propósito, pra não confirmar existência do guid alheio.</summary>
    [Test]
    public async Task Should_ReturnNaoEncontrada_When_BranchBelongsToDifferentOrganization()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Unidade De Outra Org", null).Value;
        _branchRepository.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await _handler.Handle(new DeactivateBranchCommand(branch.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NaoEncontrada"));
        Assert.That(branch.Ativo, Is.True); // não desativou por engano
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
