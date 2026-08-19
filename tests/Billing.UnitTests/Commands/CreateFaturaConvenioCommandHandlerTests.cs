using Billing.Application.Commands.CreateFaturaConvenio;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Moq;
using Patients.Contracts;
using SharedKernel;

namespace Billing.UnitTests.Commands;

[TestFixture]
public class CreateFaturaConvenioCommandHandlerTests
{
    private Mock<IFaturaRepository> _faturaRepository = null!;
    private Mock<IConvenioRepository> _convenioRepository = null!;
    private Mock<IPatientLookup> _patientLookup = null!;
    private Mock<IConvenioAdapter> _convenioAdapter = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CreateFaturaConvenioCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _faturaRepository = new Mock<IFaturaRepository>();
        _convenioRepository = new Mock<IConvenioRepository>();
        _patientLookup = new Mock<IPatientLookup>();
        _convenioAdapter = new Mock<IConvenioAdapter>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateFaturaConvenioCommandHandler(
            _faturaRepository.Object, _convenioRepository.Object, _patientLookup.Object, _convenioAdapter.Object, _unitOfWork.Object);
    }

    private static CreateFaturaConvenioCommand ValidCommand(Guid? convenioId = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), convenioId ?? Guid.NewGuid(), null, null, 500m, 20m);

    [Test]
    public async Task Should_ReturnFailure_When_ConvenioDoesNotExist()
    {
        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _convenioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Convenio?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Fatura.ConvenioInvalido"));
        _convenioAdapter.Verify(a => a.EnviarFaturaAsync(It.IsAny<Fatura>(), It.IsAny<Convenio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_ReturnFailure_When_ConvenioIsInativo()
    {
        var convenio = Convenio.Create(Guid.NewGuid(), "Convênio X", null).Value;
        convenio.Desativar();

        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _convenioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(convenio);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Convenio.Inativo"));
        _convenioAdapter.Verify(a => a.EnviarFaturaAsync(It.IsAny<Fatura>(), It.IsAny<Convenio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_CreateFatura_And_RegisterProtocolo_When_ConvenioAdapterSucceeds()
    {
        var convenio = Convenio.Create(Guid.NewGuid(), "Convênio X", "COD-123").Value;

        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _convenioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(convenio);
        _convenioAdapter.Setup(a => a.EnviarFaturaAsync(It.IsAny<Fatura>(), convenio, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("PROTO-999"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.ProtocoloConvenio, Is.EqualTo("PROTO-999"));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // persiste, depois registra protocolo
    }

    [Test]
    public async Task Should_KeepFaturaPersisted_But_WithoutProtocolo_When_ConvenioAdapterFails()
    {
        var convenio = Convenio.Create(Guid.NewGuid(), "Convênio X", null).Value;

        _patientLookup.Setup(p => p.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _convenioRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(convenio);
        _convenioAdapter.Setup(a => a.EnviarFaturaAsync(It.IsAny<Fatura>(), convenio, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(new Error("Convenio.IntegracaoIndisponivel", "Serviço externo indisponível.")));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        // Falha de INTEGRAÇÃO externa não desfaz a fatura já persistida — continua sucesso do
        // ponto de vista de negócio, só sem protocolo (reenvio fica pendente/manual).
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.ProtocoloConvenio, Is.Null);
        _faturaRepository.Verify(r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // só o primeiro save, sem o segundo (não houve protocolo pra registrar)
    }
}
