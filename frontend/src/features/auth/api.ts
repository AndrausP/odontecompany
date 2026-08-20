import { apiClient } from '../../lib/api-client'
import type { LoginResult, MeResult, SignupResult } from '../../types/auth'

export interface LoginPayload {
  email: string
  password: string
}

export async function login(payload: LoginPayload): Promise<LoginResult> {
  // Backend espera PascalCase (LoginCommand record do MediatR) — o model binder do ASP.NET
  // Core é case-insensitive por padrão, então camelCase do payload JS funciona sem conversão manual.
  const { data } = await apiClient.post<LoginResult>('/api/auth/login', {
    Email: payload.email,
    Password: payload.password,
  })
  return data
}

export interface SignupPayload {
  nome: string
  email: string
  password: string
}

/** POST /api/auth/signup (task 018) — público, sem organization. Rate limit 5/min por IP no backend. */
export async function signup(payload: SignupPayload): Promise<SignupResult> {
  const { data } = await apiClient.post<SignupResult>('/api/auth/signup', {
    Nome: payload.nome,
    Email: payload.email,
    Password: payload.password,
  })
  return data
}

/** GET /api/me (task 017/018) — snapshot completo pro cabeçalho/seletor de organization: user, organizations, org ativa e convites pendentes. */
export async function getMe(): Promise<MeResult> {
  const { data } = await apiClient.get<MeResult>('/api/me')
  return data
}

/** POST /api/auth/switch-organization (task 018) — troca a organization ativa sem refazer login, emite par de tokens novo. */
export async function switchOrganization(organizationId: string): Promise<LoginResult> {
  const { data } = await apiClient.post<LoginResult>('/api/auth/switch-organization', {
    OrganizationId: organizationId,
  })
  return data
}

/**
 * POST /api/auth/forgot-password (auditoria pré-venda) — sempre 200, exista ou não o email
 * (anti-enumeração, mesmo raciocínio do backend). Rate limit 5/min por IP.
 */
export async function forgotPassword(email: string): Promise<void> {
  await apiClient.post('/api/auth/forgot-password', { Email: email })
}

/** POST /api/auth/reset-password — token vem da URL (`?token=`), gerado por `forgotPassword`. */
export async function resetPassword(token: string, novaSenha: string): Promise<void> {
  await apiClient.post('/api/auth/reset-password', { Token: token, NovaSenha: novaSenha })
}
