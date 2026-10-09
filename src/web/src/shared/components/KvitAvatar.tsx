import { useState } from 'react'
import type { CSSProperties } from 'react'
import { cn } from '@/shared/utils/cn'

type KvitAvatarSize = 'small' | 'large' | 'fill'

type KvitAvatarColor = number | 'brand'

interface KvitAvatarProps {
  name: string
  pictureUrl: string | null
  colorIndex: KvitAvatarColor
  size?: KvitAvatarSize
}

const avatarColorCount = 10

const sizeLooks: Record<KvitAvatarSize, string> = {
  small: 'size-9 text-[0.9375rem]',
  large: 'size-13 text-[1.125rem]',
  fill: 'size-full text-xl',
}

function colorLookOf(colorIndex: KvitAvatarColor): string {
  return colorIndex === 'brand' ? 'bg-avatar text-avatar-foreground' : 'text-white'
}

function colorStyleOf(colorIndex: KvitAvatarColor): CSSProperties | undefined {
  if (colorIndex === 'brand') {
    return undefined
  }
  return { backgroundColor: `var(--avatar-${colorIndex % avatarColorCount})` }
}

export function KvitAvatar({ name, pictureUrl, colorIndex, size = 'small' }: KvitAvatarProps) {
  const [failedPictureUrl, setFailedPictureUrl] = useState<string | null>(null)
  const look = cn(
    'grid flex-none place-items-center overflow-hidden rounded-full leading-none font-extrabold',
    sizeLooks[size],
  )

  if (pictureUrl !== null && pictureUrl !== failedPictureUrl) {
    return (
      <span aria-hidden="true" className={look}>
        <img
          src={pictureUrl}
          alt=""
          referrerPolicy="no-referrer"
          className="size-full object-cover"
          onError={() => setFailedPictureUrl(pictureUrl)}
        />
      </span>
    )
  }
  const initial: string | undefined = Array.from(name.trim())[0]
  if (initial === undefined) {
    throw new Error('Expected a name to draw the initial of, got an empty name')
  }
  return (
    <span aria-hidden="true" className={cn(look, colorLookOf(colorIndex))} style={colorStyleOf(colorIndex)}>
      {initial.toUpperCase()}
    </span>
  )
}
