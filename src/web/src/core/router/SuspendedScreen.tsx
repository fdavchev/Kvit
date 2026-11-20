import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { TextWithEmailLinks } from '@/features/legal/components/TextWithEmailLinks'
import { UnsentChangesLogOutSheet } from '@/features/settings/components/UnsentChangesLogOutSheet'
import { useLogOutAndForgetCountedChanges } from '@/features/settings/hooks/useLogOutAndForgetCountedChanges'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'

export function SuspendedScreen() {
  const { t } = useTranslation()
  const logOut = useLogOutAndForgetCountedChanges(showFailure)
  const [isUnsentWarningOpen, setIsUnsentWarningOpen] = useState<boolean>(false)

  function showFailure(error: Error): void {
    console.error('Logging out from the suspended screen failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function pressLogOut(): void {
    if (logOut.unsentCount > 0) {
      setIsUnsentWarningOpen(true)
      return
    }
    logOut.logOutNow()
  }

  return (
    <KvitScreen className="justify-center">
      <KvitScreenTitle>{t('suspended.title')}</KvitScreenTitle>
      <p className="text-[1.0625rem] leading-[1.5] text-pretty">
        <TextWithEmailLinks text={t('suspended.body')} />
      </p>
      <div className="flex flex-col pt-8">
        <KvitButton variant="secondary" disabled={logOut.isLoggingOut} onClick={pressLogOut}>
          {t('settings.logOut')}
        </KvitButton>
      </div>
      {isUnsentWarningOpen && (
        <UnsentChangesLogOutSheet
          unsentCount={logOut.unsentCount}
          isLoggingOut={logOut.isLoggingOut}
          onLogOut={logOut.logOutNow}
          onClose={() => setIsUnsentWarningOpen(false)}
        />
      )}
    </KvitScreen>
  )
}
