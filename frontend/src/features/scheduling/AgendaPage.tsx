import { useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin, { type DateClickArg } from '@fullcalendar/interaction'
import type { EventClickArg, EventContentArg, EventInput } from '@fullcalendar/core'
import { format, isSameDay } from 'date-fns'
import { Bell, Calendar as CalendarIcon } from 'lucide-react'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Select } from '../../components/ui/Select'
import { cn } from '../../lib/cn'
import { useMe } from '../auth/useMe'
import { listAgendamentos, listProfissionais } from './api'
import { listPatients } from '../patients/api'
import { NovoAgendamentoModal } from './NovoAgendamentoModal'
import { AgendamentoDetalheModal } from './AgendamentoDetalheModal'
import { MiniCalendar } from './MiniCalendar'
import { UpcomingAppointments } from './UpcomingAppointments'
import { DaySummary } from './DaySummary'
import { statusDotClasses, statusEventColorVar, statusLabels } from './status-display'
import type { Agendamento, AgendamentoStatus } from '../../types/scheduling'

const STATUS_OPTIONS: AgendamentoStatus[] = ['Agendado', 'Confirmado', 'Concluido', 'Cancelado']

interface EventExtendedProps {
  agendamento: Agendamento
  patientName: string
  statusLabel: string
}

/** Chip do evento — nome do paciente (existe no domínio) + status (existe e já é o que colore o
 * evento hoje). Sem procedimento: `Agendamento` não tem esse campo (divergência #2 registrada na
 * task 031/docs/decisions.md). */
function renderEventContent(arg: EventContentArg) {
  const { agendamento, patientName, statusLabel } = arg.event.extendedProps as EventExtendedProps
  return (
    <div className="min-w-0 px-0.5 py-0.5">
      <p className="truncate text-xs font-semibold">{patientName}</p>
      <p className="flex items-center gap-1 truncate text-[11px] opacity-90">
        <span className={cn('h-1.5 w-1.5 shrink-0 rounded-full', statusDotClasses[agendamento.status])} aria-hidden="true" />
        {statusLabel}
      </p>
    </div>
  )
}

