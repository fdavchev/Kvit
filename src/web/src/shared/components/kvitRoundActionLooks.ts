import { cn } from '@/shared/utils/cn'

const roundActionLook = 'pressable inline-flex min-h-12 min-w-0 items-center gap-2 rounded-full px-5 font-extrabold'

export const kvitRoundActionLooks = {
  main: cn(roundActionLook, 'bg-primary text-primary-foreground hover:bg-primary-hover'),
  other: cn(roundActionLook, 'border-[1.5px] border-field-border text-foreground hover:bg-secondary-hover'),
} as const
