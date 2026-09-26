import { cn } from 'cn'
import type { ComponentProps } from 'react'
import { Button } from '@/shared/components/ui/button'

type KvitButtonVariant = 'primary' | 'secondary' | 'link'

const shadcnVariants = {
  primary: 'default',
  secondary: 'secondary',
  link: 'link',
} as const

interface KvitButtonProps
  extends Omit<ComponentProps<typeof Button>, 'variant' | 'size'> {
  variant?: KvitButtonVariant
}

export function KvitButton({
  variant = 'primary',
  className,
  ...props
}: KvitButtonProps) {
  return (
    <Button
      variant={shadcnVariants[variant]}
      className={cn('min-h-12 px-4 text-base', className)}
      {...props}
    />
  )
}
