import { cn } from 'cn'
import type { ComponentProps } from 'react'
import { Button } from '@/shared/components/ui/button'
import { kvitButtonLooks, type KvitButtonVariant } from './kvitButtonLooks'

interface KvitButtonProps
  extends Omit<ComponentProps<typeof Button>, 'variant' | 'size'> {
  variant?: KvitButtonVariant
}

export function KvitButton({
  variant = 'primary',
  className,
  ...props
}: KvitButtonProps) {
  const look = kvitButtonLooks[variant]
  return (
    <Button
      variant={look.shadcnVariant}
      className={cn(look.className, className)}
      {...props}
    />
  )
}
