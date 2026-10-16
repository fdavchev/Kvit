import { cn } from 'cn'
import type { ReactNode } from 'react'

const widthLooks = {
  readable: 'lg:max-w-[40rem]',
  wide: 'lg:max-w-[64rem]',
} as const

interface KvitScreenProps {
  children: ReactNode
  className?: string
  width?: keyof typeof widthLooks
}

export function KvitScreen({ children, className, width = 'readable' }: KvitScreenProps) {
  return (
    <main
      className={cn(
        'screen-padding mx-auto flex min-h-[calc(100dvh_-_var(--bottom-bar-height))] w-full max-w-md flex-col',
        widthLooks[width],
        className,
      )}
    >
      {children}
    </main>
  )
}
