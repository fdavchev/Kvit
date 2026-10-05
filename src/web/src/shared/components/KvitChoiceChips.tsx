import { useId } from 'react'
import { cn } from '@/shared/utils/cn'
import { kvitChipLook } from './kvitChipLook'

interface KvitChoiceChipsOption<Value extends string> {
  value: Value
  label: string
}

interface KvitChoiceChipsProps<Value extends string> {
  label: string
  options: readonly KvitChoiceChipsOption<Value>[]
  value: string
  onChange: (value: Value) => void
  chipClassName?: string
}

export function KvitChoiceChips<Value extends string>({
  label,
  options,
  value,
  onChange,
  chipClassName,
}: KvitChoiceChipsProps<Value>) {
  const labelId = useId()

  return (
    <div role="group" aria-labelledby={labelId} className="flex flex-col">
      <span id={labelId} className="mb-2 text-[0.9375rem] font-bold">
        {label}
      </span>
      <div className="flex flex-wrap gap-2">
        {options.map((option) => (
          <button
            key={option.value}
            type="button"
            aria-pressed={option.value === value}
            onClick={() => onChange(option.value)}
            className={cn(kvitChipLook, chipClassName)}
          >
            {option.label}
          </button>
        ))}
      </div>
    </div>
  )
}
