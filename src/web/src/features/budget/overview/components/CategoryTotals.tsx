import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalCurrencyTotal } from '@/core/services/personalEntries/personalEntriesService'
import { kvitCategoryGridLook } from '@/shared/components/KvitCategoryChoice'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { currencyLabel, formatMoney } from '@/shared/utils/formatMoney'
import { personalCategoryName } from '../../categories/personalCategoryName'

interface CategoryTotalsProps {
  totals: readonly PersonalCurrencyTotal[]
}

export function CategoryTotals({ totals }: CategoryTotalsProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const headingId = useId()

  return (
    <section aria-labelledby={headingId} className="mt-5 flex flex-col gap-4">
      <h2 id={headingId} className="sr-only">
        {t('budget.screen.categoryTotals')}
      </h2>
      {totals.map((total) => (
        <CurrencyTotals
          key={total.currency}
          total={total}
          label={totals.length > 1 ? currencyLabel(total.currency, language) : null}
        />
      ))}
    </section>
  )
}

interface CurrencyTotalsProps {
  total: PersonalCurrencyTotal
  label: string | null
}

function CurrencyTotals({ total, label }: CurrencyTotalsProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const labelId = useId()

  const grid = (
    <ul className={kvitCategoryGridLook}>
      {total.categories.map((row) => (
        <li
          key={row.category.id}
          className="flex min-w-0 flex-col items-center gap-1 py-1 text-center text-xs leading-tight"
        >
          <KvitEmojiTile emoji={row.category.emoji} background={categoryColor(row.category)} />
          <span className="w-full font-semibold break-words">{personalCategoryName(row.category, t)}</span>
          <span className="w-full font-bold break-words">{formatMoney(row.totalMinor, total.currency, language)}</span>
        </li>
      ))}
    </ul>
  )

  if (label === null) {
    return grid
  }
  return (
    <div role="group" aria-labelledby={labelId} className="flex flex-col gap-1.5">
      <p id={labelId} className="mx-1 text-[0.8125rem] font-bold text-muted-foreground">
        {label}
      </p>
      {grid}
    </div>
  )
}
