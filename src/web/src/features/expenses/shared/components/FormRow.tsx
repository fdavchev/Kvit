import type { ReactNode } from 'react'

interface FormRowProps {
  label: string
  value: ReactNode
  onOpen: () => void
}

export function FormRow({ label, value, onOpen }: FormRowProps) {
  return (
    <button
      type="button"
      onClick={onOpen}
      className="pressable flex min-h-14 w-full items-center gap-2.5 rounded-[14px] bg-card px-3.5 text-left text-card-foreground hover:bg-secondary-hover"
    >
      <span className="w-24 flex-none text-[0.875rem] text-muted-foreground">{label}</span>
      <span className="min-w-0 flex-1 truncate font-semibold">{value}</span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </button>
  )
}
