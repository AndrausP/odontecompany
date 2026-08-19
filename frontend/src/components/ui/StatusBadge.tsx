import { cn } from '../../lib/cn'
import { toneClasses } from '../../lib/status-tone'
import { statusLabels, statusTone } from '../../features/scheduling/status-display'
import type { AgendamentoStatus } from '../../types/scheduling'

export function StatusBadge({ status }: { status: AgendamentoStatus }) {
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
