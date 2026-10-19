import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { useMe } from '@/core/auth/useMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import { useTheme } from '@/core/theme/useTheme'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLanguageSwitch } from '@/shared/components/KvitLanguageSwitch'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitThemeSwitch } from '@/shared/components/KvitThemeSwitch'
import { cn } from '@/shared/utils/cn'
import { useChangeLanguage } from '../hooks/useChangeLanguage'
import { useLogOut } from '../hooks/useLogOut'
import { GuestLogOutSheet } from './GuestLogOutSheet'

export function SettingsScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const { theme, setTheme } = useTheme()
  const { data: me } = useMe()
  const isGuest = useIsGuest()
  const changeLanguage = useChangeLanguage()
  const logOut = useLogOut()
  const [isGuestLogOutOpen, setIsGuestLogOutOpen] = useState<boolean>(false)

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

  function pressLogOut(): void {
    if (isGuest) {
      setIsGuestLogOutOpen(true)
      return
    }
    logOutNow()
  }

  return (
    <KvitScreen>
      <div className="flex items-center justify-end gap-4">
        <KvitLanguageSwitch language={language} onChange={switchLanguage} />
      </div>
      <KvitScreenTitle>{t('settings.title')}</KvitScreenTitle>
      {isGuest && (
        <section className={cn(balancesCardLook, 'mb-8 flex flex-col gap-2.5')}>
          <h2 className="text-[1.0625rem] font-bold">{t('settings.guest.title')}</h2>
          <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('settings.guest.body')}</p>
          <KvitLinkButton to={routes.makeAccount}>{t('settings.guest.makeAccount')}</KvitLinkButton>
        </section>
      )}
      <div className="pb-8">
        <KvitThemeSwitch theme={theme} onChange={setTheme} />
      </div>
      <div className="flex flex-col gap-3">
        {!isGuest && me?.hasPassword === true && (
          <KvitLinkButton to={routes.changePassword} variant="secondary">
            {t('auth.changePassword.title')}
          </KvitLinkButton>
        )}
        {!isGuest && me?.hasPassword === false && (
          <KvitLinkButton to={routes.setPassword} variant="secondary">
            {t('auth.setPassword.title')}
          </KvitLinkButton>
        )}
        <KvitLinkButton
          to={routes.privacy}
          state={{ from: routes.settings }}
          variant="secondary"
        >
          {t('common.privacy')}
        </KvitLinkButton>
      </div>
      <div className="mt-auto pt-8">
        <KvitButton
          variant="secondary"
          disabled={logOut.isPending}
          onClick={pressLogOut}
        >
          {t('settings.logOut')}
        </KvitButton>
      </div>
      {isGuestLogOutOpen && (
        <GuestLogOutSheet
          isLoggingOut={logOut.isPending}
          onLogOut={logOutNow}
          onClose={() => setIsGuestLogOutOpen(false)}
        />
      )}
    </KvitScreen>
  )
}
