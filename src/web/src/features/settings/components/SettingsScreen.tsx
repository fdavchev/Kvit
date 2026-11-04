import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { useMe } from '@/core/auth/useMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { removeOutboxItem } from '@/core/outbox/outboxStorage'
import { useOutboxItems } from '@/core/outbox/useOutbox'
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
import { BudgetTotalSettings } from './BudgetTotalSettings'
import { GuestLogOutSheet } from './GuestLogOutSheet'
import { UnsentChangesLogOutSheet } from './UnsentChangesLogOutSheet'

type LogOutSheet = 'guest' | 'unsentChanges'

export function SettingsScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const { theme, setTheme } = useTheme()
  const { data: me } = useMe()
  const isGuest = useIsGuest()
  const changeLanguage = useChangeLanguage()
  const logOut = useLogOut()
  const unsentItems = useOutboxItems()
  const unsentCount = unsentItems.length
  const [openLogOutSheet, setOpenLogOutSheet] = useState<LogOutSheet | null>(null)

  function showFailure(error: Error): void {
    console.error('A settings action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function switchLanguage(newLanguage: Language): void {
    changeLanguage.mutate(newLanguage, { onError: showFailure })
  }

  function logOutNow(): void {
    const personId = me?.id
    const countedIds = unsentItems.map((item) => item.id)
    logOut.mutateAsync().then(() => {
      if (personId !== undefined && countedIds.length > 0) {
        removeCountedChanges(personId, countedIds).catch((error: unknown) => {
          console.error(`Logged out, but the unsent changes of person ${personId} could not be removed from this browser`, error)
        })
      }
    }, showFailure)
  }

  function pressLogOut(): void {
    if (isGuest) {
      setOpenLogOutSheet('guest')
      return
    }
    if (unsentCount > 0) {
      setOpenLogOutSheet('unsentChanges')
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
      {me && !me.isGuest && <BudgetTotalSettings budgetTotalCurrency={me.budgetTotalCurrency} />}
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
      {openLogOutSheet === 'guest' && (
        <GuestLogOutSheet
          unsentCount={unsentCount}
          isLoggingOut={logOut.isPending}
          onLogOut={logOutNow}
          onClose={() => setOpenLogOutSheet(null)}
        />
      )}
      {openLogOutSheet === 'unsentChanges' && (
        <UnsentChangesLogOutSheet
          unsentCount={unsentCount}
          isLoggingOut={logOut.isPending}
          onLogOut={logOutNow}
          onClose={() => setOpenLogOutSheet(null)}
        />
      )}
    </KvitScreen>
  )
}

async function removeCountedChanges(personId: string, ids: readonly string[]): Promise<void> {
  for (const id of ids) {
    await removeOutboxItem(personId, id)
  }
}
