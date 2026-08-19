import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { cancelarAgendamento, concluirAgendamento, confirmarAgendamento } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import type { Agendamento } from '../../types/scheduling'

interface AgendamentoDetalheModalProps {
  agendamento: Agendamento | null
  onClose: () => void
}

export function AgendamentoDetalheModal({ agendamento, onClose }: AgendamentoDetalheModalProps) {
  const queryClient = useQueryClient()
  const [valor, setValor] = useState('')
  const [motivo, setMotivo] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const invalidateAndClose = () => {
    queryClient.invalidateQueries({ queryKey: ['agendamentos'] })
    onClose()
  }

  const confirmarMutation = useMutation({
    mutationFn: confirmarAgendamento,
    onSuccess: invalidateAndClose,
    onError: (e) => setActionError(getApiErrorMessage(e)),
  })

  const cancelarMutation = useMutation({
    mutationFn: (id: string) => cancelarAgendamento(id, motivo || undefined),
    onSuccess: invalidateAndClose,
    onError: (e) => setActionError(getApiErrorMessage(e)),
  })

  const concluirMutation = useMutation({
    mutationFn: (id: string) => concluirAgendamento(id, Number(valor)),
    onSuccess: invalidateAndClose,
    onError: (e) => setActionError(getApiErrorMessage(e)),
  })

  if (!agendamento) return null

  const isPending = confirmarMutation.isPending || cancelarMutation.isPending || concluirMutation.isPending

  return (
    <Modal open={!!agendamento} onClose={onClose} title="Detalhes do agendamento">
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-sm text-ink-muted">Status</span>
          <StatusBadge status={agendamento.status} />
        </div>

        <div className="text-sm">
          <p>
            <span className="text-ink-muted">Início: </span>
            {format(new Date(agendamento.inicio), "dd/MM/yyyy HH:mm")}
          </p>
          <p>
            <span className="text-ink-muted">Fim: </span>
            {format(new Date(agendamento.fim), "dd/MM/yyyy HH:mm")}
          </p>
          {agendamento.motivoCancelamento && (
            <p>
              <span className="text-ink-muted">Motivo do cancelamento: </span>
              {agendamento.motivoCancelamento}
            </p>
          )}
          {agendamento.valorConsulta != null && (
            <p>
              <span className="text-ink-muted">Valor: </span>
              R$ {agendamento.valorConsulta.toFixed(2)}
            </p>
          )}
        </div>

        {actionError && <p className="text-sm text-danger">{actionError}</p>}

        {(agendamento.status === 'Agendado' || agendamento.status === 'Confirmado') && (
          <div className="space-y-3 border-t border-border pt-3">
            <div>
              <Label htmlFor="motivo">Motivo do cancelamento (opcional)</Label>
              <Input id="motivo" value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Paciente remarcou…" />
            </div>

            {agendamento.status === 'Confirmado' && (
              <div>
                <Label htmlFor="valor">Valor da consulta (R$)</Label>
                <Input id="valor" type="number" min="0" step="0.01" value={valor} onChange={(e) => setValor(e.target.value)} />
              </div>
            )}

            <div className="flex justify-end gap-2">
              <Button variant="danger" disabled={isPending} onClick={() => cancelarMutation.mutate(agendamento.id)}>
                Cancelar consulta
              </Button>
              {agendamento.status === 'Agendado' && (
                <Button disabled={isPending} onClick={() => confirmarMutation.mutate(agendamento.id)}>
                  Confirmar
                </Button>
              )}
              {agendamento.status === 'Confirmado' && (
                <Button disabled={isPending || !valor} onClick={() => concluirMutation.mutate(agendamento.id)}>
                  Concluir
                </Button>
              )}
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
