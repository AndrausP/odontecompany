import { apiClient } from '../../lib/api-client'
import type { AgendaResumo, Branch, ComissaoResumo, DashboardResumo, FaturamentoResumo } from '../../types/reports'

export interface PeriodoParams {
  dataInicio: string
  dataFim: string
}

export async function getDashboardResumo(params: PeriodoParams): Promise<DashboardResumo> {
  const { data } = await apiClient.get<DashboardResumo>('/api/reports/dashboard', { params })
  return data
}

export async function getFaturamentoResumo(params: PeriodoParams): Promise<FaturamentoResumo> {
  const { data } = await apiClient.get<FaturamentoResumo>('/api/reports/faturamento', { params })
  return data
}

export async function getAgendaResumo(params: PeriodoParams): Promise<AgendaResumo> {
  const { data } = await apiClient.get<AgendaResumo>('/api/reports/agenda', { params })
  return data
}

export interface ComissoesParams extends PeriodoParams {
  branchId?: string
  classe?: string
}

/**
 * GET /api/reports/comissoes (task 023) — Owner/Admin/Dentista. `branchId`/`classe` só fazem
 * sentido pra Owner/Admin: o backend força a própria comissão pro Dentista e ignora esses dois
 * filtros nesse caso (ver ComissoesController.GetComissoes), então o front nem envia quando o
 * usuário logado é Dentista (ver ReportsPage).
 */
export async function getComissoesResumo(params: ComissoesParams): Promise<ComissaoResumo> {
  const { data } = await apiClient.get<ComissaoResumo>('/api/reports/comissoes', { params })
  return data
}

/**
 * GET /api/branches (Owner+Admin no backend — ver BranchesController, task 030). Chamar só
 * quando `role === 'Owner' || role === 'Admin'`; ReportsPage/ComprovantePage usam `enabled` pra
 * nunca disparar isso pra outro papel e arriscar um 403 solto na tela.
 */
export async function getBranches(): Promise<Branch[]> {
  const { data } = await apiClient.get<Branch[]>('/api/branches')
  return data
}
