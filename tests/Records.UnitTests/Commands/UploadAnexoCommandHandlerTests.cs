using Moq;
using Records.Application.Commands.UploadAnexo;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Commands;

[TestFixture]
public class UploadAnexoCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepository = null!;
    private Mock<IObjectStorageService> _objectStorageService = null!;
    private Mock<IAuditLogWriter> _auditLogWriter = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private UploadAnexoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _prontuarioRepository = new Mock<IProntuarioRepository>();
        _objectStorageService = new Mock<IObjectStorageService>();
        _auditLogWriter = new Mock<IAuditLogWriter>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new UploadAnexoCommandHandler(
            _prontuarioRepository.Object, _objectStorageService.Object, _auditLogWriter.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProntuarioNotFound()
    {
        _prontuarioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Prontuario?)null);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadAnexoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "raiox.png", "image/png", 3, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.NaoEncontrado"));
        _objectStorageService.Verify(s => s.SaveAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_SaveAnexo_And_LogUploadAnexo_When_ProntuarioExists()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        var uploadedBy = Guid.NewGuid();

        _prontuarioRepository.Setup(r => r.GetByIdAsync(prontuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prontuario);
        _objectStorageService.Setup(s => s.SaveAsync(
                prontuario.OrganizationId, prontuario.Id, "raiox.png", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{prontuario.OrganizationId}/{prontuario.Id}/abc_raiox.png");

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var command = new UploadAnexoCommand(prontuario.OrganizationId, prontuario.Id, uploadedBy, "raiox.png", "image/png", 4, stream);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.NomeArquivo, Is.EqualTo("raiox.png"));

        _prontuarioRepository.Verify(r => r.AddAnexoAsync(
            It.Is<AnexoMetadata>(a => a.ProntuarioId == prontuario.Id && a.UploadedByUserId == uploadedBy),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogWriter.Verify(a => a.LogAsync(
            prontuario.OrganizationId, prontuario.Id, prontuario.PacienteId, uploadedBy, AcaoAuditoria.UploadAnexo, It.IsAny<CancellationToken>()), Times.Once);
    }
}
