import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { useUnlockGroup } from '../hooks/useUnlockGroup'

interface UnlockGroupButtonProps {
  groupId: string
  className?: string
}

export function UnlockGroupButton({ groupId, className }: UnlockGroupButtonProps) {
  const { t } = useTranslation()
  const unlockGroup = useUnlockGroup(groupId)

  function unlockNow(): void {
    void unlockGroup.mutateAsync().then(
      () => {
        toast.success(t('closing.unlocked'))
      },
      (error: unknown) => {
        console.error('Unlocking the group failed', error)
      },
    )
  }

  return (
    <>
      <KvitButton type="button" variant="secondary" className={className} disabled={unlockGroup.isPending} onClick={unlockNow}>
        {t('closing.unlock')}
      </KvitButton>
      {unlockGroup.isError && <KvitInlineError message={t(errorMessageKey(unlockGroup.error))} />}
    </>
  )
}
