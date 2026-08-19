import { useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { FaturaStatusBadge } from './FaturaStatusBadge'
import { cancelarFatura, registrarPagamentoParcela } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import type { Fatura } from '../../types/billing'

interface FaturaDetalheModalProps {
  fatura: Fatura | null
  onClose: () => void
}

export function FaturaDetalheModal({ fatura, onClose }: FaturaDetalheModalProps) {
  const queryClient = useQueryClient()

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['faturas'] })

  const pagamentoMutation = useMutation({
    mutationFn: (parcelaId: string) => registrarPagamentoParcela(fatura!.id, parcelaId),
    onSuccess: invalidate,
  })

  const cancelarMutation = useMutation({
    mutationFn: () => cancelarFatura(fatura!.id),
    onSuccess: () => {
      invalidate()
      onClose()
    },
  })

  if (!fatura) return null

  const podeCancelar = fatura.status !== 'Paga' && fatura.status !== 'Cancelada'

  return (
    <Modal open={!!fatura} onClose={onClose} title="Detalhes da fatura">
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-sm text-ink-muted">
            {fatura.tipoFatura === 'Convenio' ? 'Convênio' : 'Particular'} — R$ {fatura.valorTotal.toFixed(2)}
          </span>
          <FaturaStatusBadge status={fatura.status} />
        </div>

        {fatura.protocoloConvenio && (
          <p className="text-xs text-ink-muted">Protocolo do convênio: {fatura.protocoloConvenio}</p>
        )}

        {fatura.comissaoDentistaPercentual != null && (
          <p className="text-xs text-ink-muted">
            Comissão: {fatura.comissaoDentistaPercentual}% (R$ {fatura.valorComissao.toFixed(2)})
          </p>
        )}

        <div className="divide-y divide-border rounded-md border border-border">
          {fatura.parcelas.map((parcela) => (
            <div key={parcela.id} className="flex items-center justify-between px-3 py-2 text-sm">
              <div>
                <p className="font-medium text-ink">
                  Parcela {parcela.numeroParcela} — R$ {parcela.valorParcela.toFixed(2)}
                </p>
                <p className="text-xs text-ink-muted">
                  Vencimento {format(new Date(parcela.dataVencimento), 'dd/MM/yyyy')}
                  {parcela.dataPagamento && ` · Paga em ${format(new Date(parcela.dataPagamento), 'dd/MM/yyyy')}`}
                </p>
              </div>
              {parcela.status === 'Pendente' || parcela.status === 'Vencida' ? (
                <Button
                  variant="secondary"
                  disabled={pagamentoMutation.isPending}
                  onClick={() => pagamentoMutation.mutate(parcela.id)}
                >
                  Registrar pagamento
                </Button>
              ) : (
                <span className="text-xs font-medium text-success">Paga</span>
              )}
            </div>
          ))}
        </div>

        {(pagamentoMutation.isError || cancelarMutation.isError) && (
          <p className="text-sm text-danger">{getApiErrorMessage(pagamentoMutation.error ?? cancelarMutation.error)}</p>
        )}

        {podeCancelar && (
          <div className="flex justify-end border-t border-border pt-3">
            <Button variant="danger" disabled={cancelarMutation.isPending} onClick={() => cancelarMutation.mutate()}>
              Cancelar fatura
            </Button>
          </div>
        )}
      </div>
    </Modal>
  )
}
