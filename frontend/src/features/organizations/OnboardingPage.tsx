import { Navigate, useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useMe } from '../auth/useMe'
import { switchOrganization } from '../auth/api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { InvitesList } from '../invites/InvitesList'
import { CreateOrganizationForm } from './CreateOrganizationForm'

/**
 * Gate pós-signup (task 018) — chegada garantida pelo RequireOrganization (App.tsx) sempre que
 * GET /api/me devolve organizations: []. Convite pendente tem prioridade visual sobre "criar
 * organização", mas criar org continua disponível mesmo com convite na tela — usuário pode
 * preferir ter a própria organization em vez de entrar como membro de outra.
 */
export function OnboardingPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const setSession = useAuthStore((s) => s.setSession)
  const { data, isLoading } = useMe()

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
  // aba) — não faz sentido ficar preso no onboarding.
  if (data && data.organizations.length > 0) {
    return <Navigate to="/agenda" replace />
  }

  const pendingInvites = data?.pendingInvites ?? []

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
            <CardTitle>{pendingInvites.length > 0 ? 'Ou crie sua própria organização' : 'Criar organização'}</CardTitle>
          </CardHeader>
          <CardBody>
            <CreateOrganizationForm />
          </CardBody>
        </Card>
      </div>
    </div>
  )
}
