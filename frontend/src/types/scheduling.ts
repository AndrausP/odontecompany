/** Espelha Scheduling.Domain.Enums.AgendamentoStatus. */
export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Cancelado' | 'Concluido'

/** Espelha Scheduling.Contracts.AgendamentoDto (+ProcedimentoId, task 044). */
export interface Agendamento {
  id: string
  pacienteId: string
  profissionalId: string
  salaId: string
  inicio: string
  fim: string
  status: AgendamentoStatus
  motivoCancelamento: string | null
  valorConsulta: number | null
  createdAt: string
  updatedAt: string | null
  procedimentoId: string | null
}

/** Espelha Scheduling.Domain.Enums.TipoContrato. */
export type TipoContrato = 'Clt' | 'Pj' | 'Autonomo'

/** Espelha Scheduling.Contracts.ProfissionalDto (+TipoContrato/PercentualComissaoDefault/Email, task 042). */
export interface Profissional {
  id: string
  nome: string
  especialidade: string
  tipoContrato: TipoContrato
  percentualComissaoDefault: number | null
  email: string | null
  userId: string | null
  branchId: string | null
  ativo: boolean
}

/** Espelha Scheduling.Contracts.SalaDto. */
export interface Sala {
  id: string
  nome: string
  capacidadeMaxima: number | null
  branchId: string | null
  ativa: boolean
}

/** Corpo de POST /api/agendamentos. */
export interface CreateAgendamentoRequest {
  pacienteId: string
  profissionalId: string
  salaId: string
  inicio: string
  fim: string
  procedimentoId?: string
}

/** Espelha Scheduling.Contracts.ProcedimentoDto — catálogo de procedimentos/serviços (task 044). */
export interface Procedimento {
  id: string
  nome: string
  valorPadrao: number | null
  duracaoPadraoMinutos: number | null
  ativo: boolean
}
