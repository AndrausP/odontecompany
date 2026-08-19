using Records.Domain.Entities;

namespace Records.UnitTests.Domain;

[TestFixture]
public class AnexoMetadataTests
{
    [Test]
    public void Should_CreateAnexo_When_SizeIsWithinLimit()
    {
        var result = AnexoMetadata.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "raiox.png", "image/png", 1024, "organization/prontuario/raiox.png");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.TamanhoBytes, Is.EqualTo(1024));
    }

    [Test]
    public void Should_ReturnFailure_When_TamanhoBytesIsZeroOrNegative()
    {
        var result = AnexoMetadata.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vazio.png", "image/png", 0, "path");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Anexo.ArquivoVazio"));
    }

    [Test]
    public void Should_ReturnFailure_When_TamanhoBytesExceedsMaximum()
    {
        var result = AnexoMetadata.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "gigante.pdf", "application/pdf",
            AnexoMetadata.TamanhoMaximoBytes + 1, "path");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Anexo.ArquivoMuitoGrande"));
    }

    [Test]
    public void Should_CreateAnexo_When_TamanhoBytesEqualsMaximum()
    {
        var result = AnexoMetadata.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "limite.pdf", "application/pdf",
            AnexoMetadata.TamanhoMaximoBytes, "path");

        Assert.That(result.IsSuccess, Is.True);
    }
}
