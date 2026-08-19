import { forwardRef, type InputHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

interface CheckboxProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { label, error, className, id, ...props },
  ref,
) {
  return (
    <div>
      <label className="flex items-start gap-2 text-sm text-ink">
        <input
          ref={ref}
          id={id}
          type="checkbox"
          className={cn(
            'mt-0.5 h-4 w-4 rounded border-border-strong text-brand focus:ring-2 focus:ring-focus-ring',
            className,
          )}
          {...props}
        />
        <span>{label}</span>
      </label>
      {error && <p className="mt-1 text-xs text-danger">{error}</p>}
    </div>
  )
})
