using SharedKernel;

namespace Billing.Domain.Errors;

public static class DomainErrors
{
    public static class Fatura
    {
        public static readonly Error OrganizationInvalido = new("Fatura.OrganizationInvalido", "Organization inválido.");
        public static readonly Error PacienteInvalido = new("Fatura.PacienteInvalido", "Paciente inválido.");
        public static readonly Error ValorInvalido = new("Fatura.ValorInvalido", "Valor total deve ser maior que zero.");
        public static readonly Error NumeroParcelasInvalido = new("Fatura.NumeroParcelasInvalido", "Número de parcelas deve ser entre 1 e 12.");
        public static readonly Error NaoEncontrada = new("Fatura.NaoEncontrada", "Fatura não encontrada.");
        public static readonly Error JaCancelada = new("Fatura.JaCancelada", "Fatura já está cancelada.");
        public static readonly Error NaoPodeCancelarFaturaPaga = new("Fatura.NaoPodeCancelarFaturaPaga", "Não é possível cancelar uma fatura já totalmente paga.");
        public static readonly Error ConvenioInvalido = new("Fatura.ConvenioInvalido", "Convênio inválido ou inativo.");
        public static readonly Error ConvenioObrigatorioParaFaturaDeConvenio = new("Fatura.ConvenioObrigatorioParaFaturaDeConvenio", "Fatura de convênio exige um convênio válido.");
        public static readonly Error JaExistePorAgendamento = new("Fatura.JaExistePorAgendamento", "Já existe fatura gerada para este agendamento.");
    }

    public static class Parcela
    {
        public static readonly Error NaoEncontrada = new("Parcela.NaoEncontrada", "Parcela não encontrada.");
        public static readonly Error JaPaga = new("Parcela.JaPaga", "Parcela já está paga.");
        public static readonly Error NaoPertenceAFatura = new("Parcela.NaoPertenceAFatura", "Parcela não pertence a esta fatura.");
    }

    public static class Convenio
    {
        public static readonly Error NomeObrigatorio = new("Convenio.NomeObrigatorio", "Nome do convênio é obrigatório.");
        public static readonly Error NaoEncontrado = new("Convenio.NaoEncontrado", "Convênio não encontrado.");
        public static readonly Error Inativo = new("Convenio.Inativo", "Convênio está inativo.");
    }
}
