import { useState } from 'react'
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

/**
 * Gate pós-signup (task 018) — chegada garantida pelo RequireOrganization (App.tsx) sempre que
 * GET /api/me devolve organizations: []. Convite pendente tem prioridade visual sobre "criar
 * organização", mas criar org continua disponível mesmo com convite na tela — usuário pode
 * preferir ter a própria organization em vez de entrar como membro de outra.
 *
 * Fluxo guiado (sprint-11): empresa (organization) → primeira unidade (branch) → app. `step`
 * só existe localmente (não precisa sobreviver a reload — se recarregar no meio, `data.organizations`
 * já tem 1 item e o guard abaixo redireciona pra /agenda direto, sem travar o usuário sem unidade
 * nenhuma; criar mais unidades depois fica fora do onboarding). "Por enquanto não" pula os dois
 * passos e persiste no backend (User.OnboardingSkipped) — RequireOrganization deixa entrar mesmo
 * sem organization depois disso.
 */
export function OnboardingPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const setSession = useAuthStore((s) => s.setSession)
  const { data, isLoading } = useMe()
  const [step, setStep] = useState<'organizacao' | 'unidade'>('organizacao')

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

  // Usuário já tem organization (voltou pra essa URL de propósito, ou aceitou convite em outra
  // aba) — não faz sentido ficar preso no onboarding. Só vale no passo 1: no passo 2 (`unidade`)
  // `data.organizations` JÁ tem a organization recém-criada por este mesmo fluxo — não é "voltou
  // com organization pronta", é o meio do caminho, criar a unidade ainda falta.
  if (step === 'organizacao' && data && data.organizations.length > 0) {
    return <Navigate to="/agenda" replace />
  }

  const pendingInvites = data?.pendingInvites ?? []

  if (step === 'unidade') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface px-4 py-10">
        <div className="w-full max-w-md space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Cadastre sua primeira unidade</CardTitle>
            </CardHeader>
            <CardBody>
              <p className="mb-4 text-sm text-ink-muted">
                Empresa criada. Agora cadastre a unidade/clínica onde você atende — dá pra criar outras
                depois, todas dentro da mesma empresa.
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
            <CreateOrganizationForm onCreated={() => setStep('unidade')} />
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
