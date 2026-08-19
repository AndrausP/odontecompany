/** Espelha Scheduling.Domain.Enums.AgendamentoStatus. */
export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Cancelado' | 'Concluido'

/** Espelha Scheduling.Contracts.AgendamentoDto. */
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
}

/** Espelha Scheduling.Contracts.ProfissionalDto. */
export interface Profissional {
  id: string
  nome: string
  especialidade: string
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
}
