import { apiClient } from '../../lib/api-client'
import type { PagedResult } from '../../types/common'
import type { ItemEstoque } from '../../types/estoque'

export interface ListItensEstoqueParams {
  branchId?: string
  includeInactive?: boolean
  page?: number
  pageSize?: number
}

export async function listItensEstoque(params: ListItensEstoqueParams = {}): Promise<PagedResult<ItemEstoque>> {
  const { data } = await apiClient.get<PagedResult<ItemEstoque>>('/api/estoque', { params: { pageSize: 50, ...params } })
  return data
}

export interface CreateItemEstoquePayload {
  branchId?: string
  nome: string
  unidadeMedida: string
  quantidadeMinima: number
}

export async function createItemEstoque(payload: CreateItemEstoquePayload): Promise<ItemEstoque> {
  const { data } = await apiClient.post<ItemEstoque>('/api/estoque', payload)
  return data
}

export async function registrarEntradaEstoque(id: string, quantidade: number): Promise<ItemEstoque> {
  const { data } = await apiClient.post<ItemEstoque>(`/api/estoque/${id}/entrada`, { quantidade })
  return data
}

export async function registrarSaidaEstoque(id: string, quantidade: number): Promise<ItemEstoque> {
  const { data } = await apiClient.post<ItemEstoque>(`/api/estoque/${id}/saida`, { quantidade })
  return data
}
