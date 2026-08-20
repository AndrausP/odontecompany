import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { format, startOfMonth } from 'date-fns'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Select } from '../../components/ui/Select'
import { getApiErrorMessage } from '../../lib/query-client'
import { useAuthStore } from '../../lib/auth-store'
import type { Role } from '../../types/auth'
import { getAgendaResumo, getBranches, getComissoesResumo, getDashboardResumo, getFaturamentoResumo } from './api'

function Kpi({ label, value, tone }: { label: string; value: string; tone?: 'default' | 'positive' | 'warning' }) {
  const toneClass = tone === 'positive' ? 'text-success' : tone === 'warning' ? 'text-warning' : 'text-ink'
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-ink-muted">{label}</p>
      <p className={`mt-1 text-kpi ${toneClass}`}>{value}</p>
    </div>
  )
}

// Roles reais do backend (Identity.Domain.Enums.Role) — na prática só Dentista/Owner geram
// comissão (recepção não atende, admin não é profissional clínico), mas o dropdown lista todas
// pra não esconder a opção; o estado vazio (critério 6 da 027) cobre o caso "classe sem resultado".
const CLASSE_OPTIONS: Role[] = ['Owner', 'Admin', 'Dentista', 'Recepcao']

export function ReportsPage() {
  const role = useAuthStore((s) => s.claims?.role)
  // Dentista: backend força a própria comissão e IGNORA branchId/classe enviados (D5 do
  // ComissoesController) — a tela reflete isso escondendo os dois filtros, não só desabilitando.
  const isDentista = role === 'Dentista'
  // /api/branches é Owner+Admin no backend (task 030 deu paridade de acesso a Owner) — o filtro
  // de filial só aparece pra quem o endpoint realmente atende, senão a query nem dispara e
  // ninguém vê um 403 solto na tela.
  const isEmpresa = role === 'Owner' || role === 'Admin'

  const today = new Date()
  const [dataInicio, setDataInicio] = useState(format(startOfMonth(today), 'yyyy-MM-dd'))
  const [dataFim, setDataFim] = useState(format(today, 'yyyy-MM-dd'))
  const [branchId, setBranchId] = useState('')
  const [classe, setClasse] = useState('')

  const periodoValido = Boolean(dataInicio) && Boolean(dataFim) && dataInicio <= dataFim
  const params = { dataInicio, dataFim }

  const dashboard = useQuery({
    queryKey: ['reports', 'dashboard', params],
    queryFn: () => getDashboardResumo(params),
    enabled: periodoValido,
  })

  const faturamento = useQuery({
    queryKey: ['reports', 'faturamento', params],
    queryFn: () => getFaturamentoResumo(params),
    enabled: periodoValido,
  })

  const agenda = useQuery({
    queryKey: ['reports', 'agenda', params],
    queryFn: () => getAgendaResumo(params),
    enabled: periodoValido,
  })

  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: getBranches,
    enabled: isEmpresa,
  })

  // Dentista nunca envia branchId/classe (o servidor ignora mesmo, mas não faz sentido mandar
  // um filtro que a própria tela escondeu). Owner/Admin mandam undefined quando "Todas" está
  // selecionado — string vazia não entra na query string do axios por causa do "|| undefined".
  const comissoesParams = {
    ...params,
    branchId: isDentista ? undefined : branchId || undefined,
    classe: isDentista ? undefined : classe || undefined,
  }

  const comissoes = useQuery({
    queryKey: ['reports', 'comissoes', comissoesParams],
    queryFn: () => getComissoesResumo(comissoesParams),
    enabled: periodoValido,
  })

  const anyError = dashboard.error ?? faturamento.error ?? agenda.error ?? comissoes.error
  const anyLoading = dashboard.isLoading || faturamento.isLoading || agenda.isLoading || comissoes.isLoading

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold text-ink">Relatórios</h1>
        <p className="text-sm text-ink-muted">
          Dashboard financeiro, de ocupação de agenda e de comissões por período.
        </p>
      </div>

      <Card>
        <CardBody className="flex flex-wrap items-end gap-4">
          <div>
            <Label htmlFor="dataInicio">Data início</Label>
            <Input id="dataInicio" type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="dataFim">Data fim</Label>
            <Input id="dataFim" type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
          </div>
          {isEmpresa && (
            <div className="w-48">
              <Label htmlFor="branchId">Filial</Label>
              <Select id="branchId" value={branchId} onChange={(e) => setBranchId(e.target.value)}>
                <option value="">Todas as filiais</option>
                {branches.data?.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.nome}
                  </option>
                ))}
              </Select>
            </div>
          )}
          {!isDentista && (
            <div className="w-48">
              <Label htmlFor="classe">Classe</Label>
              <Select id="classe" value={classe} onChange={(e) => setClasse(e.target.value)}>
                <option value="">Todas</option>
                {CLASSE_OPTIONS.map((opt) => (
                  <option key={opt} value={opt}>
                    {opt}
                  </option>
                ))}
              </Select>
            </div>
          )}
          {!periodoValido && (
            <p className="text-sm text-danger">Data início não pode ser depois da data fim.</p>
          )}
        </CardBody>
      </Card>

      {anyLoading && <p className="text-sm text-ink-muted">Carregando…</p>}
      {anyError && <p className="text-sm text-danger">{getApiErrorMessage(anyError)}</p>}

      {/* Auditoria pré-venda — dashboard todo zerado sem explicação parecia quebrado no primeiro
          acesso. Só aparece quando não há paciente NEM agendamento nenhum no período (proxy de
          "organização ainda não usou o sistema de verdade" — some assim que o 1º dado existir). */}
      {dashboard.data && dashboard.data.pacientesAtivos === 0 && dashboard.data.totalAgendamentos === 0 && (
        <div className="rounded-lg border border-brand/30 bg-brand-subtle/40 px-4 py-3 text-sm text-ink-secondary">
          Ainda sem dados neste período — os números abaixo enchem conforme você cadastra pacientes
          e cria agendamentos.
        </div>
      )}

      {dashboard.data && (
        <Card>
          <CardHeader>
            <CardTitle>Resumo geral</CardTitle>
          </CardHeader>
          <CardBody className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <Kpi label="Pacientes ativos" value={String(dashboard.data.pacientesAtivos)} />
            <Kpi label="Agendamentos" value={String(dashboard.data.totalAgendamentos)} />
            <Kpi label="Taxa de conclusão" value={`${dashboard.data.taxaConclusao.toFixed(1)}%`} />
            <Kpi label="Faturado" value={`R$ ${dashboard.data.valorTotalFaturado.toFixed(2)}`} />
            <Kpi label="Recebido" value={`R$ ${dashboard.data.valorTotalRecebido.toFixed(2)}`} tone="positive" />
            <Kpi label="Pendente" value={`R$ ${dashboard.data.valorTotalPendente.toFixed(2)}`} tone="warning" />
            <Kpi label="Concluídos" value={String(dashboard.data.agendamentosConcluidos)} />
            <Kpi label="Cancelados" value={String(dashboard.data.agendamentosCancelados)} />
          </CardBody>
        </Card>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        {faturamento.data && (
          <Card>
            <CardHeader>
              <CardTitle>Faturamento</CardTitle>
            </CardHeader>
            <CardBody className="grid grid-cols-2 gap-4">
              <Kpi label="Faturado" value={`R$ ${faturamento.data.valorTotalFaturado.toFixed(2)}`} />
              <Kpi label="Recebido" value={`R$ ${faturamento.data.valorTotalRecebido.toFixed(2)}`} tone="positive" />
              <Kpi label="Pendente" value={`R$ ${faturamento.data.valorTotalPendente.toFixed(2)}`} tone="warning" />
              <Kpi label="Qtd. faturas" value={String(faturamento.data.quantidadeFaturas)} />
            </CardBody>
          </Card>
        )}

        {agenda.data && (
          <Card>
            <CardHeader>
              <CardTitle>Ocupação de agenda</CardTitle>
            </CardHeader>
            <CardBody className="grid grid-cols-2 gap-4">
              <Kpi label="Total" value={String(agenda.data.totalAgendamentos)} />
              <Kpi label="Agendados" value={String(agenda.data.agendados)} />
              <Kpi label="Confirmados" value={String(agenda.data.confirmados)} />
              <Kpi label="Concluídos" value={String(agenda.data.concluidos)} tone="positive" />
              <Kpi label="Cancelados" value={String(agenda.data.cancelados)} tone="warning" />
            </CardBody>
          </Card>
        )}
      </div>

      {comissoes.data && (
        <Card>
          <CardHeader>
            <CardTitle>Comissões</CardTitle>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <Kpi label="Comissão no período" value={`R$ ${comissoes.data.valorComissaoTotal.toFixed(2)}`} />
              <Kpi label="Total pago" value={`R$ ${comissoes.data.valorPagoTotal.toFixed(2)}`} tone="positive" />
              <Kpi
                label="Média diária de comissão"
                value={`R$ ${comissoes.data.mediaDiariaComissao.toFixed(2)}`}
                tone="positive"
              />
              <Kpi label="Qtd. faturas" value={String(comissoes.data.quantidadeFaturas)} />
            </div>

            {comissoes.data.linhas.length === 0 && (
              <p className="text-sm text-ink-muted">
                Nenhuma comissão encontrada para o período/filtro selecionado.
              </p>
            )}

            {!isDentista && comissoes.data.linhas.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-surface-sunken text-left text-xs uppercase tracking-wide text-ink-muted">
                    <tr>
                      <th className="px-4 py-2">Profissional</th>
                      <th className="px-4 py-2">Filial</th>
                      <th className="px-4 py-2">Classe</th>
                      <th className="px-4 py-2">Total pago</th>
                      <th className="px-4 py-2">Comissão</th>
                      <th className="px-4 py-2">Média diária</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {comissoes.data.linhas.map((linha, i) => (
                      <tr key={linha.profissionalId ?? `sem-profissional-${i}`}>
                        <td className="px-4 py-2 font-medium text-ink">
                          {linha.profissionalNome || 'Não atribuído'}
                        </td>
                        <td className="px-4 py-2 text-ink-secondary">{linha.branchNome ?? '—'}</td>
                        <td className="px-4 py-2 text-ink-secondary">{linha.classe ?? '—'}</td>
                        <td className="px-4 py-2 text-ink-secondary">R$ {linha.valorPago.toFixed(2)}</td>
                        <td className="px-4 py-2 text-ink-secondary">R$ {linha.valorComissao.toFixed(2)}</td>
                        <td className="px-4 py-2 text-ink-secondary">R$ {linha.mediaDiariaComissao.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      )}
    </div>
  )
}
