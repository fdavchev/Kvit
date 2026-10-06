import type { ReactNode } from 'react'

interface ExpenseFormTitleProps {
  children: ReactNode
}

export function ExpenseFormTitle({ children }: ExpenseFormTitleProps) {
  return (
    <h1 className="pt-1 text-[1.75rem] leading-[1.15] font-extrabold tracking-[-0.02em] text-balance">
      {children}
    </h1>
  )
}
