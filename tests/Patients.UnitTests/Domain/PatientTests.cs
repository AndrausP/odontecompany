using Patients.Domain.Entities;

namespace Patients.UnitTests.Domain;

[TestFixture]
public class PatientTests
{
    private const string ValidCpf = "52998224725";
    private static readonly DateTime ValidDataNascimento = new(1990, 5, 20);

    [Test]
    public void Should_CreatePatient_When_AllDataIsValid()
    {
        var organizationId = Guid.NewGuid();

        var result = Patient.Create(organizationId, "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", "maria@email.com", "Rua A, 123", consentimentoLgpd: true);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.NomeCompleto, Is.EqualTo("Maria Silva"));
        Assert.That(result.Value.Cpf.Numero, Is.EqualTo(ValidCpf));
        Assert.That(result.Value.Ativo, Is.True);
        Assert.That(result.Value.ConsentimentoLgpd, Is.True);
        Assert.That(result.Value.DataConsentimentoLgpd, Is.Not.Null);
    }

    [Test]
    public void Should_ReturnFailure_When_ConsentimentoLgpdIsFalse()
    {
        var result = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: false);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.ConsentimentoLgpdObrigatorio"));
    }

    [Test]
    public void Should_ReturnFailure_When_OrganizationIdIsEmpty()
    {
        var result = Patient.Create(Guid.Empty, "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_CpfIsInvalid()
    {
        var result = Patient.Create(Guid.NewGuid(), "Maria Silva", "11111111111", ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Cpf.Invalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_DataNascimentoIsInTheFuture()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);

        var result = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, futureDate, "11999999999", null, null, consentimentoLgpd: true);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Patient.DataNascimentoInvalida"));
    }

    [Test]
    public void Should_DeactivatePatient_When_Desativar_IsCalled()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true).Value;

        patient.Desativar();

        Assert.That(patient.Ativo, Is.False);
        Assert.That(patient.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void Should_StaySoftDeleted_When_Desativar_IsCalledTwice()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true).Value;

        patient.Desativar();
        patient.Desativar(); // idempotente — não deve lançar nem mudar comportamento

        Assert.That(patient.Ativo, Is.False);
    }

    [Test]
    public void Should_UpdateCadastralData_But_KeepCpfUnchanged_When_AtualizarDadosCadastrais_IsCalled()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true).Value;
        var originalCpf = patient.Cpf;

        var result = patient.AtualizarDadosCadastrais("Maria Silva Santos", ValidDataNascimento, "11988888888", "novo@email.com", "Rua B, 456");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(patient.NomeCompleto, Is.EqualTo("Maria Silva Santos"));
        Assert.That(patient.Telefone, Is.EqualTo("11988888888"));
        Assert.That(patient.Email, Is.EqualTo("novo@email.com"));
        Assert.That(patient.Cpf, Is.EqualTo(originalCpf), "CPF é imutável após a criação — Patient não expõe nenhum jeito de alterá-lo");
        Assert.That(patient.UpdatedAt, Is.Not.Null);
    }

    // ── Regra: OrganizationId nunca muda após criação; consentimento LGPD e sua data de aceite
    //          também são imutáveis via update de dados cadastrais. Salvaguarda pra que uma
    //          futura alteração em AtualizarDadosCadastrais (novo overload/parâmetro) não
    //          silenciosamente revogue o aceite de LGPD ou realoque o paciente pra outro organization.
    [Test]
    public void Should_KeepOrganizationIdAndLgpdConsent_When_AtualizarDadosCadastrais_IsCalled()
    {
        var organizationId = Guid.NewGuid();
        var patient = Patient.Create(organizationId, "Maria Silva", ValidCpf, ValidDataNascimento, "11999999999", null, null, consentimentoLgpd: true).Value;
        var originalConsentimento = patient.ConsentimentoLgpd;
        var originalDataConsentimento = patient.DataConsentimentoLgpd;
        var originalOrganizationId = patient.OrganizationId;

        var result = patient.AtualizarDadosCadastrais("Outro Nome", ValidDataNascimento, "11977777777", null, null);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(patient.OrganizationId, Is.EqualTo(originalOrganizationId), "OrganizationId nunca muda após criação — paciente não é realocado entre clínicas");
        Assert.That(patient.ConsentimentoLgpd, Is.EqualTo(originalConsentimento), "consentimento LGPD é imutável via update cadastral (evita revogação silenciosa)");
        Assert.That(patient.DataConsentimentoLgpd, Is.EqualTo(originalDataConsentimento), "data do aceite LGPD é rastreável e não pode ser sobrescrita por um update de dados cadastrais");
    }
}
