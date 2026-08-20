import { Check } from 'lucide-react'
import { cn } from '../../lib/cn'
import { Button } from '../../components/ui/Button'
import type { Plan, PlanTier } from '../../types/subscriptions'

/**
 * Módulos são os mesmos em todo plano (Agenda/Pacientes/Financeiro/Estoque/Relatórios) — a
 * diferença entre tiers hoje é só limite de filial + preço (`PlanCatalog` no backend não gateia
 * módulo nenhum). Bullets abaixo refletem exatamente isso, sem inventar feature que o backend não
 * confere.
 */
const commonBenefits = ['Agenda', 'Pacientes', 'Financeiro', 'Estoque', 'Relatórios']

interface PlanCardsProps {
  plans: Plan[]
  /** Tier ativo da organização, se houver — desenha "Plano atual" e desabilita o CTA desse card. */
  activeTier?: PlanTier | null
  pendingTier?: PlanTier
  disabled?: boolean
  onSelect: (tier: PlanTier) => void
}

/**
 * Cards de plano — reusado no onboarding (`SelectPlanStep`, sem `activeTier`) e na tela de
 * configurações (`SettingsPage`, com `activeTier` pra distinguir "trocar" de "plano atual").
 * Antes era uma lista de linhas simples; virou grid de 3 cards com destaque visual pro
 * Profissional (recomendado) — pedido do usuário pra "melhorar os módulos" da tela de plano.
 */
export function PlanCards({ plans, activeTier, pendingTier, disabled, onSelect }: PlanCardsProps) {
  return (
    <div className="space-y-3">
      {/* Checkout automático (Stripe) é esqueleto — auditoria pré-venda: sem isso, escolher um
          plano pago não cobrava ninguém e ninguém sabia disso. Decisão do usuário: cobrança
          manual combinada por fora enquanto o Stripe real não existe — este aviso é o que torna
          essa decisão honesta pra quem está escolhendo. */}
      <p className="rounded-md border border-info/30 bg-info-subtle px-3 py-2 text-xs text-ink-secondary">
        Escolher um plano libera o acesso na hora — a cobrança ainda é combinada diretamente com
        nossa equipe (checkout automático chega em breve).
      </p>

      <div className="grid gap-4 sm:grid-cols-3">
        {plans.map((plan) => {
          const isRecommended = plan.tier === 'Profissional'
          const isActive = activeTier === plan.tier

          return (
            <div
              key={plan.tier}
              className={cn(
                'relative flex flex-col gap-4 rounded-lg border p-5 transition-colors',
                isActive
                  ? 'border-brand bg-brand-subtle/60'
                  : isRecommended
                    ? 'border-brand bg-brand-subtle/25'
                    : 'border-border bg-surface-raised',
              )}
            >
              {isRecommended && !isActive && (
                <span className="absolute -top-3 left-4 rounded-full bg-brand-gradient px-2.5 py-0.5 text-xs font-semibold text-on-brand">
                  Recomendado
                </span>
              )}
              {isActive && (
                <span className="absolute -top-3 left-4 rounded-full bg-brand-gradient px-2.5 py-0.5 text-xs font-semibold text-on-brand">
                  Plano atual
                </span>
              )}

              <div>
                <p className="text-sm font-semibold text-ink">{plan.nome}</p>
                <p className="mt-1 text-2xl font-bold text-ink">
                  {plan.precoMensal > 0 ? (
                    <>
                      R$ {plan.precoMensal.toFixed(0)}
                      <span className="text-sm font-medium text-ink-muted">/mês</span>
                    </>
                  ) : (
                    <span className="whitespace-nowrap text-lg">Sob consulta</span>
                  )}
                </p>
                <p className="mt-1 text-xs text-ink-muted">
                  {plan.limiteFiliais >= 999
                    ? 'Unidades ilimitadas'
                    : `Até ${plan.limiteFiliais} unidade${plan.limiteFiliais > 1 ? 's' : ''}`}
                </p>
              </div>

              <ul className="flex-1 space-y-1.5 text-sm text-ink-secondary">
                {commonBenefits.map((benefit) => (
                  <li key={benefit} className="flex items-center gap-2">
                    <Check size={14} className="shrink-0 text-brand" />
                    {benefit}
                  </li>
                ))}
              </ul>

              <Button
                variant={isRecommended || isActive ? 'primary' : 'secondary'}
                className="w-full"
                disabled={disabled || isActive || pendingTier !== undefined}
                onClick={() => onSelect(plan.tier)}
              >
                {isActive ? 'Plano atual' : pendingTier === plan.tier ? 'Escolhendo…' : 'Escolher'}
              </Button>
            </div>
          )
        })}
      </div>
    </div>
  )
}
