using Moq;
using Records.Application.Commands.AddEvolucaoClinica;
using Records.Application.Interfaces;
using Records.Domain.Entities;
using Records.Domain.Enums;

namespace Records.UnitTests.Commands;

[TestFixture]
public class AddEvolucaoClinicaCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepository = null!;
    private Mock<IAuditLogWriter> _auditLogWriter = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private AddEvolucaoClinicaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _prontuarioRepository = new Mock<IProntuarioRepository>();
        _auditLogWriter = new Mock<IAuditLogWriter>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new AddEvolucaoClinicaCommandHandler(_prontuarioRepository.Object, _auditLogWriter.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProntuarioNotFound()
    {
        _prontuarioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Prontuario?)null);

        var command = new AddEvolucaoClinicaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoProcedimento.Consulta, "Avaliação.");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.NaoEncontrado"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ProntuarioIsInativo()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        prontuario.Desativar();

        _prontuarioRepository.Setup(r => r.GetByIdAsync(prontuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prontuario);

        var command = new AddEvolucaoClinicaCommand(prontuario.OrganizationId, prontuario.Id, Guid.NewGuid(), TipoProcedimento.Consulta, "Avaliação.");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Prontuario.Inativo"));
    }

    [Test]
    public async Task Should_AddEvolucao_And_LogAdicaoEvolucao_When_ProntuarioIsAtivo()
    {
        var prontuario = Prontuario.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        var profissionalId = Guid.NewGuid();

        _prontuarioRepository.Setup(r => r.GetByIdAsync(prontuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prontuario);

        var command = new AddEvolucaoClinicaCommand(
            prontuario.OrganizationId, prontuario.Id, profissionalId, TipoProcedimento.Canal, "Canal iniciado no dente 36.");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.DescricaoClinica, Is.EqualTo("Canal iniciado no dente 36."));

        _prontuarioRepository.Verify(r => r.AddEvolucaoAsync(
            It.Is<EvolucaoClinica>(e => e.ProntuarioId == prontuario.Id && e.ProfissionalUserId == profissionalId),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogWriter.Verify(a => a.LogAsync(
            prontuario.OrganizationId, prontuario.Id, prontuario.PacienteId, profissionalId, AcaoAuditoria.AdicaoEvolucao, It.IsAny<CancellationToken>()), Times.Once);
    }
}
