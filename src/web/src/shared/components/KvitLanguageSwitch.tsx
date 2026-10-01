import { useTranslation } from 'react-i18next'
import { languages, type Language } from '@/core/i18n/language'
import { kvitSwitchLooks } from './kvitSwitchLooks'

interface KvitLanguageSwitchProps {
  language: Language
  onChange: (language: Language) => void
}

export function KvitLanguageSwitch({ language, onChange }: KvitLanguageSwitchProps) {
  const { t } = useTranslation()

  function choose(option: Language): void {
    if (option !== language) {
      onChange(option)
    }
  }

  return (
    <div
      role="group"
      aria-label={t('language.label')}
      className={kvitSwitchLooks.track}
    >
      {languages.map((option) => (
        <button
          key={option}
          type="button"
          lang={option}
          aria-pressed={option === language}
          onClick={() => choose(option)}
          className={kvitSwitchLooks.option}
        >
          {t(`language.${option}`)}
        </button>
      ))}
    </div>
  )
}
