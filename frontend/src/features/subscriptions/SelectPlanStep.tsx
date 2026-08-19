import { useMutation, useQuery } from '@tanstack/react-query'
import { listPlans, selectPlan } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { cn } from '../../lib/cn'
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
      {plans.map((plan) => (
        <div
          key={plan.tier}
          className={cn(
            'flex items-center justify-between gap-4 rounded-lg border p-4',
            plan.tier === 'Profissional' ? 'border-brand bg-brand-subtle/40' : 'border-border',
          )}
        >
          <div>
            <p className="text-sm font-semibold text-ink">{plan.nome}</p>
            <p className="text-xs text-ink-muted">
              {plan.limiteFiliais >= 999 ? 'Unidades ilimitadas' : `Até ${plan.limiteFiliais} unidade${plan.limiteFiliais > 1 ? 's' : ''}`}
              {plan.precoMensal > 0 ? ` · R$ ${plan.precoMensal.toFixed(0)}/mês` : ' · Sob consulta'}
            </p>
          </div>
          <Button
            variant={plan.tier === 'Profissional' ? 'primary' : 'secondary'}
            disabled={mutation.isPending}
            onClick={() => mutation.mutate(plan.tier)}
          >
            {mutation.isPending && mutation.variables === plan.tier ? 'Escolhendo…' : 'Escolher'}
          </Button>
        </div>
      ))}

      {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}
    </div>
  )
}
