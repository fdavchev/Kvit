import { cn } from '@/shared/utils/cn'

interface TickProps {
  isOn: boolean
}

export function Tick({ isOn }: TickProps) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        'grid size-7 flex-none place-items-center rounded-lg border-2 font-extrabold',
        isOn
          ? 'border-primary bg-primary text-primary-foreground'
          : 'border-field-border bg-field text-field-foreground',
      )}
    >
      {isOn ? '✓' : ''}
    </span>
  )
}
