import { cn } from '@/shared/utils/cn'

interface KvitTabPillProps {
  activeIndex: number
  tabCount: number
  className: string
}

export function KvitTabPill({ activeIndex, tabCount, className }: KvitTabPillProps) {
  return (
    <span
      aria-hidden="true"
      data-tab-pill
      className={cn(
        'pointer-events-none absolute inset-y-1 left-1 rounded-full transition-transform duration-200 ease-out motion-reduce:transition-none',
        className,
      )}
      style={{
        width: `calc((100% - 0.5rem) / ${tabCount})`,
        transform: `translateX(${activeIndex * 100}%)`,
      }}
    />
  )
}
