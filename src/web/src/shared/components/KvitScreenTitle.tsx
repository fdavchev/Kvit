import type { ReactNode } from 'react'

interface KvitScreenTitleProps {
  children: ReactNode
}

export function KvitScreenTitle({ children }: KvitScreenTitleProps) {
  return (
    <h1 className="pt-4 pb-7 text-[2.75rem] leading-[1.05] font-extrabold tracking-[-0.03em] text-balance">
      {children}
    </h1>
  )
}
