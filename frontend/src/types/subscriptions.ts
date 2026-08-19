/** Espelha Subscriptions.Domain.Enums.PlanTier. */
export type PlanTier = 'Starter' | 'Profissional' | 'Rede'

/** Espelha Subscriptions.Contracts.PlanDto. */
export interface Plan {
  tier: PlanTier
  nome: string
  limiteFiliais: number
  precoMensal: number
}

/** Espelha Subscriptions.Contracts.SubscriptionDto. */
export interface Subscription {
  id: string
  organizationId: string
  tier: PlanTier
  status: 'Ativa' | 'Cancelada'
  criadoEm: string
}

/** Espelha Subscriptions.Application.Interfaces.CheckoutSessionResult — esqueleto (sprint-11), URL de placeholder. */
export interface CheckoutSessionResult {
  checkoutUrl: string
  providerSessionId: string | null
}
