import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { languages, type Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { KvitButton } from '@/shared/components/KvitButton'

export function LanguageSwitch() {
  const { t } = useTranslation()
  const { language, changeLanguage } = useLanguage()

  async function handleChange(newLanguage: Language): Promise<void> {
    try {
      await changeLanguage(newLanguage)
    } catch (error) {
      console.error('Could not change the language', error)
      toast.error(t('errors.generic'))
    }
  }

  return (
    <div role="group" aria-label={t('language.label')} className="flex items-center">
      {languages.map((option, index) => (
        <div key={option} className="flex items-center">
          {index > 0 && (
            <span aria-hidden="true" className="text-muted-foreground">
              ·
            </span>
          )}
          <KvitButton
            variant="link"
            lang={option}
            aria-pressed={option === language}
            className={
              option === language
                ? 'font-bold no-underline'
                : 'text-muted-foreground'
            }
            onClick={() => void handleChange(option)}
          >
            {t(`language.${option}`)}
          </KvitButton>
        </div>
      ))}
    </div>
  )
}
