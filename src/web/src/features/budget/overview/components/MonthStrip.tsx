import { useLayoutEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import { formatMonthLabel } from '@/shared/utils/calendarMonth'

interface MonthStripProps {
  months: readonly string[]
  currentMonth: string
  selectedMonth: string
  onSelect: (month: string) => void
}

export function MonthStrip({ months, currentMonth, selectedMonth, onSelect }: MonthStripProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const stripRef = useRef<HTMLDivElement>(null)

  useLayoutEffect(() => {
    const strip = stripRef.current
    if (strip === null) {
      return
    }
    strip.scrollLeft = strip.scrollWidth - strip.clientWidth
  }, [])

  return (
    <div
      ref={stripRef}
      role="group"
      aria-label={t('budget.screen.months')}
      className="flex gap-1 overflow-x-auto rounded-full bg-card p-1 shadow-[0_2px_10px_var(--track-shadow)]"
    >
      {months.map((month) => (
        <button
          key={month}
          type="button"
          aria-pressed={month === selectedMonth}
          onClick={() => onSelect(month)}
          className="pressable min-h-11 flex-none rounded-full px-3.5 text-[0.9375rem] font-semibold whitespace-nowrap text-muted-foreground hover:text-foreground aria-pressed:bg-tab-active aria-pressed:text-tab-active-foreground aria-pressed:shadow-[0_1px_3px_var(--track-shadow)]"
        >
          {formatMonthLabel(month, currentMonth, language)}
        </button>
      ))}
    </div>
  )
}
