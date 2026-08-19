import { useMemo } from 'react'
import { differenceInMinutes, isSameDay } from 'date-fns'
import { CalendarCheck, Clock, Users } from 'lucide-react'
import type { Agendamento } from '../../types/scheduling'

interface DaySummaryProps {
  agendamentos: Agendamento[]
  selectedDate: Date
}

function formatDuration(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60
  if (hours === 0) return `${minutes}min`
  if (minutes === 0) return `${hours}h`
  return `${hours}h ${minutes}min`
}

/** "Resumo do dia" — spec da imagem de referência (docs/images), task 031. KPI derivado 100%
 * client-side do `agendamentos` já carregado (nenhum request próprio, nenhum endpoint de
 * agregação novo — regra de negócio da sprint: "só visual, dado já existe").
 *
 * Não usa a classe `.text-kpi` (design-system.md §2.3 reserva ela exclusivamente pro dashboard de
 * comissões — "se aparecer em outro lugar, é quebra de padrão"); usa `text-lg font-semibold`. */
export function DaySummary({ agendamentos, selectedDate }: DaySummaryProps) {
  const { count, patients, minutes } = useMemo(() => {
    const doDia = agendamentos.filter((a) => a.status !== 'Cancelado' && isSameDay(new Date(a.inicio), selectedDate))
    const uniquePacientes = new Set(doDia.map((a) => a.pacienteId))
    const totalMinutes = doDia.reduce((sum, a) => sum + differenceInMinutes(new Date(a.fim), new Date(a.inicio)), 0)
    return { count: doDia.length, patients: uniquePacientes.size, minutes: totalMinutes }
  }, [agendamentos, selectedDate])

  const items = [
    { icon: CalendarCheck, value: String(count), label: 'Agendamentos' },
    { icon: Users, value: String(patients), label: 'Pacientes' },
    { icon: Clock, value: formatDuration(minutes), label: 'Tempo total' },
  ]

  return (
    <div className="grid grid-cols-3 gap-2">
      {items.map(({ icon: Icon, value, label }) => (
        <div key={label} className="flex flex-col items-center gap-1 rounded-md bg-surface-sunken px-2 py-3 text-center">
          <Icon size={16} className="text-brand" aria-hidden="true" />
          <p className="text-lg font-semibold text-ink">{value}</p>
          <p className="text-xs text-ink-muted">{label}</p>
        </div>
      ))}
    </div>
  )
}
