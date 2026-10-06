import { cn } from '@/shared/utils/cn'

type KvitEmojiTileSize = 'regular' | 'large'

interface KvitEmojiTileProps {
  emoji: string
  size?: KvitEmojiTileSize
  background?: string
}

const sizeLooks: Record<KvitEmojiTileSize, string> = {
  regular: 'size-11 rounded-[14px] text-[1.4rem]',
  large: 'size-18 rounded-[22px] text-[2.2rem]',
}

export function KvitEmojiTile({ emoji, size = 'regular', background }: KvitEmojiTileProps) {
  return (
    <span
      aria-hidden="true"
      className={cn('grid flex-none place-items-center bg-brand-700 leading-none', sizeLooks[size])}
      style={background === undefined ? undefined : { backgroundColor: background }}
    >
      {emoji}
    </span>
  )
}
