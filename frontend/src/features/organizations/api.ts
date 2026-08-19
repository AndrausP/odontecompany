import { apiClient } from '../../lib/api-client'
import type { CreateOrganizationResult } from '../../types/auth'

interface CreateOrganizationFields {
  nome: string
  cnpj?: string
  telefone?: string
  endereco?: string
}

/**
 * POST /api/organizations (task 018, +Cnpj/Telefone/Endereco opcionais sprint-11) — cria
 * organization e o usuário autenticado vira Owner dela automaticamente. Funciona com token SEM
 * organization ativa (é a rota que dá a primeira organization a quem não tem nenhuma) e também
 * com usuário que já tem organization (multi-org). Retorna par de tokens novo já escopado — não
 * precisa chamar switch-organization depois.
 */
export async function createOrganization(fields: CreateOrganizationFields): Promise<CreateOrganizationResult> {
  const { data } = await apiClient.post<CreateOrganizationResult>('/api/organizations', {
    Nome: fields.nome,
    Cnpj: fields.cnpj || undefined,
    Telefone: fields.telefone || undefined,
    Endereco: fields.endereco || undefined,
  })
  return data
}

interface CreateBranchFields {
  nome: string
  endereco?: string
  telefone?: string
}

/**
 * POST /api/branches (BranchesController, task 030, +Telefone opcional sprint-11) — terceiro
 * passo do onboarding guiado (sprint-11): depois de criar a organization e escolher o plano, cria
 * a primeira filial/unidade dentro dela. Exige RequireActiveOrganization no backend — só chamar
 * depois do token trocado pelo de createOrganization. Pode 400 com `Branch.LimiteDoPlanoAtingido`
 * se o plano ativo não permitir mais unidades (ver Tenancy.Application.Commands.CreateBranch).
 */
export async function createBranch(fields: CreateBranchFields): Promise<void> {
  await apiClient.post('/api/branches', {
    Nome: fields.nome,
    Endereco: fields.endereco || undefined,
    Telefone: fields.telefone || undefined,
  })
}

/**
 * POST /api/me/skip-onboarding (sprint-11) — usuário sem organization escolheu "por enquanto
 * não". Persiste no backend (User.OnboardingSkipped) pra RequireOrganization parar de forçar
 * /onboarding em qualquer sessão/device futuro.
 */
export async function skipOnboarding(): Promise<void> {
  await apiClient.post('/api/me/skip-onboarding')
}
