import type { ReactNode } from 'react'

interface KvitCategoryChoiceProps {
  label: string
  tile: ReactNode
  isPressed?: boolean
  onClick: () => void
}

export const kvitCategoryGridLook = 'grid grid-cols-4 gap-x-1.5 gap-y-2.5'

export function KvitCategoryChoice({ label, tile, isPressed, onClick }: KvitCategoryChoiceProps) {
  return (
    <button
      type="button"
      aria-pressed={isPressed}
      onClick={onClick}
      className="pressable flex min-w-0 flex-col items-center gap-1 rounded-[14px] border-2 border-transparent py-1 text-xs leading-tight font-semibold hover:bg-secondary-hover aria-pressed:border-primary"
    >
      {tile}
      <span className="w-full text-center break-words">{label}</span>
    </button>
  )
}
