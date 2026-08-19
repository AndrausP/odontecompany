import type { AgendamentoStatus } from '../../types/scheduling'
import type { StatusTone } from '../../lib/status-tone'

/**
 * Única fonte de verdade pro mapeamento `AgendamentoStatus` → apresentação visual. Antes só
 * existia dentro de `StatusBadge.tsx` (sem export) — task 031 precisava do mesmo mapeamento em
 * mais 3 lugares (chip do calendário, dot da lista de próximos agendamentos, legenda), então
 * subiu pra cá. `StatusBadge` importa daqui em vez de manter cópia própria (mesmo risco de drift
 * já registrado em design-system.md §5 pro par StatusBadge/FaturaStatusBadge).
 */
export const statusLabels: Record<AgendamentoStatus, string> = {
  Agendado: 'Agendado',
  Confirmado: 'Confirmado',
  Concluido: 'Concluído',
  Cancelado: 'Cancelado',
}

export const statusTone: Record<AgendamentoStatus, StatusTone> = {
  Agendado: 'warning',
  Confirmado: 'info',
  Concluido: 'success',
  Cancelado: 'neutral',
}

/** Classe de fundo sólido (pro dot da legenda/lista) — não confundir com `toneClasses`, que é o
 * par claro (`-subtle`) pra fundo de badge. */
export const statusDotClasses: Record<AgendamentoStatus, string> = {
  Agendado: 'bg-warning',
  Confirmado: 'bg-info',
  Concluido: 'bg-success',
  Cancelado: 'bg-ink-muted',
}

/** Mesma cor do dot, como CSS var — FullCalendar exige string de cor (não classe Tailwind) pro
 * background/borderColor de evento. `var(--color-*)` em vez de hex fixo acompanha o token
 * automaticamente no dark mode. */
export const statusEventColorVar: Record<AgendamentoStatus, string> = {
  Agendado: 'var(--color-warning)',
  Confirmado: 'var(--color-info)',
  Concluido: 'var(--color-success)',
  Cancelado: 'var(--color-ink-muted)',
}
