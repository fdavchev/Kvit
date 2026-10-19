import { cn } from '@/shared/utils/cn'

type KvitEmojiTileSize = 'regular' | 'large'

type KvitEmojiTileVariant = 'filled' | 'dashed'

interface KvitEmojiTileProps {
  emoji: string
  size?: KvitEmojiTileSize
  variant?: KvitEmojiTileVariant
  background?: string
}

const sizeLooks: Record<KvitEmojiTileSize, string> = {
  regular: 'size-11 rounded-[14px] text-[1.4rem]',
  large: 'size-18 rounded-[22px] text-[2.2rem]',
}

const variantLooks: Record<KvitEmojiTileVariant, string> = {
  filled: 'bg-brand-700',
  dashed: 'border-2 border-dashed border-field-border bg-track text-muted-foreground',
}

export function KvitEmojiTile({ emoji, size = 'regular', variant = 'filled', background }: KvitEmojiTileProps) {
  return (
    <span
      aria-hidden="true"
      className={cn('grid flex-none place-items-center leading-none', variantLooks[variant], sizeLooks[size])}
      style={background === undefined ? undefined : { backgroundColor: background }}
    >
      {emoji}
    </span>
  )
}
