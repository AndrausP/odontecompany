using Moq;
using Tenancy.Application.Commands.UpdateBranch;
using Tenancy.Application.Interfaces;
using Tenancy.Domain.Entities;

namespace Tenancy.UnitTests.Commands;

[TestFixture]
public class UpdateBranchCommandHandlerTests
{
    private Mock<IBranchRepository> _branchRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UpdateBranchCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _branchRepository = new Mock<IBranchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UpdateBranchCommandHandler(_branchRepository.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_UpdateFields_When_BranchExistsAndBelongsToOrganization()
    {
        var organizationId = Guid.NewGuid();
        var branch = Branch.Create(organizationId, "Unidade Antiga", null).Value;
        _branchRepository.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await _handler.Handle(
            new UpdateBranchCommand(branch.Id, organizationId, "Unidade Nova", "Rua Nova, 1", "11999999999"), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Nome, Is.EqualTo("Unidade Nova"));
        Assert.That(result.Value.Telefone, Is.EqualTo("11999999999"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNaoEncontrada_When_BranchDoesNotExist()
    {
        _branchRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Branch?)null);

        var result = await _handler.Handle(new UpdateBranchCommand(Guid.NewGuid(), Guid.NewGuid(), "Unidade Nova", null, null), CancellationToken.None);

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

        var result = await _handler.Handle(
            new UpdateBranchCommand(branch.Id, Guid.NewGuid(), "Unidade Nova", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NaoEncontrada"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnNomeObrigatorio_When_NomeIsBlank()
    {
        var organizationId = Guid.NewGuid();
        var branch = Branch.Create(organizationId, "Unidade Antiga", null).Value;
        _branchRepository.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await _handler.Handle(new UpdateBranchCommand(branch.Id, organizationId, " ", null, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Branch.NomeObrigatorio"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