export function AgendaPage() {
  const navigate = useNavigate()
  const calendarRef = useRef<FullCalendar>(null)

  const [novoModalOpen, setNovoModalOpen] = useState(false)
  const [novoModalDate, setNovoModalDate] = useState<string | undefined>(undefined)
  const [selecionado, setSelecionado] = useState<Agendamento | null>(null)
  const [selectedDate, setSelectedDate] = useState(() => new Date())

  const [busca, setBusca] = useState('')
  const [profissionalFiltro, setProfissionalFiltro] = useState('')
  const [statusFiltro, setStatusFiltro] = useState('')

  const { data: me } = useMe()
  const pendingInvitesCount = me?.pendingInvites.length ?? 0

  // Pega até 200 agendamentos (paginação da API) — suficiente pra visão de calendário sem filtro
  // de período; se o volume crescer, trocar por refetch por range visível (FullCalendar.datesSet
  // dá o range exato) é a evolução natural.
  const { data, isLoading, isError } = useQuery({
    queryKey: ['agendamentos'],
    queryFn: () => listAgendamentos({}),
  })

  // Mesma queryKey de NovoAgendamentoModal — cache compartilhado, zero request duplicado.
  const { data: profissionais } = useQuery({ queryKey: ['profissionais'], queryFn: listProfissionais })
  const { data: pacientesPage } = useQuery({
    queryKey: ['patients', 'select'],
    queryFn: () => listPatients({ pageSize: 200 }),
  })

  const patientName = useMemo(() => {
    const map = new Map((pacientesPage?.items ?? []).map((p) => [p.id, p.nomeCompleto]))
    return (pacienteId: string) => map.get(pacienteId) ?? 'Paciente'
  }, [pacientesPage])

  // Memoizado: `data?.items ?? []` cria array novo a cada render, o que invalidaria os `useMemo`
  // abaixo (daysWithEvents/filteredAgendamentos) toda vez sem necessidade — sinalizado pelo oxlint.
  const agendamentos = useMemo(() => data?.items ?? [], [data])

  const daysWithEvents = useMemo(() => {
    const set = new Set<string>()
    for (const a of agendamentos) {
      if (a.status !== 'Cancelado') set.add(format(new Date(a.inicio), 'yyyy-MM-dd'))
    }
    return set
  }, [agendamentos])

  // Filtro afeta só o calendário principal (critério 6 da task 031) — sidebar (mini calendário,
  // próximos agendamentos, resumo do dia) sempre reflete o dado completo, pra não sumir enquanto
  // o usuário só está buscando 1 paciente.
  const filteredAgendamentos = useMemo(() => {
    const term = busca.trim().toLowerCase()
    return agendamentos.filter((a) => {
      if (profissionalFiltro && a.profissionalId !== profissionalFiltro) return false
      if (statusFiltro && a.status !== statusFiltro) return false
      if (term) {
        const profissionalNome = profissionais?.find((p) => p.id === a.profissionalId)?.nome ?? ''
        const haystack = `${patientName(a.pacienteId)} ${profissionalNome}`.toLowerCase()
        if (!haystack.includes(term)) return false
      }
      return true
    })
  }, [agendamentos, busca, profissionalFiltro, statusFiltro, profissionais, patientName])

  const hasFiltros = busca !== '' || profissionalFiltro !== '' || statusFiltro !== ''
  const limparFiltros = () => {
    setBusca('')
    setProfissionalFiltro('')
    setStatusFiltro('')
  }

  const events = useMemo<EventInput[]>(
    () =>
      filteredAgendamentos.map((a) => ({
        id: a.id,
        start: a.inicio,
        end: a.fim,
        backgroundColor: statusEventColorVar[a.status],
        borderColor: statusEventColorVar[a.status],
        extendedProps: {
          agendamento: a,
          patientName: patientName(a.pacienteId),
          statusLabel: statusLabels[a.status],
        } satisfies EventExtendedProps,
      })),
    [filteredAgendamentos, patientName],
  )

  const goToDate = (date: Date) => {
    setSelectedDate(date)
    calendarRef.current?.getApi().gotoDate(date)
  }

  const handleDateClick = (arg: DateClickArg) => {
    setSelectedDate(arg.date)
    setNovoModalDate(arg.dateStr.slice(0, 10))
    setNovoModalOpen(true)
  }

  const handleEventClick = (arg: EventClickArg) => {
    setSelecionado(arg.event.extendedProps.agendamento as Agendamento)
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-ink">Agenda</h1>
          <p className="text-sm text-ink-muted">Gerencie os agendamentos da clínica</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => navigate('/convites')}
            aria-label="Convites pendentes"
            className="relative rounded-md p-2 text-ink-secondary hover:bg-surface-sunken hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring"
          >
            <Bell size={18} />
            {pendingInvitesCount > 0 && (
              <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand px-1 text-[10px] font-semibold text-on-brand">
                {pendingInvitesCount}
              </span>
            )}
          </button>
          <Button
            onClick={() => {
              setNovoModalDate(undefined)
              setNovoModalOpen(true)
            }}
          >
            + Novo agendamento
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1fr_320px] lg:items-start">
        <Card>
          <CardHeader className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <CalendarIcon size={18} className="text-brand" />
              Calendário
            </CardTitle>
          </CardHeader>
          <CardBody>
            {isError && <p className="text-sm text-danger">Não foi possível carregar os agendamentos.</p>}
            {isLoading ? (
              <p className="text-sm text-ink-muted">Carregando…</p>
            ) : (
              <>
                <FullCalendar
                  ref={calendarRef}
                  plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
                  initialView="timeGridWeek"
                  headerToolbar={{
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek,timeGridDay',
                  }}
                  locale="pt-br"
                  buttonText={{ today: 'Hoje', month: 'Mês', week: 'Semana', day: 'Dia' }}
                  allDaySlot={false}
                  slotMinTime="07:00:00"
                  slotMaxTime="20:00:00"
                  // Só rótulo de hora cheia (07:00, 08:00…), não a cada 30min — igual ao Google
                  // Calendar e à imagem de referência; menos poluição visual na régua de horário.
                  slotDuration="00:30:00"
                  slotLabelInterval="01:00:00"
                  // Linha vermelha do horário atual — task 033, tema via --fc-now-indicator-color
                  // em index.css (antes ficava desligado por ser vermelho cru sem tema).
                  nowIndicator
                  height="auto"
                  events={events}
                  eventContent={renderEventContent}
                  dateClick={handleDateClick}
                  eventClick={handleEventClick}
                  dayHeaderClassNames={(arg) => (isSameDay(arg.date, selectedDate) ? ['bg-brand-subtle'] : [])}
                />

                {/* Barra de filtros — critério 6 da task 031. Filtra só o calendário acima. */}
                <div className="mt-4 flex flex-wrap items-end gap-2 border-t border-border pt-4">
                  <div className="min-w-[200px] flex-1">
                    <Input
                      value={busca}
                      onChange={(e) => setBusca(e.target.value)}
                      placeholder="Buscar agendamentos…"
                      aria-label="Buscar por paciente ou profissional"
                    />
                  </div>
                  <div className="w-44">
                    <Select
                      value={profissionalFiltro}
                      onChange={(e) => setProfissionalFiltro(e.target.value)}
                      aria-label="Filtrar por profissional"
                    >
                      <option value="">Profissional</option>
                      {profissionais?.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.nome}
                        </option>
                      ))}
                    </Select>
                  </div>
                  <div className="w-40">
                    <Select
                      value={statusFiltro}
                      onChange={(e) => setStatusFiltro(e.target.value)}
                      aria-label="Filtrar por status"
                    >
                      <option value="">Status</option>
                      {STATUS_OPTIONS.map((s) => (
                        <option key={s} value={s}>
                          {statusLabels[s]}
                        </option>
                      ))}
                    </Select>
                  </div>
                  <Button type="button" variant="ghost" onClick={limparFiltros} disabled={!hasFiltros}>
                    Limpar filtros
                  </Button>
                </div>

                {/* Legenda de status (não procedimento — divergência #2, ver task 031). */}
                <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-secondary">
                  {STATUS_OPTIONS.map((s) => (
                    <span key={s} className="flex items-center gap-1.5">
                      <span className={cn('h-2 w-2 rounded-full', statusDotClasses[s])} aria-hidden="true" />
                      {statusLabels[s]}
                    </span>
                  ))}
                </div>
              </>
            )}
          </CardBody>
        </Card>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Calendário rápido</CardTitle>
            </CardHeader>
            <CardBody>
              <MiniCalendar selectedDate={selectedDate} daysWithEvents={daysWithEvents} onSelectDate={goToDate} />
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Próximos agendamentos</CardTitle>
            </CardHeader>
            <CardBody>
              <UpcomingAppointments agendamentos={agendamentos} patientName={patientName} />
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Resumo do dia</CardTitle>
            </CardHeader>
            <CardBody>
              <DaySummary agendamentos={agendamentos} selectedDate={selectedDate} />
            </CardBody>
          </Card>
        </div>
      </div>

      <NovoAgendamentoModal open={novoModalOpen} onClose={() => setNovoModalOpen(false)} initialDate={novoModalDate} />
      <AgendamentoDetalheModal agendamento={selecionado} onClose={() => setSelecionado(null)} />
    </div>
  )
}
