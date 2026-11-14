import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { TextWithEmailLinks } from '@/features/legal/components/TextWithEmailLinks'
import { useLogOut } from '@/features/settings/hooks/useLogOut'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'

export function SuspendedScreen() {
  const { t } = useTranslation()
  const logOut = useLogOut()

  function logOutNow(): void {
    logOut.mutate(undefined, {
      onError: (error) => {
        console.error('Logging out from the suspended screen failed', error)
        toast.error(t(errorMessageKey(error)))
      },
    })
  }

  return (
    <KvitScreen className="justify-center">
      <KvitScreenTitle>{t('suspended.title')}</KvitScreenTitle>
      <p className="text-[1.0625rem] leading-[1.5] text-pretty">
        <TextWithEmailLinks text={t('suspended.body')} />
      </p>
      <div className="flex flex-col pt-8">
        <KvitButton variant="secondary" disabled={logOut.isPending} onClick={logOutNow}>
          {t('settings.logOut')}
        </KvitButton>
      </div>
    </KvitScreen>
  )
}
