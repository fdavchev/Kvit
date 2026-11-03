import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { useOutboxSender } from '@/core/outbox/useOutbox'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface DiscardWaitingSheetProps {
  item: OutboxItem
  onClose: () => void
}

export function DiscardWaitingSheet({ item, onClose }: DiscardWaitingSheetProps) {
  const { t } = useTranslation()
  const sender = useOutboxSender()
  const sheet = useKvitSheet()
  const [isDiscarding, setIsDiscarding] = useState<boolean>(false)
  const isSending = item.state === 'sending'

  function discard(): void {
    setIsDiscarding(true)
    sender
      .discard(item.id)
      .then(sheet.closeIfShown)
      .catch((error: unknown) => {
        console.error(`Could not discard the waiting ${item.kind} ${item.id}`, error)
        toast.error(t('errors.generic'))
        setIsDiscarding(false)
      })
  }

  return (
    <KvitSheet title={t('outbox.discardTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('outbox.discardBody')}</p>
      <div className="flex flex-col gap-2">
        <KvitButton variant="danger" className="bg-field" disabled={isSending || isDiscarding} onClick={discard}>
          {isSending ? t('outbox.sending') : t('outbox.discard')}
        </KvitButton>
        <KvitButton variant="link" onClick={sheet.close}>
          {t('settlements.cancel')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
