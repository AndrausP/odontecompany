using SharedKernel;

namespace Patients.Domain.Errors;

/// <summary>Catálogo de erros de negócio do módulo Patients — sempre tipados, nunca exception pra fluxo esperado.</summary>
public static class DomainErrors
{
    public static class Patient
    {
        public static readonly Error OrganizationInvalido = new("Patient.OrganizationInvalido", "Organization inválido.");
        public static readonly Error NomeObrigatorio = new("Patient.NomeObrigatorio", "Nome completo é obrigatório.");
        public static readonly Error TelefoneObrigatorio = new("Patient.TelefoneObrigatorio", "Telefone é obrigatório.");
        public static readonly Error DataNascimentoInvalida = new("Patient.DataNascimentoInvalida", "Data de nascimento inválida.");
        public static readonly Error ConsentimentoLgpdObrigatorio = new(
            "Patient.ConsentimentoLgpdObrigatorio",
            "Não é possível cadastrar um paciente sem o consentimento LGPD explícito.");
        public static readonly Error CpfJaCadastrado = new("Patient.CpfJaCadastrado", "CPF já cadastrado para este organization.");
        public static readonly Error NaoEncontrado = new("Patient.NaoEncontrado", "Paciente não encontrado.");
    }

    public static class CpfErrors
    {
        public static readonly Error Obrigatorio = new("Cpf.Obrigatorio", "CPF é obrigatório.");
        public static readonly Error Invalido = new("Cpf.Invalido", "CPF inválido.");
    }
}
