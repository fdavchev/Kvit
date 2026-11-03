import { useTranslation } from 'react-i18next'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface GuestLogOutSheetProps {
  unsentCount: number
  isLoggingOut: boolean
  onLogOut: () => void
  onClose: () => void
}

export function GuestLogOutSheet({ unsentCount, isLoggingOut, onLogOut, onClose }: GuestLogOutSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()

  return (
    <KvitSheet title={t('settings.guest.logOutTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('settings.guest.logOutBody')}</p>
      {unsentCount > 0 && (
        <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('outbox.logOutBody', { count: unsentCount })}</p>
      )}
      <div className="flex flex-col gap-2">
        <KvitLinkButton to={routes.makeAccount}>{t('settings.guest.makeAccount')}</KvitLinkButton>
        <KvitButton variant="danger" className="bg-field" disabled={isLoggingOut} onClick={onLogOut}>
          {t('settings.guest.logOutAnyway')}
        </KvitButton>
        <KvitButton variant="link" onClick={sheet.close}>
          {t('settlements.cancel')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
