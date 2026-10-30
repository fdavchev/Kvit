import { cn } from '@/shared/utils/cn'
import { progressFillRatio, progressStateOf, type ProgressState } from '@/shared/utils/progressState'

interface KvitProgressBarProps {
  label: string
  spentMinor: number
  limitMinor: number
}

const fillLooks: Record<ProgressState, string> = {
  ok: 'bg-positive',
  near: 'bg-warning',
  over: 'bg-negative',
}

export function KvitProgressBar({ label, spentMinor, limitMinor }: KvitProgressBarProps) {
  const state = progressStateOf(spentMinor, limitMinor)

  return (
    <span
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuenow={Math.min(spentMinor, limitMinor)}
      aria-valuemax={limitMinor}
      data-limit-state={state}
      className="block h-2 w-full overflow-hidden rounded-full bg-background"
    >
      <span
        data-limit-fill
        className={cn(
          'block h-full origin-left rounded-full transition-transform duration-200 ease-out motion-reduce:transition-none',
          fillLooks[state],
        )}
        style={{ transform: `scaleX(${progressFillRatio(spentMinor, limitMinor)})` }}
      />
    </span>
  )
}
