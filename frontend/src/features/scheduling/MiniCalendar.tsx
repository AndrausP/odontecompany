import { useState } from 'react'
import {
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  startOfMonth,
  startOfWeek,
  subMonths,
} from 'date-fns'
import { ptBR } from 'date-fns/locale'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '../../lib/cn'

const WEEKDAY_LABELS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']

interface MiniCalendarProps {
  /** Dia selecionado no calendário principal — controla o destaque aqui (task 031 critério 4). */
  selectedDate: Date
  /** Dias com pelo menos 1 agendamento — vira um ponto sob o número (sinal visual, sem contagem). */
  daysWithEvents: Set<string>
  onSelectDate: (date: Date) => void
}

/** "Calendário rápido" — spec da imagem de referência (docs/images), task 031. Navega o mês
 * independente do mês do calendário principal; clicar num dia só navega/seleciona, não troca de
 * view (Mês/Semana/Dia continua sendo escolha do FullCalendar). */
export function MiniCalendar({ selectedDate, daysWithEvents, onSelectDate }: MiniCalendarProps) {
  const [visibleMonth, setVisibleMonth] = useState(() => startOfMonth(selectedDate))

  const gridStart = startOfWeek(startOfMonth(visibleMonth))
  const gridEnd = endOfWeek(endOfMonth(visibleMonth))
  const days = eachDayOfInterval({ start: gridStart, end: gridEnd })

  const today = new Date()

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <p className="text-sm font-semibold capitalize text-ink">{format(visibleMonth, 'MMMM yyyy', { locale: ptBR })}</p>
        <div className="flex items-center gap-1">
          <button
            type="button"
            aria-label="Mês anterior"
            onClick={() => setVisibleMonth((m) => subMonths(m, 1))}
            className="rounded p-1 text-ink-muted hover:bg-surface-sunken hover:text-ink"
          >
            <ChevronLeft size={14} />
          </button>
          <button
            type="button"
            aria-label="Próximo mês"
            onClick={() => setVisibleMonth((m) => addMonths(m, 1))}
            className="rounded p-1 text-ink-muted hover:bg-surface-sunken hover:text-ink"
          >
            <ChevronRight size={14} />
          </button>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-y-1 text-center">
        {WEEKDAY_LABELS.map((label, i) => (
          <span key={i} className="text-xs font-medium text-ink-muted">
            {label}
          </span>
        ))}

        {days.map((day) => {
          const inMonth = isSameMonth(day, visibleMonth)
          const isSelected = isSameDay(day, selectedDate)
          const isToday = isSameDay(day, today)
          const hasEvents = daysWithEvents.has(format(day, 'yyyy-MM-dd'))

          return (
            <button
              key={day.toISOString()}
              type="button"
              onClick={() => onSelectDate(day)}
              className={cn(
                'relative mx-auto flex h-7 w-7 items-center justify-center rounded-full text-xs transition-colors',
                !inMonth && 'text-ink-muted/50',
                inMonth && !isSelected && 'text-ink-secondary hover:bg-surface-sunken',
                isSelected && 'bg-brand text-on-brand font-semibold',
                !isSelected && isToday && 'ring-1 ring-inset ring-brand text-brand font-semibold',
              )}
            >
              {format(day, 'd')}
              {hasEvents && !isSelected && (
                <span className="absolute bottom-0.5 h-1 w-1 rounded-full bg-brand" aria-hidden="true" />
              )}
            </button>
          )
        })}
      </div>
    </div>
  )
}
