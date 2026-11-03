import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { useLogDisplayProblem } from '../hooks/useLogDisplayProblem'

interface WaitingRowProps {
  title: string
  amountMinor?: number
  currency?: Currency
  detail?: string
  leading?: ReactNode
  showsRateNote?: boolean
  displayProblem?: string | null
  onOpen: () => void
}

export function WaitingRow({
  title,
  amountMinor,
  currency,
  detail,
  leading,
  showsRateNote = true,
  displayProblem = null,
  onOpen,
}: WaitingRowProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const notSynced = t('outbox.notSynced')
  useLogDisplayProblem(displayProblem)

  return (
    <button
      type="button"
      data-waiting-row
      onClick={onOpen}
      className="pressable flex min-h-16 w-full items-center rounded-2xl border-2 border-dashed border-field-border bg-card px-3 py-2 text-left text-card-foreground hover:bg-secondary-hover"
    >
      <span className="flex min-w-0 flex-1 items-center gap-3 opacity-80">
        {leading}
        <span className="flex min-w-30 flex-1 flex-col">
          <span className="line-clamp-2 font-semibold break-words italic">{title}</span>
          <span className="min-w-0 text-[0.8125rem] break-words">{detail === undefined ? notSynced : `${detail} · ${notSynced}`}</span>
          {showsRateNote && currency === 'EUR' && <span className="text-[0.8125rem] break-words">{t('outbox.rateWhenSent')}</span>}
        </span>
        {amountMinor !== undefined && currency !== undefined && (
          <span className="text-right font-bold">{formatMoney(amountMinor, currency, language)}</span>
        )}
      </span>
    </button>
  )
}
