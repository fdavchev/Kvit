import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useIsAdmin } from '@/core/auth/useIsAdmin'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { useMe } from '@/core/auth/useMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { removeOutboxItem } from '@/core/outbox/outboxStorage'
import { useOutboxItems } from '@/core/outbox/useOutbox'
import { routes } from '@/core/router/routes'
import type { DeleteAccountNameChoice } from '@/core/services/me/meService'
import { useTheme } from '@/core/theme/useTheme'
import { useSaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLanguageSwitch } from '@/shared/components/KvitLanguageSwitch'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitThemeSwitch } from '@/shared/components/KvitThemeSwitch'
import { cn } from '@/shared/utils/cn'
import { useChangeLanguage } from '../hooks/useChangeLanguage'
import { useDeleteMyAccount } from '../hooks/useDeleteMyAccount'
import { useDownloadMyData } from '../hooks/useDownloadMyData'
import { useLogOut } from '../hooks/useLogOut'
import { BudgetTotalSettings } from './BudgetTotalSettings'
import { DeleteAccountSheet } from './DeleteAccountSheet'
import { GuestLogOutSheet } from './GuestLogOutSheet'
import { UnsentChangesLogOutSheet } from './UnsentChangesLogOutSheet'

type LogOutSheet = 'guest' | 'unsentChanges'

type DeleteAccountStep =
  | { sheet: 'nameChoice' }
  | { sheet: 'unsentChanges'; nameChoice: DeleteAccountNameChoice }

export function SettingsScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { language } = useLanguage()
  const { theme, setTheme } = useTheme()
  const { data: me } = useMe()
  const isGuest = useIsGuest()
  const isAdmin = useIsAdmin()
  const changeLanguage = useChangeLanguage()
  const logOut = useLogOut()
  const downloadMyData = useDownloadMyData()
  const oneDownloadAtATime = useSaveOnce()
  const deleteMyAccount = useDeleteMyAccount()
  const oneDeletionAtATime = useSaveOnce()
  const downloadHintId = useId()
  const unsentItems = useOutboxItems()
  const unsentCount = unsentItems.length
  const [openLogOutSheet, setOpenLogOutSheet] = useState<LogOutSheet | null>(null)
  const [deleteAccountStep, setDeleteAccountStep] = useState<DeleteAccountStep | null>(null)

  function showFailure(error: Error): void {
    console.error('A settings action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function switchLanguage(newLanguage: Language): void {
    changeLanguage.mutate(newLanguage, { onError: showFailure })
  }

  function downloadData(): void {
    if (!oneDownloadAtATime.tryStart()) {
      return
    }
    downloadMyData.mutate(undefined, {
      onSuccess: () => {
        toast.success(t('settings.downloadDataDone'))
      },
      onError: showFailure,
      onSettled: oneDownloadAtATime.finish,
    })
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

  function deleteAccountNow(nameChoice: DeleteAccountNameChoice): void {
    if (!oneDeletionAtATime.tryStart()) {
      return
    }
    const personId = me?.id
    const countedIds = unsentItems.map((item) => item.id)
    const doneTextKey = isGuest ? 'settings.deleteAccountDoneGuest' : 'settings.deleteAccountDone'
    deleteMyAccount.mutateAsync(nameChoice).then(() => {
      toast.success(t(doneTextKey))
      navigate(routes.welcome, { replace: true })
      if (personId !== undefined && countedIds.length > 0) {
        removeCountedChanges(personId, countedIds).catch((error: unknown) => {
          console.error(`Deleted the account, but the unsent changes of person ${personId} could not be removed from this browser`, error)
        })
      }
    }, showFailure).finally(oneDeletionAtATime.finish)
  }

  function confirmDeleteAccount(nameChoice: DeleteAccountNameChoice): void {
    if (unsentCount > 0) {
      setDeleteAccountStep({ sheet: 'unsentChanges', nameChoice })
      return
    }
    deleteAccountNow(nameChoice)
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
        <div className="flex flex-col gap-1.5">
          <KvitButton
            variant="secondary"
            disabled={downloadMyData.isPending}
            aria-busy={downloadMyData.isPending}
            aria-describedby={downloadHintId}
            onClick={downloadData}
          >
            {t('settings.downloadData')}
          </KvitButton>
          <p id={downloadHintId} className="text-[0.875rem] text-pretty text-muted-foreground">
            {t('settings.downloadDataHint')}
          </p>
        </div>
        <KvitLinkButton
          to={routes.terms}
          state={{ from: routes.settings }}
          variant="secondary"
        >
          {t('common.terms')}
        </KvitLinkButton>
        <KvitLinkButton
          to={routes.privacy}
          state={{ from: routes.settings }}
          variant="secondary"
        >
          {t('common.privacy')}
        </KvitLinkButton>
        {isAdmin && (
          <KvitLinkButton to={routes.admin} variant="secondary">
            {t('adminStats.title')}
          </KvitLinkButton>
        )}
        {isAdmin && (
          <KvitLinkButton to={routes.adminTools} variant="secondary">
            {t('settings.ownerTools')}
          </KvitLinkButton>
        )}
      </div>
      <div className="mt-auto flex flex-col gap-3 pt-8">
        <KvitButton variant="danger" onClick={() => setDeleteAccountStep({ sheet: 'nameChoice' })}>
          {t('settings.deleteAccount')}
        </KvitButton>
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
      {deleteAccountStep?.sheet === 'nameChoice' && (
        <DeleteAccountSheet
          isGuest={isGuest}
          isDeleting={deleteMyAccount.isPending}
          onConfirm={confirmDeleteAccount}
          onClose={() => setDeleteAccountStep(null)}
        />
      )}
      {deleteAccountStep?.sheet === 'unsentChanges' && (
        <UnsentChangesLogOutSheet
          unsentCount={unsentCount}
          isLoggingOut={deleteMyAccount.isPending}
          onLogOut={() => deleteAccountNow(deleteAccountStep.nameChoice)}
          onClose={() => setDeleteAccountStep(null)}
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
