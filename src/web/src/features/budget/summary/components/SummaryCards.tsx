import { useId, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type {
  SummaryCategory,
  SummaryConvertedTotals,
  SummaryCurrency,
} from '@/core/services/personalSummary/personalSummaryService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { cn } from '@/shared/utils/cn'
import { currencyLabel, moneyParts, type Currency } from '@/shared/utils/formatMoney'
import { signedMoneyParts } from '@/shared/utils/formatSignedMoney'
import { personalCategoryName } from '../../categories/personalCategoryName'
import { summaryChangeOf, type SummaryChange, type SummaryDirection } from '../summaryChange'

type TotalKind = 'spending' | 'income'

interface SummaryCardsProps {
  currencies: readonly SummaryCurrency[]
  converted: SummaryConvertedTotals | null
  categories: readonly PersonalCategory[]
}

const inCurrencyKeys: Record<Currency, string> = { MKD: 'budget.card.inMkd', EUR: 'budget.card.inEur' }
const includesConvertedKeys: Record<Currency, string> = {
  MKD: 'budget.card.includesConvertedEuro',
  EUR: 'budget.card.includesConvertedDenars',
}
const arrows: Record<'up' | 'down' | 'new', string> = { up: '\u{25B2}', down: '\u{25BC}', new: '\u{25B2}' }
const smallLook = 'text-[0.8125rem]'

export function SummaryCards({ currencies, converted, categories }: SummaryCardsProps) {
  return (
    <div className="flex flex-col gap-3">
      {converted !== null && (
        <ConvertedCard
          converted={converted}
          hasConvertedItems={currencies.some((row) => row.currency !== converted.currency)}
        />
      )}
      {currencies.map((row) => (
        <CurrencyCard key={row.currency} row={row} categories={categories} />
      ))}
    </div>
  )
}

interface SummaryCardProps {
  heading: string
  children: ReactNode
}

function SummaryCard({ heading, children }: SummaryCardProps) {
  const headingId = useId()

  return (
    <section aria-labelledby={headingId} className={cn(balancesCardLook, 'flex flex-col')}>
      <h2 id={headingId} className="text-[0.8125rem] font-bold text-muted-foreground">
        {heading}
      </h2>
      {children}
    </section>
  )
}

interface ConvertedCardProps {
  converted: SummaryConvertedTotals
  hasConvertedItems: boolean
}

function ConvertedCard({ converted, hasConvertedItems }: ConvertedCardProps) {
  const { t } = useTranslation()

  return (
    <SummaryCard heading={t(inCurrencyKeys[converted.currency])}>
      <Totals
        currency={converted.currency}
        spendingThisMinor={converted.spendingThisMinor}
        spendingPreviousMinor={converted.spendingPreviousMinor}
        incomeThisMinor={converted.incomeThisMinor}
        incomePreviousMinor={converted.incomePreviousMinor}
      />
      {hasConvertedItems && (
        <p className={cn(smallLook, 'mt-1 text-muted-foreground')}>{t(includesConvertedKeys[converted.currency])}</p>
      )}
    </SummaryCard>
  )
}

interface CurrencyCardProps {
  row: SummaryCurrency
  categories: readonly PersonalCategory[]
}

function CurrencyCard({ row, categories }: CurrencyCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const rowsByThisMonth = [...row.categories].sort((first, second) => second.thisMinor - first.thisMinor)

  return (
    <SummaryCard heading={currencyLabel(row.currency, language)}>
      <Totals
        currency={row.currency}
        spendingThisMinor={row.spendingThisMinor}
        spendingPreviousMinor={row.spendingPreviousMinor}
        incomeThisMinor={row.incomeThisMinor}
        incomePreviousMinor={row.incomePreviousMinor}
      />
      {rowsByThisMonth.length > 0 && (
        <ul
          aria-label={t('budget.screen.categoryTotals')}
          className="mt-2 -mx-1 flex flex-col border-t border-field-border pt-2"
        >
          {rowsByThisMonth.map((categoryRow) => (
            <CategoryListItem
              key={categoryRow.categoryId}
              row={categoryRow}
              category={categoryOfRow(categoryRow, categories)}
              currency={row.currency}
            />
          ))}
        </ul>
      )}
    </SummaryCard>
  )
}

interface TotalsProps {
  currency: Currency
  spendingThisMinor: number
  spendingPreviousMinor: number
  incomeThisMinor: number
  incomePreviousMinor: number
}

function Totals({
  currency,
  spendingThisMinor,
  spendingPreviousMinor,
  incomeThisMinor,
  incomePreviousMinor,
}: TotalsProps) {
  const { t } = useTranslation()
  const hasSpending = spendingThisMinor > 0 || spendingPreviousMinor > 0
  const hasIncome = incomeThisMinor > 0 || incomePreviousMinor > 0

  return (
    <>
      {hasSpending && (
        <TotalRow
          kind="spending"
          label={t('budget.screen.spent')}
          currency={currency}
          thisMinor={spendingThisMinor}
          previousMinor={spendingPreviousMinor}
        />
      )}
      {hasIncome && (
        <TotalRow
          kind="income"
          label={t('budget.screen.income')}
          currency={currency}
          thisMinor={incomeThisMinor}
          previousMinor={incomePreviousMinor}
        />
      )}
    </>
  )
}

interface TotalRowProps {
  kind: TotalKind
  label: string
  currency: Currency
  thisMinor: number
  previousMinor: number
}

function TotalRow({ kind, label, currency, thisMinor, previousMinor }: TotalRowProps) {
  const { language } = useLanguage()
  const change = summaryChangeOf(thisMinor, previousMinor)
  const look = changeLook(change.direction, kind)

  return (
    <div data-summary-total={kind} className="flex flex-col gap-0.5 py-2">
      <div className="flex items-baseline gap-3">
        <span className="min-w-0 flex-1 font-semibold break-words">{label}</span>
        <AmountThisMonth className="text-xl leading-tight font-extrabold" currency={currency} amountMinor={thisMinor} />
      </div>
      <div className={cn(smallLook, 'flex flex-wrap items-baseline gap-x-3 gap-y-0.5')}>
        <ChangeText change={change} look={look} />
        {change.direction !== 'same' && (
          <span data-summary-difference className={cn('font-semibold whitespace-nowrap', look)}>
            <KvitAmount parts={signedMoneyParts(thisMinor - previousMinor, currency, language)} />
          </span>
        )}
        <AmountPreviousMonth currency={currency} amountMinor={previousMinor} />
      </div>
    </div>
  )
}

interface CategoryListItemProps {
  row: SummaryCategory
  category: PersonalCategory
  currency: Currency
}

function CategoryListItem({ row, category, currency }: CategoryListItemProps) {
  const { t } = useTranslation()
  const change = summaryChangeOf(row.thisMinor, row.previousMinor)

  return (
    <li className="flex flex-col gap-0.5 px-1 py-2">
      <div className="flex items-baseline gap-3">
        <span className="flex min-w-0 flex-1 items-baseline gap-2">
          <span data-summary-emoji aria-hidden="true" className="flex-none">
            {category.emoji}
          </span>
          <span data-summary-name className="min-w-0 font-semibold break-words">
            {personalCategoryName(category, t)}
          </span>
        </span>
        <AmountThisMonth className="font-bold" currency={currency} amountMinor={row.thisMinor} />
      </div>
      <div className={cn(smallLook, 'flex flex-wrap items-baseline gap-x-3 gap-y-0.5')}>
        <ChangeText change={change} look={changeLook(change.direction, 'spending')} />
        <AmountPreviousMonth currency={currency} amountMinor={row.previousMinor} />
      </div>
    </li>
  )
}

interface AmountProps {
  currency: Currency
  amountMinor: number
}

function AmountThisMonth({ currency, amountMinor, className }: AmountProps & { className: string }) {
  const { language } = useLanguage()

  return (
    <span data-summary-this className={cn('ml-auto text-right whitespace-nowrap', className)}>
      <KvitAmount parts={moneyParts(amountMinor, currency, language)} />
    </span>
  )
}

function AmountPreviousMonth({ currency, amountMinor }: AmountProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()

  return (
    <span data-summary-previous className="ml-auto whitespace-nowrap text-muted-foreground">
      {t('budget.summary.was')} <KvitAmount parts={moneyParts(amountMinor, currency, language)} />
    </span>
  )
}

interface ChangeTextProps {
  change: SummaryChange
  look: string
}

function ChangeText({ change, look }: ChangeTextProps) {
  const { t } = useTranslation()

  return (
    <span
      data-summary-change
      data-summary-direction={change.direction}
      className={cn('font-semibold whitespace-nowrap', look)}
    >
      {change.direction === 'same' && t('budget.summary.same')}
      {change.direction === 'new' && (
        <>
          <span aria-hidden="true">{`${arrows.new} `}</span>
          {t('budget.summary.new')}
        </>
      )}
      {(change.direction === 'up' || change.direction === 'down') && (
        <>
          <span aria-hidden="true">
            {`${arrows[change.direction]} ${t('budget.summary.percent', { percent: change.percent })}`}
          </span>
          <span className="sr-only">{t(`budget.summary.${change.direction}`, { percent: change.percent })}</span>
        </>
      )}
    </span>
  )
}

function changeLook(direction: SummaryDirection, kind: TotalKind): string {
  if (kind === 'income' || direction === 'same') {
    return 'text-muted-foreground'
  }
  return direction === 'down' ? 'text-positive' : 'text-negative'
}

function categoryOfRow(row: SummaryCategory, categories: readonly PersonalCategory[]): PersonalCategory {
  const category = categories.find((candidate) => candidate.id === row.categoryId)
  if (category === undefined) {
    throw new Error(`The category ${row.categoryId} of a summary row is not one of the personal categories`)
  }
  return category
}
