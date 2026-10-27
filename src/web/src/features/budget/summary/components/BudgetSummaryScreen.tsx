import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type {
  SummaryConvertedTotals,
  SummaryCurrency,
} from '@/core/services/personalSummary/personalSummaryService'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { addMonths, currentMonthInTimeZone, formatMonthLabel, isCalendarMonth } from '@/shared/utils/calendarMonth'
import { usePersonalCategoriesWithArchived } from '../../categories/hooks/usePersonalCategoriesWithArchived'
import { convertedTotalsInSetting } from '../../convertedTotals'
import { usePersonalSummary } from '../hooks/usePersonalSummary'
import { SummaryCards } from './SummaryCards'

const chartEmoji = '\u{1F4CA}'
const monthRefusedCode = 'PERSONAL_ENTRY_MONTH_INVALID'

export function BudgetSummaryScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const [searchParams] = useSearchParams()
  const [currentMonth] = useState<string>(() => currentMonthInTimeZone(me.timeZone, new Date()))
  const month = searchParams.get('month') ?? currentMonth

  if (!isCalendarMonth(month)) {
    return (
      <KvitScreen>
        <div className="flex min-h-12">
          <KvitBackButton to={routes.budget} />
        </div>
        <KvitScreenTitle>{t('budget.summary.row')}</KvitScreenTitle>
        <RefusedMonthError message={t(`errors.${monthRefusedCode}`)} />
      </KvitScreen>
    )
  }
  return <MonthSummary month={month} currentMonth={currentMonth} />
}

interface MonthSummaryProps {
  month: string
  currentMonth: string
}

function MonthSummary({ month, currentMonth }: MonthSummaryProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const previousMonth = addMonths(month, -1)
  const summaryQuery = usePersonalSummary(month)

  function renderContent() {
    if (summaryQuery.isPending) {
      return <SummarySkeleton />
    }
    if (summaryQuery.isError) {
      const message = t(errorMessageKey(summaryQuery.error))
      if (hasErrorCode(summaryQuery.error, monthRefusedCode)) {
        return <RefusedMonthError message={message} />
      }
      return <KvitError message={message} onRetry={() => void summaryQuery.refetch()} />
    }
    const { currencies, convertedTotals } = summaryQuery.data
    if (currencies.length === 0) {
      return (
        <div className="flex flex-col items-center">
          <KvitEmpty emoji={chartEmoji} message={t('budget.summary.empty')} />
          <KvitLinkButton to={routes.budget} variant="secondary">
            {t('budget.summary.toBudget')}
          </KvitLinkButton>
        </div>
      )
    }
    const converted = convertedTotalsInSetting(convertedTotals, me.budgetTotalCurrency)
    if (currencies.every((row) => row.categories.length === 0)) {
      return <SummaryCards currencies={currencies} converted={converted} categories={[]} />
    }
    return <SummaryCardsWithCategoryNames currencies={currencies} converted={converted} />
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.budgetOf(month)} />
      </div>
      <KvitScreenTitle>
        {t('budget.summary.title', {
          month: formatMonthLabel(month, currentMonth, language),
          previousMonth: formatMonthLabel(previousMonth, currentMonth, language),
        })}
      </KvitScreenTitle>
      {renderContent()}
    </KvitScreen>
  )
}

interface RefusedMonthErrorProps {
  message: string
}

function RefusedMonthError({ message }: RefusedMonthErrorProps) {
  const { t } = useTranslation()

  return (
    <KvitError
      message={message}
      action={
        <KvitLinkButton to={routes.budget} variant="secondary">
          {t('budget.summary.toBudget')}
        </KvitLinkButton>
      }
    />
  )
}

interface SummaryCardsWithCategoryNamesProps {
  currencies: readonly SummaryCurrency[]
  converted: SummaryConvertedTotals | null
}

function SummaryCardsWithCategoryNames({ currencies, converted }: SummaryCardsWithCategoryNamesProps) {
  const { t } = useTranslation()
  const categoriesQuery = usePersonalCategoriesWithArchived()

  if (categoriesQuery.isPending) {
    return <SummarySkeleton />
  }
  if (categoriesQuery.isError) {
    return (
      <KvitError message={t(errorMessageKey(categoriesQuery.error))} onRetry={() => void categoriesQuery.refetch()} />
    )
  }
  return <SummaryCards currencies={currencies} converted={converted} categories={categoriesQuery.data} />
}

function SummarySkeleton() {
  return (
    <KvitLoading>
      <div className="flex flex-col gap-3">
        <KvitSkeleton className="h-36 rounded-[20px]" />
        <KvitSkeleton className="h-56 rounded-[20px]" />
        <KvitSkeleton className="h-36 rounded-[20px]" />
      </div>
    </KvitLoading>
  )
}
