import { apiClient } from '../../lib/api-client'
import type { CreateOrganizationResult } from '../../types/auth'

/**
 * POST /api/organizations (task 018) — cria organization e o usuário autenticado vira Owner dela
 * automaticamente. Funciona com token SEM organization ativa (é a rota que dá a primeira
 * organization a quem não tem nenhuma) e também com usuário que já tem organization (multi-org).
 * Retorna par de tokens novo já escopado — não precisa chamar switch-organization depois.
 */
export async function createOrganization(nome: string): Promise<CreateOrganizationResult> {
  const { data } = await apiClient.post<CreateOrganizationResult>('/api/organizations', { Nome: nome })
  return data
}
