/**
 * Convenção única status→token (design-system.md §5) — consolida `StatusBadge` (agendamento) e
 * `FaturaStatusBadge` (fatura), que antes tinham cores hardcoded quase idênticas por acidente.
 * Cada tela mapeia seu próprio enum de status pra um destes 5 "tons" conceituais.
 */
export const toneClasses = {
  warning: 'bg-warning-subtle text-warning',
  info: 'bg-info-subtle text-info',
  success: 'bg-success-subtle text-success',
  danger: 'bg-danger-subtle text-danger',
  neutral: 'bg-surface-sunken text-ink-secondary line-through',
} as const

export type StatusTone = keyof typeof toneClasses
