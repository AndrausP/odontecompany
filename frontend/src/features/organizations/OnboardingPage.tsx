import { useRef, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useMe } from '../auth/useMe'
import { switchOrganization } from '../auth/api'
import { skipOnboarding } from './api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { InvitesList } from '../invites/InvitesList'
import { CreateOrganizationForm } from './CreateOrganizationForm'
import { CreateBranchForm } from './CreateBranchForm'
import { SelectPlanStep } from '../subscriptions/SelectPlanStep'

type Step = 'organizacao' | 'plano' | 'unidade'

/**
 * Gate pós-signup (task 018) — chegada garantida pelo RequireOrganization (App.tsx) sempre que
 * GET /api/me indica organization ausente OU sem plano ativo. Convite pendente tem prioridade
 * visual sobre "criar organização", mas criar org continua disponível mesmo com convite na tela —
 * usuário pode preferir ter a própria organization em vez de entrar como membro de outra.
 *
 * Fluxo guiado (sprint-11): empresa (organization) → plano → primeira unidade (branch) → app.
 * "Por enquanto não" só existe no passo 1 e pula os 3, persistindo no backend
 * (`User.OnboardingSkipped`) — a partir do passo 2 não tem mais como pular (a regra de negócio é
 * bloquear quem não tem plano; se chegou a criar a organização, plano é obrigatório).
 *
 * `step` é local (não sobrevive a reload). Se recarregar no meio: `data.organizations.length > 0`
 * já basta pra pular direto pro passo `plano` (nunca de volta pro 1, criar organization de novo
 * seria errado); se a organization JÁ tinha plano e branches antes desta sprint existir (org
 * antiga, recadastrando plano agora), depois de escolher o plano vai direto pro app, sem forçar
 * criar outra unidade (`hadOrganizationOnArrival` decide isso — travado num ref na PRIMEIRA vez
 * que `data` carrega, nunca recalculado: criar a organization no passo 1 muda `data.organizations`
 * no mesmo `useMe()` cache, e se isso fosse recalculado a cada render o fluxo "acabei de criar"
 * ficaria indistinguível de "já tinha organization" assim que o passo 1 termina — bug real pego
 * testando ao vivo, ver docs/knowledge/errors-aprendidos.md).
 */
export function OnboardingPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const setSession = useAuthStore((s) => s.setSession)
  const { data, isLoading } = useMe()

  const hadOrganizationOnArrivalRef = useRef<boolean | null>(null)
  if (!isLoading && data && hadOrganizationOnArrivalRef.current === null) {
    hadOrganizationOnArrivalRef.current = data.organizations.length > 0
  }
  const hadOrganizationOnArrival = hadOrganizationOnArrivalRef.current ?? false

  const [step, setStep] = useState<Step | null>(null)
  const effectiveStep: Step = step ?? (hadOrganizationOnArrival ? 'plano' : 'organizacao')

  const skipMutation = useMutation({
    mutationFn: skipOnboarding,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] })
      navigate('/agenda', { replace: true })
    },
  })

  // acceptInvite não emite token novo — pra de fato entrar na organization recém-afiliada é
  // preciso trocar em seguida. Aqui SEMPRE troca: quem chegou no onboarding tinha zero organization,
  // então o primeiro convite aceito vira a organization ativa automaticamente (sem passo manual).
  const switchMutation = useMutation({
    mutationFn: switchOrganization,
    onSuccess: (result) => {
      setSession({ accessToken: result.accessToken, refreshToken: result.refreshToken })
      queryClient.clear()
      navigate('/agenda', { replace: true })
    },
  })

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface">
        <p className="text-sm text-ink-muted">Carregando…</p>
      </div>
    )
  }

  // Já tem organization E plano ativo — não faz sentido ficar preso no onboarding (voltou pra
  // essa URL de propósito, ou reload no fim do fluxo). SÓ dispara com `step === null` (nenhuma
  // interação nesta sessão ainda) — uma vez que `step` foi setado explicitamente (usuário avançou
  // pro passo 'plano'/'unidade' NESTA visita), esse guard nunca mais interfere: senão, assim que
  // o plano é selecionado, `data` já reflete org+plano prontos e este `if` navegava pra /agenda
  // ANTES do passo 'unidade' ter chance de renderizar — pulava a criação da 1ª unidade por
  // completo pra quem chegou sem organization nenhuma (bug real pego testando ao vivo, ver
  // docs/knowledge/errors-aprendidos.md).
  if (step === null && data && data.organizations.length > 0 && data.activePlanTier) {
    return <Navigate to="/agenda" replace />
  }

  const pendingInvites = data?.pendingInvites ?? []

  if (effectiveStep === 'plano') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-10">
        <div className="w-full max-w-lg space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Escolha um plano</CardTitle>
            </CardHeader>
            <CardBody>
              <p className="mb-4 text-sm text-ink-muted">
                Sem cobrança nesta etapa — o plano define quantas unidades sua empresa pode ter.
              </p>
              <SelectPlanStep
                onSelected={async () => {
                  // RequireOrganization lê `activePlanTier` do MESMO cache ['me'] — sem invalidar
                  // aqui, o `navigate` abaixo bate num cache stale (ainda `activePlanTier: null`)
                  // e o gate manda de volta pro /onboarding na hora (bug real pego testando ao
                  // vivo — parecia que escolher plano "não fazia nada").
                  await queryClient.invalidateQueries({ queryKey: ['me'] })
                  if (hadOrganizationOnArrival) {
                    navigate('/agenda', { replace: true })
                  } else {
                    setStep('unidade')
                  }
                }}
              />
            </CardBody>
          </Card>
        </div>
      </div>
    )
  }

  if (effectiveStep === 'unidade') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-10">
        <div className="w-full max-w-md space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Cadastre sua primeira unidade</CardTitle>
            </CardHeader>
            <CardBody>
              <p className="mb-4 text-sm text-ink-muted">
                Empresa e plano prontos. Agora cadastre a unidade/clínica onde você atende — dá pra
                criar outras depois, todas dentro da mesma empresa.
              </p>
              <CreateBranchForm onCreated={() => navigate('/agenda', { replace: true })} />
            </CardBody>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-10">
      <div className="w-full max-w-md space-y-4">
        {pendingInvites.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Você tem convites pendentes</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3">
              <InvitesList
                invites={pendingInvites}
                onAccepted={(organizationId) => switchMutation.mutate(organizationId)}
              />
              {switchMutation.isPending && <p className="text-xs text-ink-muted">Entrando na organização…</p>}
              {switchMutation.isError && (
                <p className="text-sm text-danger">{getApiErrorMessage(switchMutation.error)}</p>
              )}
            </CardBody>
          </Card>
        )}

        <Card>
          <CardHeader>
            <CardTitle>{pendingInvites.length > 0 ? 'Ou crie sua própria empresa' : 'Criar empresa'}</CardTitle>
          </CardHeader>
          <CardBody className="space-y-4">
            <CreateOrganizationForm onCreated={() => setStep('plano')} />
            <div className="flex items-center justify-center border-t border-border pt-4">
              <button
                type="button"
                onClick={() => skipMutation.mutate()}
                disabled={skipMutation.isPending}
                className="text-xs font-medium text-ink-muted hover:text-ink-secondary hover:underline disabled:opacity-50"
              >
                {skipMutation.isPending ? 'Aguarde…' : 'Por enquanto não'}
              </button>
            </div>
            {skipMutation.isError && (
              <p className="text-center text-xs text-danger">{getApiErrorMessage(skipMutation.error)}</p>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  )
}
