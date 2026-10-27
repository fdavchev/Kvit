import type { ReactNode, Ref } from 'react'

interface KvitScreenTitleProps {
  children: ReactNode
  ref?: Ref<HTMLHeadingElement>
}

export function KvitScreenTitle({ children, ref }: KvitScreenTitleProps) {
  return (
    <h1
      ref={ref}
      tabIndex={-1}
      className="pt-4 pb-7 text-[2.75rem] leading-[1.05] font-extrabold tracking-[-0.03em] text-balance outline-none"
    >
      {children}
    </h1>
  )
}
