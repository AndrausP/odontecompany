import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { format, startOfMonth } from 'date-fns'
import { ArrowLeft, Printer } from 'lucide-react'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Select } from '../../components/ui/Select'
import { getApiErrorMessage } from '../../lib/query-client'
import { useAuthStore } from '../../lib/auth-store'
import { useMe } from '../../features/auth/useMe'
import type { Role } from '../../types/auth'
import { getBranches, getComissoesResumo } from './api'

/** Papéis atendidos pelo comprovante (critério 2/3 da 028). `Recepcao` fica de fora — não gera
 * comissão (mesma regra do backend, task 023) — e some do menu já na task 026. */
const ALLOWED_ROLES: Role[] = ['Owner', 'Admin', 'Dentista']

// Roles reais do backend (Identity.Domain.Enums.Role) — mesma lista da 027, pro filtro "Classe".
const CLASSE_OPTIONS: Role[] = ['Owner', 'Admin', 'Dentista', 'Recepcao']

/** Sentinela pro bucket "sem profissional vinculado" (profissionalId null vindo do backend) —
 * precisa de um valor de <option> diferente de "" (que já significa "todos" no seletor). */
const SEM_PROFISSIONAL = '__sem-profissional__'

function formatMoney(value: number): string {
  return `R$ ${value.toFixed(2)}`
}

/** `dataInicio`/`dataFim` chegam como "yyyy-MM-dd" — formata sem passar por `Date`, que
 * interpretaria a string em UTC e poderia exibir o dia errado dependendo do fuso do navegador. */
function formatDateBR(iso: string): string {
  const [y, m, d] = iso.split('-')
  if (!y || !m || !d) return iso
  return `${d}/${m}/${y}`
}

/** Resumo de uma emissão — ou o `ComissaoResumo` agregado inteiro (visão empresa, sem filtro de
 * profissional), ou os totais de um único profissional (visão colaborador, ou visão empresa com
 * um profissional selecionado no filtro). */
interface Emissao {
  nomeProfissional: string | null
  valorPagoTotal: number
  valorComissaoTotal: number
  quantidadeFaturas: number
  mediaDiariaComissao: number
  vazio: boolean
}

