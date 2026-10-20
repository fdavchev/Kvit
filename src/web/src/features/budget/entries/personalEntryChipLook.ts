import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { cn } from '@/shared/utils/cn'

export const personalEntryChipLook = cn(
  kvitChipLook,
  'gap-1.5 aria-invalid:border-destructive disabled:pointer-events-none disabled:opacity-50',
)
