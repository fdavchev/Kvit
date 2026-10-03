import { useTranslation } from 'react-i18next'
import { resolveColorScheme } from '@/core/theme/colorScheme'
import { useTheme } from '@/core/theme/useTheme'
import { cn } from '@/shared/utils/cn'
import { kvitIconAttributes } from './kvitIconAttributes'

const iconLook =
  'col-start-1 row-start-1 size-[22px] transition-[opacity,rotate,scale] duration-180 ease-out motion-reduce:transition-none'

const shownIconLook = 'rotate-0 scale-100 opacity-100'

export function KvitThemeToggle() {
  const { t } = useTranslation()
  const { theme, setTheme } = useTheme()
  const isDark = resolveColorScheme(theme) === 'dark'

  return (
    <button
      type="button"
      aria-label={t(isDark ? 'common.switchToLightMode' : 'common.switchToDarkMode')}
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      className="pressable grid size-11 shrink-0 place-items-center rounded-full bg-track text-muted-foreground hover:text-foreground"
    >
      <svg
        {...kvitIconAttributes}
        className={cn(iconLook, isDark ? 'scale-60 rotate-60 opacity-0' : shownIconLook)}
      >
        <path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z" />
      </svg>
      <svg
        {...kvitIconAttributes}
        className={cn(iconLook, isDark ? shownIconLook : 'scale-60 -rotate-60 opacity-0')}
      >
        <circle cx="12" cy="12" r="4" />
        <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
      </svg>
    </button>
  )
}
