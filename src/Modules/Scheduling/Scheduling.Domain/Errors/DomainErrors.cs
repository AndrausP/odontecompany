using SharedKernel;

namespace Scheduling.Domain.Errors;

/// <summary>Catálogo de erros de negócio do módulo Scheduling — sempre tipados, nunca exception pra fluxo esperado.</summary>
public static class DomainErrors
{
    public static class PeriodoHorarioErrors
    {
        public static readonly Error InicioDeveSerAntesDoFim = new(
            "PeriodoHorario.InicioDeveSerAntesDoFim", "O horário de início deve ser anterior ao horário de fim.");

        public static readonly Error DuracaoMinimaNaoAtingida = new(
            "PeriodoHorario.DuracaoMinimaNaoAtingida", "O período precisa ter no mínimo 15 minutos.");
    }

    public static class Agendamento
    {
        public static readonly Error OrganizationInvalido = new("Agendamento.OrganizationInvalido", "Organization inválido.");
        public static readonly Error PacienteInvalido = new("Agendamento.PacienteInvalido", "Paciente inválido.");
        public static readonly Error ProfissionalInvalido = new("Agendamento.ProfissionalInvalido", "Profissional inválido.");
        public static readonly Error SalaInvalida = new("Agendamento.SalaInvalida", "Sala inválida.");
        public static readonly Error NaoEncontrado = new("Agendamento.NaoEncontrado", "Agendamento não encontrado.");
        public static readonly Error PacienteNaoEncontrado = new("Agendamento.PacienteNaoEncontrado", "Paciente não encontrado.");
        public static readonly Error ProfissionalNaoEncontrado = new("Agendamento.ProfissionalNaoEncontrado", "Profissional não encontrado ou inativo.");
        public static readonly Error SalaNaoEncontrada = new("Agendamento.SalaNaoEncontrada", "Sala não encontrada ou inativa.");
        public static readonly Error HorarioIndisponivel = new(
            "Agendamento.HorarioIndisponivel", "Já existe um agendamento para este profissional neste horário.");
        public static readonly Error SoConfirmaSeAgendado = new(
            "Agendamento.SoConfirmaSeAgendado", "Só é possível confirmar um agendamento com status Agendado.");
        public static readonly Error SoCancelaSeAgendadoOuConfirmado = new(
            "Agendamento.SoCancelaSeAgendadoOuConfirmado", "Só é possível cancelar um agendamento Agendado ou Confirmado.");
        public static readonly Error SoConcluiSeConfirmado = new(
            "Agendamento.SoConcluiSeConfirmado", "Só é possível concluir um agendamento Confirmado.");
        public static readonly Error ValorInvalido = new("Agendamento.ValorInvalido", "Valor da consulta deve ser maior que zero.");
        public static readonly Error ConflitoDeConcorrencia = new(
            "Agendamento.ConflitoDeConcorrencia", "Este agendamento foi alterado por outra requisição — recarregue e tente novamente.");
        public static readonly Error LockIndisponivel = new(
            "Agendamento.LockIndisponivel", "Não foi possível obter o lock de confirmação deste horário — tente novamente em instantes.");
        public static readonly Error StatusInvalido = new("Agendamento.StatusInvalido", "Status informado é inválido.");
        public static readonly Error SemPermissaoAgendaAlheia = new(
            "Agendamento.SemPermissaoAgendaAlheia", "Dentista não pode gerenciar agendamento da agenda de outro profissional.");
    }

    public static class Profissional
    {
        public static readonly Error NomeObrigatorio = new("Profissional.NomeObrigatorio", "Nome é obrigatório.");
        public static readonly Error EspecialidadeObrigatoria = new("Profissional.EspecialidadeObrigatoria", "Especialidade é obrigatória.");
        public static readonly Error NaoEncontrado = new("Profissional.NaoEncontrado", "Profissional não encontrado.");
        public static readonly Error BranchInvalida = new("Profissional.BranchInvalida", "Branch inválida ou inativa.");
    }

    public static class Sala
    {
        public static readonly Error NomeObrigatorio = new("Sala.NomeObrigatorio", "Nome é obrigatório.");
        public static readonly Error CapacidadeInvalida = new("Sala.CapacidadeInvalida", "Capacidade máxima deve ser maior que zero.");
        public static readonly Error NaoEncontrada = new("Sala.NaoEncontrada", "Sala não encontrada.");
        public static readonly Error BranchInvalida = new("Sala.BranchInvalida", "Branch inválida ou inativa.");
    }
}
