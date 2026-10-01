import { cn } from 'cn'
import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { themeChoices, type ThemeChoice } from '@/core/theme/theme'
import { kvitSwitchLooks } from './kvitSwitchLooks'

interface KvitThemeSwitchProps {
  theme: ThemeChoice
  onChange: (choice: ThemeChoice) => void
}

const choiceTextKeys: Record<ThemeChoice, string> = {
  system: 'settings.themeSystem',
  light: 'settings.themeLight',
  dark: 'settings.themeDark',
}

export function KvitThemeSwitch({ theme, onChange }: KvitThemeSwitchProps) {
  const { t } = useTranslation()
  const labelId = useId()

  function choose(option: ThemeChoice): void {
    if (option !== theme) {
      onChange(option)
    }
  }

  return (
    <div role="group" aria-labelledby={labelId} className="flex flex-col">
      <span id={labelId} className="mb-2 text-[0.9375rem] font-bold">
        {t('settings.theme')}
      </span>
      <div className={cn(kvitSwitchLooks.track, 'w-full')}>
        {themeChoices.map((option) => (
          <button
            key={option}
            type="button"
            aria-pressed={option === theme}
            onClick={() => choose(option)}
            className={cn(kvitSwitchLooks.option, 'flex-auto')}
          >
            {t(choiceTextKeys[option])}
          </button>
        ))}
      </div>
    </div>
  )
}
