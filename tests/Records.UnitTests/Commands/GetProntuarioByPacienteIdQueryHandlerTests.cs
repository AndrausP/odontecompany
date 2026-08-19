using Moq;
using Records.Application.Interfaces;
using Records.Application.Queries.GetProntuarioByPacienteId;
using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Commands;

[TestFixture]
public class GetProntuarioByPacienteIdQueryHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepository = null!;
    private Mock<IAuditLogWriter> _auditLogWriter = null!;
    private GetProntuarioByPacienteIdQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _prontuarioRepository = new Mock<IProntuarioRepository>();
        _auditLogWriter = new Mock<IAuditLogWriter>();
        _handler = new GetProntuarioByPacienteIdQueryHandler(_prontuarioRepository.Object, _auditLogWriter.Object);
    }

    [Test]
    public async Task Should_ReturnFailure_And_NeverLog_When_ProntuarioNotFound()
    {
        _prontuarioRepository.Setup(r => r.GetByPacienteIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Prontuario?)null);

        var result = await _handler.Handle(new GetProntuarioByPacienteIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.NaoEncontrado"));
        _auditLogWriter.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AcaoAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnProntuario_And_LogLeitura_When_ProntuarioExists()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        var requestingUserId = Guid.NewGuid();

        _prontuarioRepository.Setup(r => r.GetByPacienteIdAsync(prontuario.PacienteId, It.IsAny<CancellationToken>())).ReturnsAsync(prontuario);
        _prontuarioRepository.Setup(r => r.ListEvolucoesAsync(prontuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvolucaoClinica>());
        _prontuarioRepository.Setup(r => r.ListAnexosAsync(prontuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AnexoMetadata>());

        var result = await _handler.Handle(new GetProntuarioByPacienteIdQuery(prontuario.PacienteId, requestingUserId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.PacienteId, Is.EqualTo(prontuario.PacienteId));

        // Toda leitura de prontuário é auditada — não é opcional (LGPD).
        _auditLogWriter.Verify(a => a.LogAsync(
            prontuario.OrganizationId, prontuario.Id, prontuario.PacienteId, requestingUserId, AcaoAuditoria.Leitura, It.IsAny<CancellationToken>()), Times.Once);
    }
}
