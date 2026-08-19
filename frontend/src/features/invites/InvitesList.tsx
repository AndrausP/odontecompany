import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { acceptInvite } from './api'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Modal } from '../../components/ui/Modal'
import { getApiErrorMessage } from '../../lib/query-client'
import type { PendingInvite } from '../../types/auth'

const tokenSchema = z.object({
  token: z.string().min(1, 'Cole o token do convite'),
})

type TokenForm = z.infer<typeof tokenSchema>

interface InvitesListProps {
  invites: PendingInvite[]
  /** Chamado depois que um convite é aceito com sucesso, com a organization recém-afiliada — quem
   * usa decide o que fazer (onboarding troca pra ela na hora; /convites só atualiza a lista). */
  onAccepted?: (organizationId: string) => void
}

/**
 * Lista de convites pendentes (GET /api/me/invites, também embutido em GET /api/me) — reusada no
 * onboarding (zero organization) e na tela /convites (usuário já com organization).
 *
 * IMPORTANTE: PendingInviteDto não carrega o token em claro — o backend só guarda o HASH
 * (Identity.Domain.Entities.Invite.TokenHash) e o valor em claro só existe no momento da criação
 * do convite (devolvido na resposta HTTP de quem convida, e logado por LoggingInviteNotifier —
 * não há provider de email real nesta sprint, débito nomeado da task 016). Por isso "aceitar" aqui
 * pede o token colado manualmente, repassado por fora pelo criador do convite — não dá pra aceitar
 * só com o InviteId da lista.
 */
export function InvitesList({ invites, onAccepted }: InvitesListProps) {
  const queryClient = useQueryClient()
  const [activeInvite, setActiveInvite] = useState<PendingInvite | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TokenForm>({ resolver: zodResolver(tokenSchema) })

  const mutation = useMutation({
    mutationFn: (values: TokenForm) => acceptInvite(values.token),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['me'] })
      reset()
      setActiveInvite(null)
      onAccepted?.(result.organizationId)
    },
  })

  const closeModal = () => {
    setActiveInvite(null)
    reset()
    mutation.reset()
  }

  if (invites.length === 0) return null

  return (
    <div className="space-y-2">
      {invites.map((invite) => (
        <div
          key={invite.inviteId}
          className="flex items-center justify-between rounded-md border border-border bg-surface-raised px-4 py-3"
        >
          <div>
            <p className="text-sm font-medium text-ink">{invite.organizationName}</p>
            <p className="text-xs text-ink-muted">
              Papel: {invite.role} · Expira em {new Date(invite.expiresAt).toLocaleDateString('pt-BR')}
            </p>
          </div>
          <Button type="button" variant="secondary" onClick={() => setActiveInvite(invite)}>
            Aceitar
          </Button>
        </div>
      ))}

      <Modal open={activeInvite !== null} onClose={closeModal} title="Aceitar convite">
        {activeInvite && (
          <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
            <p className="text-sm text-ink-secondary">
              Convite para <strong>{activeInvite.organizationName}</strong>. Cole abaixo o token que quem te
              convidou te repassou (ainda não há envio automático por email nesta versão).
            </p>

            <div>
              <Label htmlFor="inviteToken">Token do convite</Label>
              <Input id="inviteToken" autoComplete="off" error={errors.token?.message} {...register('token')} />
            </div>

            {/* Backend responde 404 genérico pra token inexistente/expirado/já usado — anti-enumeração,
                nunca diferencia o motivo real (ver comentário de acceptInvite em features/invites/api.ts). */}
            {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="secondary" onClick={closeModal}>
                Cancelar
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? 'Aceitando…' : 'Aceitar convite'}
              </Button>
            </div>
          </form>
        )}
      </Modal>
    </div>
  )
}