export function ComprovantePage() {
  const navigate = useNavigate()
  const claims = useAuthStore((s) => s.claims)
  const { data: me } = useMe()
  const role = claims?.role

  const isDentista = role === 'Dentista'
  const isEmpresa = role === 'Owner' || role === 'Admin'
  // Critério 4: Recepcao (ou usuário sem role, ex. sem organization ativa) não pode ver o
  // documento — mas recebe mensagem clara aqui dentro, nunca um redirect silencioso ou 403 cru.
  const semPermissao = !role || !ALLOWED_ROLES.includes(role)

  const today = new Date()
  const [dataInicio, setDataInicio] = useState(format(startOfMonth(today), 'yyyy-MM-dd'))
  const [dataFim, setDataFim] = useState(format(today, 'yyyy-MM-dd'))
  const [branchId, setBranchId] = useState('')
  const [classe, setClasse] = useState('')
  const [profissionalId, setProfissionalId] = useState('')

  const periodoValido = Boolean(dataInicio) && Boolean(dataFim) && dataInicio <= dataFim

  // Mesma regra da 027 (ver comentário em api.ts): Dentista nunca manda branchId/classe — o
  // backend ignora mesmo, mas a tela também esconde os dois filtros pra esse papel.
  const params = {
    dataInicio,
    dataFim,
    branchId: isDentista ? undefined : branchId || undefined,
    classe: isDentista ? undefined : classe || undefined,
  }

  // /api/branches é Owner+Admin no backend (task 030 deu paridade de acesso a Owner) — dispara
  // pra quem o endpoint realmente atende.
  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: getBranches,
    enabled: isEmpresa && !semPermissao,
  })

  const comissoes = useQuery({
    queryKey: ['reports', 'comissoes', 'comprovante', params],
    queryFn: () => getComissoesResumo(params),
    enabled: periodoValido && !semPermissao,
  })

  const organizationName = me?.organizations.find((o) => o.organizationId === me.activeOrganizationId)
    ?.organizationName

  // Lista de profissionais pro seletor da visão empresa — vem só do que o próprio período/filtro
  // já trouxe (sem endpoint novo). Deduplicada porque uma linha pode se repetir por combinação
  // de filial/classe quando não há filtro de filial/classe aplicado.
  const profissionaisDisponiveis = useMemo(() => {
    if (!isEmpresa || !comissoes.data) return []
    const map = new Map<string, string>()
    for (const linha of comissoes.data.linhas) {
      const key = linha.profissionalId ?? SEM_PROFISSIONAL
      if (!map.has(key)) map.set(key, linha.profissionalNome || 'Não atribuído')
    }
    return Array.from(map.entries())
  }, [isEmpresa, comissoes.data])

  // Monta a emissão a exibir/imprimir: agregado geral, ou só um profissional (colaborador
  // sempre; empresa quando seleciona alguém no filtro).
  const emissao: Emissao | null = useMemo(() => {
    if (!comissoes.data) return null
    const resumo = comissoes.data

    if (isDentista) {
      const linha = resumo.linhas[0]
      // R12: Dentista sem Profissional vinculado recebe totais zerados do backend — mostrar como
      // estado vazio (nome ainda vem de /api/me), nunca como erro.
      const nome = me?.user.nome ?? linha?.profissionalNome ?? null
      return {
        nomeProfissional: nome,
        valorPagoTotal: resumo.valorPagoTotal,
        valorComissaoTotal: resumo.valorComissaoTotal,
        quantidadeFaturas: resumo.quantidadeFaturas,
        mediaDiariaComissao: resumo.mediaDiariaComissao,
        vazio: resumo.quantidadeFaturas === 0 && resumo.valorComissaoTotal === 0,
      }
    }

    if (isEmpresa && profissionalId) {
      const linhasDoProfissional = resumo.linhas.filter(
        (l) => (l.profissionalId ?? SEM_PROFISSIONAL) === profissionalId,
      )
      const valorPagoTotal = linhasDoProfissional.reduce((acc, l) => acc + l.valorPago, 0)
      const valorComissaoTotal = linhasDoProfissional.reduce((acc, l) => acc + l.valorComissao, 0)
      const quantidadeFaturas = linhasDoProfissional.reduce((acc, l) => acc + l.quantidadeFaturas, 0)
      const mediaDiariaComissao = resumo.diasNoPeriodo > 0 ? valorComissaoTotal / resumo.diasNoPeriodo : 0
      return {
        nomeProfissional: linhasDoProfissional[0]?.profissionalNome ?? 'Não atribuído',
        valorPagoTotal,
        valorComissaoTotal,
        quantidadeFaturas,
        mediaDiariaComissao,
        vazio: linhasDoProfissional.length === 0,
      }
    }

    // Visão empresa agregada (sem profissional selecionado).
    return {
      nomeProfissional: null,
      valorPagoTotal: resumo.valorPagoTotal,
      valorComissaoTotal: resumo.valorComissaoTotal,
      quantidadeFaturas: resumo.quantidadeFaturas,
      mediaDiariaComissao: resumo.mediaDiariaComissao,
      vazio: resumo.linhas.length === 0,
    }
  }, [comissoes.data, isDentista, isEmpresa, profissionalId, me])

  const emitidoEm = format(new Date(), "dd/MM/yyyy 'às' HH:mm")

  if (semPermissao) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold text-ink">Comprovante de Pagamento</h1>
        <Card>
          <CardBody>
            <p className="text-sm text-danger">
              Você não tem permissão para acessar o comprovante de pagamento de comissão.
            </p>
          </CardBody>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="no-print flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Comprovante de Pagamento</h1>
          <p className="text-sm text-ink-muted">
            {isDentista
              ? 'Comprovante da sua comissão no período.'
              : 'Comprovante de comissão — geral ou por profissional.'}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => navigate(-1)}>
            <ArrowLeft size={16} />
            Voltar
          </Button>
          <Button onClick={() => window.print()} disabled={!emissao}>
            <Printer size={16} />
            Imprimir
          </Button>
        </div>
      </div>

      <Card className="no-print">
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
          {isEmpresa && (
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
          {isEmpresa && (
            <div className="w-56">
              <Label htmlFor="profissionalId">Profissional</Label>
              <Select id="profissionalId" value={profissionalId} onChange={(e) => setProfissionalId(e.target.value)}>
                <option value="">Todos os profissionais (resumo geral)</option>
                {profissionaisDisponiveis.map(([id, nome]) => (
                  <option key={id} value={id}>
                    {nome}
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

      {comissoes.isLoading && <p className="no-print text-sm text-ink-muted">Carregando…</p>}
      {comissoes.error && (
        <p className="no-print text-sm text-danger">{getApiErrorMessage(comissoes.error)}</p>
      )}

      {emissao && (
        <div className="print-area">
          <Card>
            <CardHeader>
              <CardTitle>OdontoPlatform</CardTitle>
              <p className="text-sm font-medium text-ink">Comprovante de Pagamento de Comissão</p>
            </CardHeader>
            <CardBody className="space-y-4">
              <div className="space-y-1 text-sm text-ink-secondary">
                <p>
                  <span className="font-medium text-ink">Organização:</span> {organizationName ?? '—'}
                </p>
                <p>
                  <span className="font-medium text-ink">Profissional:</span>{' '}
                  {emissao.nomeProfissional ?? 'Todos os profissionais'}
                </p>
                <p>
                  <span className="font-medium text-ink">Período:</span> {formatDateBR(dataInicio)} –{' '}
                  {formatDateBR(dataFim)}
                </p>
              </div>

              {emissao.vazio ? (
                <p className="text-sm text-ink-muted">Nenhum pagamento no período</p>
              ) : (
                <>
                  <div className="grid grid-cols-2 gap-4 border-y border-border py-4 sm:grid-cols-4">
                    <div>
                      <p className="text-xs uppercase tracking-wide text-ink-muted">Total pago</p>
                      <p className="mt-1 text-kpi text-ink">{formatMoney(emissao.valorPagoTotal)}</p>
                    </div>
                    <div>
                      <p className="text-xs uppercase tracking-wide text-ink-muted">Total de comissão</p>
                      <p className="mt-1 text-kpi text-ink">{formatMoney(emissao.valorComissaoTotal)}</p>
                    </div>
                    <div>
                      <p className="text-xs uppercase tracking-wide text-ink-muted">Média diária</p>
                      <p className="mt-1 text-kpi text-ink">{formatMoney(emissao.mediaDiariaComissao)}</p>
                    </div>
                    <div>
                      <p className="text-xs uppercase tracking-wide text-ink-muted">Qtd. de faturas</p>
                      <p className="mt-1 text-kpi text-ink">{emissao.quantidadeFaturas}</p>
                    </div>
                  </div>

                  {/* Quebra por profissional (critério 3) — só na visão empresa agregada, sem
                      filtro de profissional selecionado. */}
                  {isEmpresa && !profissionalId && comissoes.data && comissoes.data.linhas.length > 0 && (
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
                            <tr key={`${linha.profissionalId ?? 'sem-profissional'}-${linha.branchId ?? 'sem-filial'}-${i}`}>
                              <td className="px-4 py-2 font-medium text-ink">
                                {linha.profissionalNome || 'Não atribuído'}
                              </td>
                              <td className="px-4 py-2 text-ink-secondary">{linha.branchNome ?? '—'}</td>
                              <td className="px-4 py-2 text-ink-secondary">{linha.classe ?? '—'}</td>
                              <td className="px-4 py-2 text-ink-secondary">{formatMoney(linha.valorPago)}</td>
                              <td className="px-4 py-2 text-ink-secondary">{formatMoney(linha.valorComissao)}</td>
                              <td className="px-4 py-2 text-ink-secondary">{formatMoney(linha.mediaDiariaComissao)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </>
              )}

              <p className="border-t border-border pt-3 text-xs text-ink-muted">Emitido em {emitidoEm}</p>
            </CardBody>
          </Card>
        </div>
      )}
    </div>
  )
}
