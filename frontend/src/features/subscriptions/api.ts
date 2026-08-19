import { apiClient } from '../../lib/api-client'
import type { CheckoutSessionResult, Plan, PlanTier, Subscription } from '../../types/subscriptions'

/** GET /api/subscriptions/plans — catálogo estático dos 3 planos, mesmo conteúdo da landing. */
export async function listPlans(): Promise<Plan[]> {
  const { data } = await apiClient.get<Plan[]>('/api/subscriptions/plans')
  return data
}

/** GET /api/subscriptions/me — assinatura ativa da organização do token, ou null (onboarding incompleto). */
export async function getMySubscription(): Promise<Subscription | null> {
  const { data } = await apiClient.get<Subscription | null>('/api/subscriptions/me')
  return data
}

/** POST /api/subscriptions — escolhe (ou troca) o plano da organização do token. Sem cobrança nesta rodada. */
export async function selectPlan(tier: PlanTier): Promise<Subscription> {
  const { data } = await apiClient.post<Subscription>('/api/subscriptions', { Tier: tier })
  return data
}

/** POST /api/subscriptions/checkout — ESQUELETO (sprint-11): não cobra de verdade, devolve URL de placeholder. */
export async function startCheckout(tier: PlanTier): Promise<CheckoutSessionResult> {
  const { data } = await apiClient.post<CheckoutSessionResult>('/api/subscriptions/checkout', { Tier: tier })
  return data
}
