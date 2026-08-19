using Patients.Domain.ValueObjects;

namespace Patients.UnitTests.Domain;

[TestFixture]
public class CpfTests
{
    // CPF de teste com dígito verificador matematicamente válido (mod 11) — amplamente usado
    // como exemplo de CPF válido em geradores de dados de teste (não pertence a pessoa real).
    private const string ValidCpf = "52998224725";

    [Test]
    public void Should_CreateCpf_When_DigitsAreValid()
    {
        var result = Cpf.Create(ValidCpf);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Numero, Is.EqualTo(ValidCpf));
    }

    [Test]
    public void Should_CreateCpf_When_InputHasPunctuation()
    {
        var result = Cpf.Create("529.982.247-25");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Numero, Is.EqualTo(ValidCpf));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Should_ReturnFailure_When_CpfIsEmpty(string? cpf)
    {
        var result = Cpf.Create(cpf);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Cpf.Obrigatorio"));
    }

    [TestCase("1234567890")] // 10 dígitos
    [TestCase("123456789012")] // 12 dígitos
    public void Should_ReturnFailure_When_LengthIsWrong(string cpf)
    {
        var result = Cpf.Create(cpf);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Cpf.Invalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_CheckDigitIsWrong()
    {
        var result = Cpf.Create("52998224700"); // últimos 2 dígitos adulterados

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Cpf.Invalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_AllDigitsAreTheSame()
    {
        var result = Cpf.Create("11111111111"); // passa no cálculo do DV, mas nunca é um CPF real

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Cpf.Invalido"));
    }

    [Test]
    public void Should_BeEqual_When_NumeroIsTheSame()
    {
        var a = Cpf.Create(ValidCpf).Value;
        var b = Cpf.Create("529.982.247-25").Value;

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a == b, Is.True);
    }
}
