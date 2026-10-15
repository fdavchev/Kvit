import { useId, useRef, type ReactNode } from 'react'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { useToastsAboveElement } from '@/shared/hooks/useToastsAboveElement'
import { cn } from '@/shared/utils/cn'

interface ClosingBarFrameProps {
  title: string
  icon?: string
  children?: ReactNode
}

export function ClosingBarFrame({ title, icon, children }: ClosingBarFrameProps) {
  const titleId = useId()
  const rootRef = useRef<HTMLElement>(null)
  useToastsAboveElement(rootRef)

  return (
    <section
      ref={rootRef}
      aria-labelledby={titleId}
      className={cn('pointer-events-auto flex w-full flex-col gap-2 rounded-[22px] px-3.5 py-3', kvitGlassPanelLook)}
    >
      <p className="flex items-start gap-2 font-bold text-pretty">
        {icon !== undefined && <span aria-hidden="true">{icon}</span>}
        <span id={titleId}>{title}</span>
      </p>
      {children}
    </section>
  )
}
