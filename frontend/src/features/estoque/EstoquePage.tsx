import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Input } from '../../components/ui/Input'
import { listItensEstoque, registrarEntradaEstoque, registrarSaidaEstoque } from './api'
import { CreateItemEstoqueModal } from './CreateItemEstoqueModal'
import { getApiErrorMessage } from '../../lib/query-client'
import type { ItemEstoque } from '../../types/estoque'

function MovimentacaoRow({ item }: { item: ItemEstoque }) {
  const [quantidade, setQuantidade] = useState('')
  const queryClient = useQueryClient()

  const entrada = useMutation({
    mutationFn: (qtd: number) => registrarEntradaEstoque(item.id, qtd),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['estoque'] })
      setQuantidade('')
    },
  })

  const saida = useMutation({
    mutationFn: (qtd: number) => registrarSaidaEstoque(item.id, qtd),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['estoque'] })
      setQuantidade('')
    },
  })

  const pending = entrada.isPending || saida.isPending
  const error = entrada.error ?? saida.error

  const parsed = Number(quantidade)
  const valid = quantidade.trim() !== '' && parsed > 0

  return (
    <div className="flex items-center gap-2">
      <div className="w-24">
        <Input
          type="number"
          min="0"
          step="0.01"
          placeholder="Qtd."
          value={quantidade}
          onChange={(e) => setQuantidade(e.target.value)}
          disabled={pending}
        />
      </div>
      <Button
        type="button"
        variant="secondary"
        disabled={!valid || pending}
        onClick={() => entrada.mutate(parsed)}
      >
        + Entrada
      </Button>
      <Button
        type="button"
        variant="secondary"
        disabled={!valid || pending}
        onClick={() => saida.mutate(parsed)}
      >
        − Saída
      </Button>
      {error && <span className="text-xs text-danger">{getApiErrorMessage(error)}</span>}
    </div>
  )
}

export function EstoquePage() {
  const [createOpen, setCreateOpen] = useState(false)
  const [includeInactive, setIncludeInactive] = useState(false)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['estoque', { includeInactive }],
    queryFn: () => listItensEstoque({ includeInactive }),
  })

  const items = data?.items ?? []
  const baixoCount = items.filter((i) => i.estoqueBaixo).length

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Estoque</h1>
          <p className="text-sm text-ink-muted">
            Materiais e insumos odontológicos.
            {baixoCount > 0 && (
              <span className="ml-2 font-medium text-warning">{baixoCount} item(ns) com estoque baixo</span>
            )}
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>+ Novo item</Button>
      </div>

      <Card className="p-0 overflow-hidden">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <label className="flex items-center gap-2 text-sm text-ink-secondary">
            <input
              type="checkbox"
              checked={includeInactive}
              onChange={(e) => setIncludeInactive(e.target.checked)}
              className="rounded border-border-strong"
            />
            Mostrar itens inativos
          </label>
        </div>

        {isLoading && <p className="p-4 text-sm text-ink-muted">Carregando…</p>}
        {isError && <p className="p-4 text-sm text-danger">{getApiErrorMessage(error)}</p>}

        {!isLoading && !isError && items.length === 0 && (
          <p className="p-4 text-sm text-ink-muted">Nenhum item de estoque cadastrado.</p>
        )}

        {!isLoading && items.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-surface-sunken text-left text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-2">Nome</th>
                  <th className="px-4 py-2">Unidade</th>
                  <th className="px-4 py-2">Qtd. atual</th>
                  <th className="px-4 py-2">Qtd. mínima</th>
                  <th className="px-4 py-2">Status</th>
                  <th className="px-4 py-2">Movimentação</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {items.map((item) => (
                  <tr key={item.id} className={item.ativo ? '' : 'opacity-50'}>
                    <td className="px-4 py-2 font-medium text-ink">{item.nome}</td>
                    <td className="px-4 py-2 text-ink-secondary">{item.unidadeMedida}</td>
                    <td className="px-4 py-2 text-ink-secondary">{item.quantidadeAtual}</td>
                    <td className="px-4 py-2 text-ink-secondary">{item.quantidadeMinima}</td>
                    <td className="px-4 py-2 space-x-1">
                      {item.estoqueBaixo ? (
                        <span className="inline-flex items-center rounded-full bg-warning-subtle px-2.5 py-0.5 text-xs font-medium text-warning">
                          Estoque baixo
                        </span>
                      ) : (
                        <span className="inline-flex items-center rounded-full bg-success-subtle px-2.5 py-0.5 text-xs font-medium text-success">
                          OK
                        </span>
                      )}
                      {!item.ativo && (
                        <span className="inline-flex items-center rounded-full bg-surface-sunken px-2.5 py-0.5 text-xs font-medium text-ink-secondary">
                          Inativo
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-2">{item.ativo && <MovimentacaoRow item={item} />}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <CreateItemEstoqueModal open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
