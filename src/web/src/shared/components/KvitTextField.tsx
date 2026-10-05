import type { ComponentProps, ReactNode } from 'react'
import { cn } from '@/shared/utils/cn'

type InputProps = ComponentProps<'input'>

interface KvitTextFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  type?: 'text' | 'email' | 'password'
  placeholder?: string
  autoComplete?: string
  inputMode?: InputProps['inputMode']
  enterKeyHint?: InputProps['enterKeyHint']
  autoCapitalize?: string
  autoCorrect?: string
  spellCheck?: boolean
  hint?: string
  invalid?: boolean
  trailing?: ReactNode
}

export function KvitTextField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  hint,
  invalid,
  trailing,
  ...inputAttributes
}: KvitTextFieldProps) {
  const hintId = `${id}-hint`
  const hasTrailing: boolean = trailing !== undefined
  return (
    <div className="flex flex-col">
      <label htmlFor={id} className="mb-2 text-[0.9375rem] font-bold">
        {label}
      </label>
      <div className="relative">
        <input
          id={id}
          type={type}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          aria-describedby={hint === undefined ? undefined : hintId}
          aria-invalid={invalid}
          className={cn(
            'min-h-14 w-full rounded-[14px] border-2 border-field-border bg-field px-4 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0 aria-invalid:border-destructive',
            hasTrailing && 'pr-14',
          )}
          {...inputAttributes}
        />
        {hasTrailing && (
          <div className="absolute inset-y-0 right-1.5 flex items-center">{trailing}</div>
        )}
      </div>
      {hint !== undefined && (
        <p
          id={hintId}
          className="mt-2 text-[0.9375rem] leading-[1.45] text-pretty text-muted-foreground"
        >
          {hint}
        </p>
      )}
    </div>
  )
}
