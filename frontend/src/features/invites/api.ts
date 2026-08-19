import { apiClient } from '../../lib/api-client'
import type { AcceptInviteResult, PendingInvite } from '../../types/auth'

/** GET /api/me/invites (task 018) — convites pendentes pro email do usuário logado, em qualquer organization. */
export async function getMyInvites(): Promise<PendingInvite[]> {
  const { data } = await apiClient.get<PendingInvite[]>('/api/me/invites')
  return data
}

/**
 * POST /api/invites/{token}/accept (task 018) — cria a membership e marca o convite como aceito.
 * NÃO emite token novo (ver comentário de AcceptInviteResultDto no backend) — pra usar a
 * organization recém-afiliada é preciso chamar switchOrganization(organizationId) em seguida.
 * Qualquer falha (token inexistente/expirado/já usado) volta 404 genérico — anti-enumeração, o
 * feedback pro usuário é sempre "convite inválido ou expirado", nunca diferencia o motivo real.
 */
export async function acceptInvite(token: string): Promise<AcceptInviteResult> {
  const { data } = await apiClient.post<AcceptInviteResult>(`/api/invites/${encodeURIComponent(token)}/accept`)
  return data
}
