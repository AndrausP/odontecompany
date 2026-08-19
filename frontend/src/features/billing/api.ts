import { apiClient } from '../../lib/api-client'
import type { PagedResult } from '../../types/common'
import type { Convenio, Fatura, FormaPagamento } from '../../types/billing'

export interface ListFaturasParams {
  pacienteId?: string
  status?: string
  page?: number
  pageSize?: number
}

export async function listFaturas(params: ListFaturasParams = {}): Promise<PagedResult<Fatura>> {
  const { data } = await apiClient.get<PagedResult<Fatura>>('/api/faturas', { params: { pageSize: 20, ...params } })
  return data
}

export interface CreateFaturaParticularPayload {
  pacienteId: string
  agendamentoId?: string
  profissionalId?: string
  valorTotal: number
  numeroParcelas: number
  formaPagamento: FormaPagamento
  comissaoDentistaPercentual?: number
}

export async function createFaturaParticular(payload: CreateFaturaParticularPayload): Promise<Fatura> {
  const { data } = await apiClient.post<Fatura>('/api/faturas/particular', payload)
  return data
}

export interface CreateFaturaConvenioPayload {
  pacienteId: string
  convenioId: string
  agendamentoId?: string
  profissionalId?: string
  valorTotal: number
  comissaoDentistaPercentual?: number
}

export async function createFaturaConvenio(payload: CreateFaturaConvenioPayload): Promise<Fatura> {
  const { data } = await apiClient.post<Fatura>('/api/faturas/convenio', payload)
  return data
}

export async function registrarPagamentoParcela(faturaId: string, parcelaId: string): Promise<Fatura> {
  const { data } = await apiClient.post<Fatura>(`/api/faturas/${faturaId}/parcelas/${parcelaId}/pagamento`)
  return data
}

export async function cancelarFatura(faturaId: string): Promise<void> {
  await apiClient.post(`/api/faturas/${faturaId}/cancelar`)
}

export async function listConvenios(includeInactive = false): Promise<Convenio[]> {
  const { data } = await apiClient.get<Convenio[]>('/api/convenios', { params: { includeInactive } })
  return data
}

export async function createConvenio(nome: string, codigoExterno?: string): Promise<Convenio> {
  const { data } = await apiClient.post<Convenio>('/api/convenios', { nome, codigoExterno })
  return data
}
