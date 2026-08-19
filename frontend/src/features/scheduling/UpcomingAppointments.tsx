import { format, isFuture, intervalToDuration } from 'date-fns'
import { cn } from '../../lib/cn'
import type { Agendamento } from '../../types/scheduling'
import { toneClasses } from '../../lib/status-tone'
import { statusDotClasses, statusTone } from './status-display'

interface UpcomingAppointmentsProps {
  agendamentos: Agendamento[]
  patientName: (pacienteId: string) => string
  /** Ex.: "Em 2h" / "Em 3h 30m" / "Em 45m" — relativo a agora, não ao dia selecionado (é sempre a
   * próxima fila global, não fica presa ao "Resumo do dia"). */
  now?: Date
  max?: number
}

function relativeLabel(inicio: string, now: Date): string {
  const start = new Date(inicio)
  const { days = 0, hours = 0, minutes = 0 } = intervalToDuration({ start: now, end: start })
  if (days > 0) return `Em ${days}d`
  if (hours > 0) return `Em ${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`
  return `Em ${Math.max(minutes, 1)}m`
}

/** "Próximos agendamentos" — spec da imagem de referência (docs/images), task 031. Lista global
 * (não filtra pelo dia selecionado no mini calendário/calendário principal, ao contrário do
 * "Resumo do dia") — é sempre "o que vem a seguir", útil mesmo se o usuário estiver olhando outra
 * semana no calendário principal. */
export function UpcomingAppointments({ agendamentos, patientName, now = new Date(), max = 3 }: UpcomingAppointmentsProps) {
  const upcoming = agendamentos
    .filter((a) => a.status !== 'Cancelado' && isFuture(new Date(a.inicio)))
    .sort((a, b) => new Date(a.inicio).getTime() - new Date(b.inicio).getTime())
    .slice(0, max)

  if (upcoming.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum agendamento futuro.</p>
  }

  return (
    <ul className="space-y-3">
      {upcoming.map((a) => (
        <li key={a.id} className="flex items-center gap-3">
          <span className={cn('h-2 w-2 shrink-0 rounded-full', statusDotClasses[a.status])} aria-hidden="true" />
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-ink">
              {format(new Date(a.inicio), 'HH:mm')} · {patientName(a.pacienteId)}
            </p>
          </div>
          <span
            className={cn(
              'shrink-0 rounded-full px-2 py-0.5 text-xs font-medium',
              toneClasses[statusTone[a.status]],
            )}
          >
            {relativeLabel(a.inicio, now)}
          </span>
        </li>
      ))}
    </ul>
  )
}
