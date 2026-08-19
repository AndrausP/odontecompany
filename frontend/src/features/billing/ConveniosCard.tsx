import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { createConvenio, listConvenios } from './api'
import { getApiErrorMessage } from '../../lib/query-client'

/** Gestão de convênios — Owner+Admin, ver ConveniosController (backend, task 030). Inline nesta página porque é config auxiliar do Financeiro, não merece rota própria ainda. */
export function ConveniosCard() {
  const queryClient = useQueryClient()
  const [nome, setNome] = useState('')

  const { data: convenios } = useQuery({ queryKey: ['convenios'], queryFn: () => listConvenios(true) })

  const mutation = useMutation({
    mutationFn: () => createConvenio(nome),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['convenios'] })
      setNome('')
    },
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Convênios</CardTitle>
      </CardHeader>
      <CardBody className="space-y-3">
        <form
          className="flex gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            if (nome.trim()) mutation.mutate()
          }}
        >
          <Input placeholder="Nome do convênio" value={nome} onChange={(e) => setNome(e.target.value)} />
          <Button type="submit" variant="secondary" disabled={mutation.isPending || !nome.trim()}>
            + Adicionar
          </Button>
        </form>

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <ul className="divide-y divide-border text-sm">
          {convenios?.map((c) => (
            <li key={c.id} className="flex items-center justify-between py-1.5">
              <span className={c.ativo ? 'text-ink' : 'text-ink-muted line-through'}>{c.nome}</span>
              {!c.ativo && <span className="text-xs text-ink-muted">inativo</span>}
            </li>
          ))}
          {convenios?.length === 0 && <li className="py-1.5 text-ink-muted">Nenhum convênio cadastrado.</li>}
        </ul>
      </CardBody>
    </Card>
  )
}
