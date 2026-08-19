import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
}

const variantClasses: Record<NonNullable<ButtonProps['variant']>, string> = {
  // bg-brand-gradient (não bg-brand): task 032 — gradiente teal→azul no dark pra bater com a foto
  // de referência; em light os 2 stops são iguais (== --color-brand), visualmente idêntico ao
  // botão sólido de sempre. hover via brightness (não bg-brand-hover): swap de bg-color não faz
  // sentido em cima de um background-image — ver docs/decisions.md.
  primary: 'bg-brand-gradient text-on-brand hover:brightness-90 focus-visible:outline-focus-ring',
  secondary:
    'bg-surface-raised text-ink border border-border-strong hover:bg-surface-sunken focus-visible:outline-focus-ring',
  // Sem token `danger-hover` no mínimo da task 024 — usa brightness() em vez de inventar hex novo (ver §5 do design-system.md).
  danger: 'bg-danger text-on-brand hover:brightness-90 focus-visible:outline-focus-ring',
  ghost: 'bg-transparent text-ink-secondary hover:bg-surface-sunken focus-visible:outline-focus-ring',
}

export function Button({ variant = 'primary', className, disabled, ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors',
        'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2',
        'disabled:opacity-50 disabled:cursor-not-allowed',
        variantClasses[variant],
        className,
      )}
      disabled={disabled}
      {...props}
    />
  )
}
