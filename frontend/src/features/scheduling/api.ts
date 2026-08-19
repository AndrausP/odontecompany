import { apiClient } from '../../lib/api-client'
import type { PagedResult } from '../../types/common'
import type { Agendamento, CreateAgendamentoRequest, Profissional, Sala } from '../../types/scheduling'

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
