import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface UnsentChangesLogOutSheetProps {
  unsentCount: number
  isLoggingOut: boolean
  onLogOut: () => void
  onClose: () => void
}

export function UnsentChangesLogOutSheet({ unsentCount, isLoggingOut, onLogOut, onClose }: UnsentChangesLogOutSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()

  return (
    <KvitSheet title={t('outbox.logOutTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('outbox.logOutBody', { count: unsentCount })}</p>
      <div className="flex flex-col gap-2">
        <KvitButton variant="danger" className="bg-field" disabled={isLoggingOut} onClick={onLogOut}>
          {t('outbox.logOutAnyway')}
        </KvitButton>
        <KvitButton variant="link" onClick={sheet.close}>
          {t('outbox.stay')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
