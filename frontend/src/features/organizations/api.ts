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

/**
 * POST /api/branches (BranchesController, task 030) — segundo passo do onboarding guiado
 * (sprint-11): depois de criar a organization, cria a primeira filial/unidade dentro dela. Exige
 * RequireActiveOrganization no backend — só chamar depois do token trocado pelo de createOrganization.
 */
export async function createBranch(nome: string, endereco?: string): Promise<void> {
  await apiClient.post('/api/branches', { Nome: nome, Endereco: endereco || undefined })
}

/**
 * POST /api/me/skip-onboarding (sprint-11) — usuário sem organization escolheu "por enquanto
 * não". Persiste no backend (User.OnboardingSkipped) pra RequireOrganization parar de forçar
 * /onboarding em qualquer sessão/device futuro.
 */
export async function skipOnboarding(): Promise<void> {
  await apiClient.post('/api/me/skip-onboarding')
}
