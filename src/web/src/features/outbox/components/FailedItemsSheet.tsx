import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { useOutboxItems, useOutboxSender } from '@/core/outbox/useOutbox'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { formatMoney } from '@/shared/utils/formatMoney'
import { failureMessageKey } from '../failureMessageKey'
import { amountOfItem, failureOf, titleWithoutCategoryOf } from '../waitingItemDisplay'

interface FailedItemsSheetProps {
  onClose: () => void
}

export function FailedItemsSheet({ onClose }: FailedItemsSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const sender = useOutboxSender()
  const failedItems = useOutboxItems((item) => item.state === 'failed')
  const sheet = useKvitSheet()
  const hasFailedItems = failedItems.length > 0
  const { closeIfShown } = sheet

  useEffect(() => {
    if (!hasFailedItems) {
      closeIfShown()
    }
  }, [hasFailedItems, closeIfShown])

  function showFailure(action: string, item: OutboxItem, error: unknown): void {
    console.error(`Could not ${action} the failed ${item.kind} ${item.id}`, error)
    toast.error(t('errors.generic'))
  }

  function retry(item: OutboxItem): void {
    sender.retry(item.id).catch((error: unknown) => showFailure('retry', item, error))
  }

  function discard(item: OutboxItem): void {
    sender.discard(item.id).catch((error: unknown) => showFailure('discard', item, error))
  }

  return (
    <KvitSheet title={t('outbox.failedSheetTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <ul className="flex flex-col gap-3">
        {failedItems.map((item) => {
          const { amountMinor, currency } = amountOfItem(item)
          return (
            <li key={item.id} className="flex flex-col gap-2 border-t border-field-border pt-3 first:border-t-0 first:pt-0">
              <div className="flex items-start gap-3">
                <span className="min-w-0 flex-1 font-semibold break-words">{titleWithoutCategoryOf(item, t)}</span>
                <span className="font-bold whitespace-nowrap">{formatMoney(amountMinor, currency, language)}</span>
              </div>
              <p className="text-[0.875rem] text-pretty text-danger">{t(failureMessageKey(failureOf(item)))}</p>
              <div className="grid grid-cols-2 gap-2">
                <KvitButton variant="secondary" className="bg-field" onClick={() => discard(item)}>
                  {t('outbox.discard')}
                </KvitButton>
                <KvitButton variant="secondary" className="bg-field" onClick={() => retry(item)}>
                  {t('outbox.retry')}
                </KvitButton>
              </div>
            </li>
          )
        })}
      </ul>
    </KvitSheet>
  )
}
