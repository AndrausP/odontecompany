import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { format } from 'date-fns'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Select } from '../../components/ui/Select'
import { listFaturas } from './api'
import { listPatients } from '../patients/api'
import { CreateFaturaModal } from './CreateFaturaModal'
import { FaturaDetalheModal } from './FaturaDetalheModal'
import { FaturaStatusBadge } from './FaturaStatusBadge'
import { ConveniosCard } from './ConveniosCard'
import { useAuthStore } from '../../lib/auth-store'
import type { Fatura, StatusFatura } from '../../types/billing'

const statusOptions: StatusFatura[] = ['Pendente', 'ParcialmentePaga', 'Paga', 'Vencida', 'Cancelada']

export function FinanceiroPage() {
  const role = useAuthStore((s) => s.claims?.role)
  const [statusFilter, setStatusFilter] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [selecionada, setSelecionada] = useState<Fatura | null>(null)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['faturas', { status: statusFilter }],
    queryFn: () => listFaturas({ status: statusFilter || undefined }),
  })

  // Reutiliza a mesma query key de CreatePatientModal/CreateFaturaModal — cache compartilhado,
  // não dispara uma segunda requisição se já estiver quente.
  const { data: pacientesPage } = useQuery({
    queryKey: ['patients', 'select'],
    queryFn: () => listPatients({ pageSize: 100 }),
  })

  const nomePorPaciente = useMemo(() => {
    const map = new Map<string, string>()
    for (const p of pacientesPage?.items ?? []) map.set(p.id, p.nomeCompleto)
    return map
  }, [pacientesPage])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-ink">Financeiro</h1>
        <Button onClick={() => setCreateOpen(true)}>+ Nova fatura</Button>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader className="flex items-center justify-between gap-4">
            <CardTitle>Faturas</CardTitle>
            <Select className="max-w-[200px]" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos os status</option>
              {statusOptions.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </Select>
          </CardHeader>
          <CardBody className="p-0">
            {isError && <p className="p-5 text-sm text-danger">Não foi possível carregar as faturas.</p>}
            {isLoading && <p className="p-5 text-sm text-ink-muted">Carregando…</p>}

            {data && (
              <table className="w-full text-sm">
                <thead className="border-b border-border text-left text-xs uppercase text-ink-muted">
                  <tr>
                    <th className="px-5 py-2 font-medium">Paciente</th>
                    <th className="px-5 py-2 font-medium">Tipo</th>
                    <th className="px-5 py-2 font-medium">Valor</th>
                    <th className="px-5 py-2 font-medium">Status</th>
                    <th className="px-5 py-2 font-medium">Criada em</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {data.items.map((fatura) => (
                    <tr key={fatura.id} className="cursor-pointer hover:bg-surface-sunken" onClick={() => setSelecionada(fatura)}>
                      <td className="px-5 py-3 font-medium text-ink">
                        {nomePorPaciente.get(fatura.pacienteId) ?? fatura.pacienteId.slice(0, 8)}
                      </td>
                      <td className="px-5 py-3 text-ink-secondary">{fatura.tipoFatura === 'Convenio' ? 'Convênio' : 'Particular'}</td>
                      <td className="px-5 py-3 text-ink-secondary">R$ {fatura.valorTotal.toFixed(2)}</td>
                      <td className="px-5 py-3">
                        <FaturaStatusBadge status={fatura.status} />
                      </td>
                      <td className="px-5 py-3 text-ink-secondary">{format(new Date(fatura.createdAt), 'dd/MM/yyyy')}</td>
                    </tr>
                  ))}
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-5 py-8 text-center text-sm text-ink-muted">
                        Nenhuma fatura encontrada.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            )}
          </CardBody>
        </Card>

        {(role === 'Owner' || role === 'Admin') && <ConveniosCard />}
      </div>

      <CreateFaturaModal open={createOpen} onClose={() => setCreateOpen(false)} />
      <FaturaDetalheModal fatura={selecionada} onClose={() => setSelecionada(null)} />
    </div>
  )
}
