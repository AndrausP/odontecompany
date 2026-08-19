import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { listPatients, deactivatePatient } from './api'
import { CreatePatientModal } from './CreatePatientModal'
import { EditPatientModal } from './EditPatientModal'
import { useAuthStore } from '../../lib/auth-store'
import { formatCpf } from '../../lib/cpf'
import type { Patient } from '../../types/patients'

const PAGE_SIZE = 20

export function PatientsPage() {
  const queryClient = useQueryClient()
  const role = useAuthStore((s) => s.claims?.role)
  const canManage = role === 'Owner' || role === 'Admin' || role === 'Recepcao'

  const [nomeFilter, setNomeFilter] = useState('')
  const [page, setPage] = useState(1)
  const [createOpen, setCreateOpen] = useState(false)
  const [editing, setEditing] = useState<Patient | null>(null)
  const [deactivatingId, setDeactivatingId] = useState<string | null>(null)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['patients', { nome: nomeFilter, page }],
    queryFn: () => listPatients({ nome: nomeFilter || undefined, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivatePatient,
    onMutate: (id) => setDeactivatingId(id),
    onSettled: () => setDeactivatingId(null),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['patients'] }),
  })

  const handleDeactivate = (patient: Patient) => {
    // Confirmação simples via window.confirm — evita apagar por engano; soft delete no backend,
    // não é destrutivo de verdade (dado nunca é removido fisicamente), mas ainda merece pausa.
    if (window.confirm(`Desativar o cadastro de ${patient.nomeCompleto}?`)) {
      deactivateMutation.mutate(patient.id)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-ink">Pacientes</h1>
        {canManage && <Button onClick={() => setCreateOpen(true)}>+ Novo paciente</Button>}
      </div>

      <Card>
        <CardHeader className="flex items-center justify-between gap-4">
          <CardTitle>Cadastro</CardTitle>
          <Input
            placeholder="Buscar por nome…"
            className="max-w-xs"
            value={nomeFilter}
            onChange={(e) => {
              setNomeFilter(e.target.value)
              setPage(1)
            }}
          />
        </CardHeader>
        <CardBody className="p-0">
          {isError && <p className="p-5 text-sm text-danger">Não foi possível carregar os pacientes.</p>}
          {isLoading && <p className="p-5 text-sm text-ink-muted">Carregando…</p>}

          {data && (
            <>
              <table className="w-full text-sm">
                <thead className="border-b border-border text-left text-xs uppercase text-ink-muted">
                  <tr>
                    <th className="px-5 py-2 font-medium">Nome</th>
                    <th className="px-5 py-2 font-medium">CPF</th>
                    <th className="px-5 py-2 font-medium">Telefone</th>
                    <th className="px-5 py-2 font-medium">Nascimento</th>
                    {canManage && <th className="px-5 py-2 font-medium text-right">Ações</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {data.items.map((patient) => (
                    <tr key={patient.id} className="hover:bg-surface-sunken">
                      <td className="px-5 py-3 font-medium text-ink">{patient.nomeCompleto}</td>
                      <td className="px-5 py-3 text-ink-secondary">{formatCpf(patient.cpf)}</td>
                      <td className="px-5 py-3 text-ink-secondary">{patient.telefone}</td>
                      <td className="px-5 py-3 text-ink-secondary">{format(new Date(patient.dataNascimento), 'dd/MM/yyyy')}</td>
                      {canManage && (
                        <td className="px-5 py-3 text-right">
                          <div className="flex justify-end gap-2">
                            <button
                              type="button"
                              onClick={() => setEditing(patient)}
                              className="text-xs font-medium text-brand hover:underline"
                            >
                              Editar
                            </button>
                            <button
                              type="button"
                              disabled={deactivatingId === patient.id}
                              onClick={() => handleDeactivate(patient)}
                              className="text-xs font-medium text-danger hover:underline disabled:opacity-50"
                            >
                              Desativar
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={canManage ? 5 : 4} className="px-5 py-8 text-center text-sm text-ink-muted">
                        Nenhum paciente encontrado.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              <div className="flex items-center justify-between border-t border-border px-5 py-3 text-sm text-ink-muted">
                <span>
                  {data.totalCount} paciente{data.totalCount === 1 ? '' : 's'}
                </span>
                <div className="flex items-center gap-2">
                  <Button variant="secondary" disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
                    Anterior
                  </Button>
                  <span>
                    Página {data.page} de {Math.max(data.totalPages, 1)}
                  </span>
                  <Button variant="secondary" disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
                    Próxima
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardBody>
      </Card>

      <CreatePatientModal open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditPatientModal patient={editing} onClose={() => setEditing(null)} />
    </div>
  )
}
