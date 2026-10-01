import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/utils/cn'
import { kvitIconAttributes } from './kvitIconAttributes'

interface KvitPasswordToggleProps {
  isShown: boolean
  onToggle: () => void
}

const iconLook =
  'col-start-1 row-start-1 size-6 transition-[opacity,scale] duration-150 ease-out motion-reduce:transition-none'

export function KvitPasswordToggle({ isShown, onToggle }: KvitPasswordToggleProps) {
  const { t } = useTranslation()
  return (
    <button
      type="button"
      aria-pressed={isShown}
      aria-label={t(isShown ? 'common.hidePassword' : 'common.showPassword')}
      onMouseDown={(event) => event.preventDefault()}
      onClick={onToggle}
      className="pressable group grid size-11 place-items-center rounded-xl text-muted-foreground hover:text-foreground"
    >
      <svg
        {...kvitIconAttributes}
        className={cn(iconLook, 'group-aria-pressed:scale-75 group-aria-pressed:opacity-0')}
      >
        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
        <circle cx="12" cy="12" r="3" />
      </svg>
      <svg
        {...kvitIconAttributes}
        className={cn(
          iconLook,
          'scale-75 opacity-0 group-aria-pressed:scale-100 group-aria-pressed:opacity-100',
        )}
      >
        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
        <path d="M1 1l22 22" />
      </svg>
    </button>
  )
}
