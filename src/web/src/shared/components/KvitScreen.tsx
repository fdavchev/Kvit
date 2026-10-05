import { cn } from 'cn'
import type { ReactNode } from 'react'

interface KvitScreenProps {
  children: ReactNode
  className?: string
}

export function KvitScreen({ children, className }: KvitScreenProps) {
  return (
    <main
      className={cn(
        'screen-padding mx-auto flex min-h-[calc(100dvh_-_var(--bottom-bar-height))] w-full max-w-md flex-col',
        className,
      )}
    >
      {children}
    </main>
  )
}
