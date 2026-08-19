using SharedKernel;

namespace Records.Domain.Errors;

/// <summary>Catálogo de erros de negócio do módulo Records — sempre tipados, nunca exception pra fluxo esperado.</summary>
public static class DomainErrors
{
    public static class Prontuario
    {
        public static readonly Error OrganizationInvalido = new("Prontuario.OrganizationInvalido", "Organization inválido.");
        public static readonly Error PacienteInvalido = new("Prontuario.PacienteInvalido", "Paciente inválido.");
        public static readonly Error PacienteNaoEncontrado = new("Prontuario.PacienteNaoEncontrado", "Paciente não encontrado ou inativo.");
        public static readonly Error NaoEncontrado = new("Prontuario.NaoEncontrado", "Prontuário não encontrado.");
        public static readonly Error JaExistePorPaciente = new("Prontuario.JaExistePorPaciente", "Já existe prontuário para este paciente.");
        public static readonly Error Inativo = new("Prontuario.Inativo", "Prontuário está inativo.");
    }

    public static class Evolucao
    {
        public static readonly Error DescricaoObrigatoria = new("Evolucao.DescricaoObrigatoria", "Descrição clínica é obrigatória.");
        public static readonly Error ProfissionalInvalido = new("Evolucao.ProfissionalInvalido", "Profissional inválido.");
    }

    public static class Anexo
    {
        public static readonly Error ArquivoVazio = new("Anexo.ArquivoVazio", "Arquivo vazio ou não enviado.");
        public static readonly Error ArquivoMuitoGrande = new("Anexo.ArquivoMuitoGrande", "Arquivo excede o tamanho máximo permitido (20MB).");
        public static readonly Error TipoNaoPermitido = new("Anexo.TipoNaoPermitido", "Tipo de arquivo não permitido.");
    }
}
