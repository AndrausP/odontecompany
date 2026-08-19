import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { useMe } from '../auth/useMe'
import { InvitesList } from './InvitesList'

/**
 * Rota /convites — convites pendentes de um usuário que JÁ tem organization ativa (recebeu um
 * convite novo depois do onboarding). Diferente do onboarding, aceitar aqui não força troca de
 * organization: só atualiza a lista (queryClient.invalidateQueries dentro de InvitesList) — o
 * usuário troca manualmente pelo seletor no AppLayout quando quiser.
 */
export function InvitesPage() {
  const { data, isLoading } = useMe()
  const pendingInvites = data?.pendingInvites ?? []

  return (
    <Card>
      <CardHeader>
        <CardTitle>Convites pendentes</CardTitle>
      </CardHeader>
      <CardBody>
        {isLoading && <p className="text-sm text-ink-muted">Carregando…</p>}
        {!isLoading && pendingInvites.length === 0 && (
          <p className="text-sm text-ink-muted">Você não tem convites pendentes no momento.</p>
        )}
        {!isLoading && pendingInvites.length > 0 && <InvitesList invites={pendingInvites} />}
      </CardBody>
    </Card>
  )
}
