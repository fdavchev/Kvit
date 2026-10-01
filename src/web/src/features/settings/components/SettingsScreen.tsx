import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import { useTheme } from '@/core/theme/useTheme'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLanguageSwitch } from '@/shared/components/KvitLanguageSwitch'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitThemeSwitch } from '@/shared/components/KvitThemeSwitch'
import { useChangeLanguage } from '../hooks/useChangeLanguage'
import { useLogOut } from '../hooks/useLogOut'

export function SettingsScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const { theme, setTheme } = useTheme()
  const changeLanguage = useChangeLanguage()
  const logOut = useLogOut()

  function showFailure(error: Error): void {
    console.error('A settings action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function switchLanguage(newLanguage: Language): void {
    changeLanguage.mutate(newLanguage, { onError: showFailure })
  }

  function logOutNow(): void {
    logOut.mutate(undefined, { onError: showFailure })
  }

  return (
    <KvitScreen>
      <div className="flex items-center justify-between gap-4">
        <KvitBackButton to={routes.dashboard} />
        <KvitLanguageSwitch language={language} onChange={switchLanguage} />
      </div>
      <KvitScreenTitle>{t('settings.title')}</KvitScreenTitle>
      <div className="pb-8">
        <KvitThemeSwitch theme={theme} onChange={setTheme} />
      </div>
      <KvitLinkButton to={routes.changePassword} variant="secondary">
        {t('auth.changePassword.title')}
      </KvitLinkButton>
      <div className="mt-auto pt-8 md:mt-3 md:pt-0">
        <KvitButton
          variant="secondary"
          disabled={logOut.isPending}
          onClick={logOutNow}
        >
          {t('settings.logOut')}
        </KvitButton>
      </div>
    </KvitScreen>
  )
}
