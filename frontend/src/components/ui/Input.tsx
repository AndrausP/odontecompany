import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input({ error, className, ...props }, ref) {
  return (
    <div className="w-full">
      <input
        ref={ref}
        className={cn(
          'w-full rounded-md border bg-surface-raised px-3 py-2 text-sm text-ink shadow-sm transition-colors',
          'focus:outline-none focus:ring-2 focus:ring-focus-ring focus:border-brand',
          'disabled:bg-surface-sunken disabled:text-ink-muted disabled:cursor-not-allowed',
          error ? 'border-danger' : 'border-border-strong',
          className,
        )}
        {...props}
      />
      {error && <p className="mt-1 text-xs text-danger">{error}</p>}
    </div>
  )
})
