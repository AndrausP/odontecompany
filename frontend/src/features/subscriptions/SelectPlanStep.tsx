import { useMutation, useQuery } from '@tanstack/react-query'
import { listPlans, selectPlan } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { PlanCards } from './PlanCards'
import type { PlanTier } from '../../types/subscriptions'

interface SelectPlanStepProps {
  onSelected: (tier: PlanTier) => void
}

/**
 * Passo 2 do onboarding guiado (sprint-11) — sem plano ativo, a organização não passa do
 * `RequireOrganization` (frontend) pra dentro do app de verdade. Sem cobrança real nesta rodada:
 * "Escolher plano" já ativa na hora (backend `SelectPlanCommandHandler`) — o botão "Assinar com
 * cartão" (checkout Stripe) é esqueleto, não aparece aqui de propósito (POST /subscriptions já
 * resolve o gate; checkout fica pra quando o Stripe estiver conectado de verdade).
 *
 * Cards vêm de `PlanCards` (compartilhado com a tela de configurações) — antes era uma lista de
 * linhas simples, virou grid de 3 cards com destaque pro Profissional.
 */
export function SelectPlanStep({ onSelected }: SelectPlanStepProps) {
  const { data: plans, isLoading, isError } = useQuery({ queryKey: ['subscriptions', 'plans'], queryFn: listPlans })

  const mutation = useMutation({
    mutationFn: selectPlan,
    onSuccess: (subscription) => onSelected(subscription.tier),
  })

  if (isLoading) return <p className="text-sm text-ink-muted">Carregando planos…</p>
  if (isError || !plans) return <p className="text-sm text-danger">Não foi possível carregar os planos.</p>

  return (
    <div className="space-y-3">
      <PlanCards
        plans={plans}
        pendingTier={mutation.isPending ? mutation.variables : undefined}
        onSelect={(tier) => mutation.mutate(tier)}
      />

      {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}
    </div>
  )
}
