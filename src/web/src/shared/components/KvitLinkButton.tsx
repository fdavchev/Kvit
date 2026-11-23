import { cn } from 'cn'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { buttonVariants } from '@/shared/components/ui/button'
import { kvitButtonLooks, type KvitButtonVariant } from './kvitButtonLooks'

interface KvitLinkButtonProps {
  to: string
  state?: object
  replace?: boolean
  variant?: KvitButtonVariant
  children: ReactNode
}

export function KvitLinkButton({
  to,
  state,
  replace,
  variant = 'primary',
  children,
}: KvitLinkButtonProps) {
  const look = kvitButtonLooks[variant]
  return (
    <Link
      to={to}
      state={state}
      replace={replace}
      className={cn(
        buttonVariants({ variant: look.shadcnVariant }),
        look.className,
      )}
    >
      {children}
    </Link>
  )
}
