using Patients.Domain.Errors;
using Patients.Domain.ValueObjects;
using SharedKernel;

namespace Patients.Domain.Entities;

/// <summary>
/// Paciente da clínica, sempre pertencente a um organization. Dado de paciente nunca é apagado
/// fisicamente — só desativado (<see cref="Desativar"/>). CPF é imutável após a criação (é a
/// chave de identificação do paciente); qualquer correção de CPF errado exige novo cadastro,
/// nunca update — decisão deliberada pra não permitir reescrever a identidade de um paciente
/// já vinculado a histórico clínico/financeiro.
/// </summary>
public class Patient : AggregateRoot, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string NomeCompleto { get; private set; } = string.Empty;
    public Cpf Cpf { get; private set; } = null!;
    public DateTime DataNascimento { get; private set; }
    public string Telefone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Endereco { get; private set; }
    public bool ConsentimentoLgpd { get; private set; }
    public DateTime? DataConsentimentoLgpd { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Patient() { } // EF Core

    private Patient(
        Guid organizationId,
        string nomeCompleto,
        Cpf cpf,
        DateTime dataNascimento,
        string telefone,
        string? email,
        string? endereco,
        bool consentimentoLgpd)
    {
        OrganizationId = organizationId;
        NomeCompleto = nomeCompleto;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        Telefone = telefone;
        Email = email;
        Endereco = endereco;
        ConsentimentoLgpd = consentimentoLgpd;
        DataConsentimentoLgpd = consentimentoLgpd ? DateTime.UtcNow : null;
        Ativo = true;
    }

    /// <summary>
    /// Cria um paciente. <paramref name="consentimentoLgpd"/> precisa ser explicitamente
    /// <c>true</c> — não existe cadastro de paciente sem aceite de LGPD registrado.
    /// </summary>
    public static Result<Patient> Create(
        Guid organizationId,
        string nomeCompleto,
        string cpf,
        DateTime dataNascimento,
        string telefone,
        string? email,
        string? endereco,
        bool consentimentoLgpd)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Patient>(DomainErrors.Patient.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nomeCompleto))
            return Result.Failure<Patient>(DomainErrors.Patient.NomeObrigatorio);

        if (!consentimentoLgpd)
            return Result.Failure<Patient>(DomainErrors.Patient.ConsentimentoLgpdObrigatorio);

        if (dataNascimento == default || dataNascimento.Date > DateTime.UtcNow.Date)
            return Result.Failure<Patient>(DomainErrors.Patient.DataNascimentoInvalida);

        if (string.IsNullOrWhiteSpace(telefone))
            return Result.Failure<Patient>(DomainErrors.Patient.TelefoneObrigatorio);

        var cpfResult = Cpf.Create(cpf);
        if (cpfResult.IsFailure)
            return Result.Failure<Patient>(cpfResult.Error);

        return Result.Success(new Patient(
            organizationId,
            nomeCompleto.Trim(),
            cpfResult.Value,
            dataNascimento,
            telefone.Trim(),
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim(),
            consentimentoLgpd));
    }

    /// <summary>Atualiza dados cadastrais. Nunca toca em CPF — CPF é imutável após a criação.</summary>
    public Result AtualizarDadosCadastrais(string nomeCompleto, DateTime dataNascimento, string telefone, string? email, string? endereco)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            return Result.Failure(DomainErrors.Patient.NomeObrigatorio);

        if (dataNascimento == default || dataNascimento.Date > DateTime.UtcNow.Date)
            return Result.Failure(DomainErrors.Patient.DataNascimentoInvalida);

        if (string.IsNullOrWhiteSpace(telefone))
            return Result.Failure(DomainErrors.Patient.TelefoneObrigatorio);

        NomeCompleto = nomeCompleto.Trim();
        DataNascimento = dataNascimento;
        Telefone = telefone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Endereco = string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim();
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Soft delete — nunca remove a linha do banco. Idempotente: desativar duas vezes não é erro.</summary>
    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
