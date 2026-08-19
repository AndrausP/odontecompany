import { cn } from '../../lib/cn'
import { toneClasses, type StatusTone } from '../../lib/status-tone'
import type { StatusFatura } from '../../types/billing'

const statusTone: Record<StatusFatura, StatusTone> = {
  Pendente: 'warning',
  ParcialmentePaga: 'info',
  Paga: 'success',
  Vencida: 'danger',
  Cancelada: 'neutral',
}

const statusLabels: Record<StatusFatura, string> = {
  Pendente: 'Pendente',
  ParcialmentePaga: 'Parcialmente paga',
  Paga: 'Paga',
  Vencida: 'Vencida',
  Cancelada: 'Cancelada',
}

export function FaturaStatusBadge({ status }: { status: StatusFatura }) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        toneClasses[statusTone[status]],
      )}
    >
      {statusLabels[status]}
    </span>
  )
}
