import { useEffect, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useOutboxSummary } from '@/core/outbox/useOutbox'
import { KvitButton } from '@/shared/components/KvitButton'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { FailedItemsSheet } from './FailedItemsSheet'

const upToDateShownForMilliseconds = 3000

export function OutboxBar() {
  const { t } = useTranslation()
  const summary = useOutboxSummary()
  const [isReviewing, setIsReviewing] = useState<boolean>(false)
  const isUpToDate = summary.justFinished && summary.total === 0
  const [wasUpToDate, setWasUpToDate] = useState<boolean>(isUpToDate)
  const [hasUpToDateTimedOut, setHasUpToDateTimedOut] = useState<boolean>(false)
  if (isUpToDate !== wasUpToDate) {
    setWasUpToDate(isUpToDate)
    setHasUpToDateTimedOut(false)
  }

  useEffect(() => {
    if (!isUpToDate) {
      return
    }
    const timer = setTimeout(() => setHasUpToDateTimedOut(true), upToDateShownForMilliseconds)
    return () => clearTimeout(timer)
  }, [isUpToDate])

  function content(): ReactNode {
    if (summary.failed > 0) {
      return (
        <>
          <span className="min-w-0 flex-1">{t('outbox.failed', { count: summary.failed })}</span>
          <KvitButton variant="smallLink" className="w-auto flex-none px-2" onClick={() => setIsReviewing(true)}>
            {t('outbox.review')}
          </KvitButton>
        </>
      )
    }
    if (summary.waiting > 0) {
      return (
        <span className="min-w-0 flex-1">
          {t('outbox.waiting', { count: summary.waiting })} · {t('outbox.sending')}
        </span>
      )
    }
    if (isUpToDate && !hasUpToDateTimedOut) {
      return <span className="min-w-0 flex-1">{t('outbox.upToDate')}</span>
    }
    return null
  }

  const shownContent = content()

  return (
    <>
      {shownContent !== null && (
        <div
          data-outbox-bar
          role="status"
          className="pointer-events-none fixed inset-x-0 bottom-[max(calc(var(--bottom-bar-height)+4px),calc(env(safe-area-inset-bottom)+8px))] z-20 mx-auto w-full max-w-md px-4 motion-safe:animate-outbox-bar-in"
        >
          <div
            className={cn(
              'pointer-events-auto flex min-h-12 items-center gap-2 rounded-2xl px-4 py-1 text-[0.9375rem] leading-tight font-semibold',
              kvitGlassPanelLook,
            )}
          >
            {shownContent}
          </div>
        </div>
      )}
      {isReviewing && <FailedItemsSheet onClose={() => setIsReviewing(false)} />}
    </>
  )
}
