import { apiClient } from '../../lib/api-client'
import type { PagedResult } from '../../types/common'
import type { Agendamento, CreateAgendamentoRequest, Procedimento, Profissional, Sala, TipoContrato } from '../../types/scheduling'

export interface ListAgendamentosParams {
  dataInicio?: string
  dataFim?: string
  profissionalId?: string
  status?: string
  page?: number
  pageSize?: number
}

export async function listAgendamentos(params: ListAgendamentosParams): Promise<PagedResult<Agendamento>> {
  const { data } = await apiClient.get<PagedResult<Agendamento>>('/api/agendamentos', {
    params: { pageSize: 200, ...params },
  })
  return data
}

export async function createAgendamento(payload: CreateAgendamentoRequest): Promise<Agendamento> {
  const { data } = await apiClient.post<Agendamento>('/api/agendamentos', payload)
  return data
}

export async function confirmarAgendamento(id: string): Promise<Agendamento> {
  const { data } = await apiClient.post<Agendamento>(`/api/agendamentos/${id}/confirmar`)
  return data
}

export async function cancelarAgendamento(id: string, motivo?: string): Promise<Agendamento> {
  const { data } = await apiClient.post<Agendamento>(`/api/agendamentos/${id}/cancelar`, { motivo })
  return data
}

export async function concluirAgendamento(id: string, valor: number): Promise<Agendamento> {
  const { data } = await apiClient.post<Agendamento>(`/api/agendamentos/${id}/concluir`, { valor })
  return data
}

export async function listProfissionais(): Promise<Profissional[]> {
  const { data } = await apiClient.get<Profissional[]>('/api/profissionais')
  return data
}

export async function listSalas(): Promise<Sala[]> {
  const { data } = await apiClient.get<Sala[]>('/api/salas')
  return data
}

export interface CreateProfissionalFields {
  nome: string
  especialidade: string
  tipoContrato: TipoContrato
  percentualComissaoDefault?: number
  /** Opcional (task 042) — se informado, o backend dispara um convite Role.Dentista pra esse email. */
  email?: string
  branchId?: string
}

/** Envelope de resposta do Create (task 042) — o profissional existe mesmo quando o convite falha. */
export interface CreateProfissionalResult {
  profissional: Profissional
  conviteEnviado: boolean
  conviteAviso: string | null
}

/**
 * POST /api/profissionais (Owner/Admin) — tela de Configurações (task 041/auditoria pré-venda):
 * até aqui o endpoint existia sem NENHUMA tela pra chamar, então uma organização nova não
 * conseguia popular o select de "Novo agendamento" (Agenda inutilizável sem isso). Task 042:
 * `email` opcional — quando informado, o backend também dispara um convite Role.Dentista, pra o
 * profissional poder criar o próprio login; sem email, continua sendo só um recurso da Agenda,
 * sem acesso nenhum (comportamento de sempre).
 */
export async function createProfissional(fields: CreateProfissionalFields): Promise<CreateProfissionalResult> {
  const { data } = await apiClient.post<CreateProfissionalResult>('/api/profissionais', {
    Nome: fields.nome,
    Especialidade: fields.especialidade,
    TipoContrato: fields.tipoContrato,
    PercentualComissaoDefault: fields.percentualComissaoDefault || undefined,
    Email: fields.email || undefined,
    UserId: null,
    BranchId: fields.branchId || undefined,
  })
  return data
}

export interface CreateSalaFields {
  nome: string
  capacidadeMaxima?: number
  branchId?: string
}

/** POST /api/salas (Owner/Admin) — mesma lacuna fechada pra Sala. */
export async function createSala(fields: CreateSalaFields): Promise<Sala> {
  const { data } = await apiClient.post<Sala>('/api/salas', {
    Nome: fields.nome,
    CapacidadeMaxima: fields.capacidadeMaxima || undefined,
    BranchId: fields.branchId || undefined,
  })
  return data
}

/** PUT /api/profissionais/{id} (Owner/Admin) — edição de cadastro (tela de Configurações). Não reenvia convite. */
export async function updateProfissional(id: string, fields: CreateProfissionalFields): Promise<Profissional> {
  const { data } = await apiClient.put<Profissional>(`/api/profissionais/${id}`, {
    Nome: fields.nome,
    Especialidade: fields.especialidade,
    TipoContrato: fields.tipoContrato,
    PercentualComissaoDefault: fields.percentualComissaoDefault || undefined,
    Email: fields.email || undefined,
    BranchId: fields.branchId || undefined,
  })
  return data
}

/** PUT /api/salas/{id} (Owner/Admin) — edição de cadastro. */
export async function updateSala(id: string, fields: CreateSalaFields): Promise<Sala> {
  const { data } = await apiClient.put<Sala>(`/api/salas/${id}`, {
    Nome: fields.nome,
    CapacidadeMaxima: fields.capacidadeMaxima || undefined,
    BranchId: fields.branchId || undefined,
  })
  return data
}

export interface ProcedimentoFields {
  nome: string
  valorPadrao?: number
  duracaoPadraoMinutos?: number
}

export async function listProcedimentos(): Promise<Procedimento[]> {
  const { data } = await apiClient.get<Procedimento[]>('/api/procedimentos')
  return data
}

/** POST /api/procedimentos (Owner/Admin) — catálogo de procedimentos/serviços (task 044). */
export async function createProcedimento(fields: ProcedimentoFields): Promise<Procedimento> {
  const { data } = await apiClient.post<Procedimento>('/api/procedimentos', {
    Nome: fields.nome,
    ValorPadrao: fields.valorPadrao || undefined,
    DuracaoPadraoMinutos: fields.duracaoPadraoMinutos || undefined,
  })
  return data
}

/** PUT /api/procedimentos/{id} (Owner/Admin) — edição de cadastro. */
export async function updateProcedimento(id: string, fields: ProcedimentoFields): Promise<Procedimento> {
  const { data } = await apiClient.put<Procedimento>(`/api/procedimentos/${id}`, {
    Nome: fields.nome,
    ValorPadrao: fields.valorPadrao || undefined,
    DuracaoPadraoMinutos: fields.duracaoPadraoMinutos || undefined,
  })
  return data
}
