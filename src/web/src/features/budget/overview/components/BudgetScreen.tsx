import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import type { PersonalEntry, PersonalEntryKind } from '@/core/services/personalEntries/personalEntriesService'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { currentMonthInTimeZone } from '@/shared/utils/calendarMonth'
import { cn } from '@/shared/utils/cn'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { budgetMonths } from '../../budgetMonths'
import { PersonalIncomeSheet } from '../../entries/components/PersonalIncomeSheet'
import { PersonalSpendingSheet } from '../../entries/components/PersonalSpendingSheet'
import { usePersonalEntries } from '../../entries/hooks/usePersonalEntries'
import { RecurringIncomeListSheet } from '../../recurring/components/RecurringIncomeListSheet'
import { RecurringIncomeRow, RecurringIncomeRowError } from '../../recurring/components/RecurringIncomeRow'
import { useCatchUpOnOpen } from '../../recurring/hooks/useCatchUpOnOpen'
import { useRecurringIncomes } from '../../recurring/hooks/useRecurringIncomes'
import { BudgetAddChoiceSheet } from './BudgetAddChoiceSheet'
import { BudgetFloatingAddButton, BudgetRoundAddButton } from './BudgetAddButton'
import { BudgetTotalCard } from './BudgetTotalCard'
import { CategoryTotals } from './CategoryTotals'
import { MonthStrip } from './MonthStrip'
import { PersonalEntryListItem } from './PersonalEntryListItem'

const receiptEmoji = '\u{1F9FE}'

type AddingStep = 'choice' | PersonalEntryKind

const mostIncomesInSheet = 2

export function BudgetScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const isDesktop = useIsDesktop()
  const [currentMonth] = useState<string>(() => currentMonthInTimeZone(me.timeZone, new Date()))
  const [selectedMonth, setSelectedMonth] = useState<string>(currentMonth)
  const [addingStep, setAddingStep] = useState<AddingStep | null>(null)
  const [openedEntry, setOpenedEntry] = useState<PersonalEntry | null>(null)
  const [isRepeatingListOpen, setIsRepeatingListOpen] = useState<boolean>(false)
  const [openedRecurringIncome, setOpenedRecurringIncome] = useState<RecurringIncome | null>(null)
  const navigate = useNavigate()
  const entriesQuery = usePersonalEntries(selectedMonth)
  const recurringQuery = useRecurringIncomes()
  const entriesRef = useListAnimation<HTMLUListElement>()
  useCatchUpOnOpen()
  const today = todayInTimeZone(me.timeZone, new Date())

  function openRepeating(incomes: readonly RecurringIncome[]): void {
    if (incomes.length > 0 && incomes.length <= mostIncomesInSheet) {
      setIsRepeatingListOpen(true)
      return
    }
    void navigate(routes.budgetRepeating)
  }

  function closeRepeatingList(editedIncome: RecurringIncome | null): void {
    setIsRepeatingListOpen(false)
    setOpenedRecurringIncome(editedIncome)
  }

  function renderRepeatingRow(): ReactNode {
    if (recurringQuery.isPending) {
      return null
    }
    if (recurringQuery.isError) {
      return <RecurringIncomeRowError onRetry={() => void recurringQuery.refetch()} />
    }
    const incomes = recurringQuery.data
    return <RecurringIncomeRow incomes={incomes} onOpen={() => openRepeating(incomes)} />
  }

  function renderMonth(repeatingRow: ReactNode) {
    if (entriesQuery.isPending) {
      return (
        <KvitLoading>
          <KvitSkeleton className="mt-5 h-28 rounded-[20px]" />
          <KvitSkeleton className="mt-3 h-16 rounded-[20px]" />
          <div className="mt-5 grid grid-cols-4 gap-x-1.5">
            <KvitSkeleton className="h-20 rounded-[14px]" />
            <KvitSkeleton className="h-20 rounded-[14px]" />
            <KvitSkeleton className="h-20 rounded-[14px]" />
            <KvitSkeleton className="h-20 rounded-[14px]" />
          </div>
          <div className="mt-5 flex flex-col gap-2">
            <KvitSkeleton className="h-16" />
            <KvitSkeleton className="h-16" />
          </div>
        </KvitLoading>
      )
    }
    if (entriesQuery.isError) {
      return (
        <>
          {repeatingRow}
          <KvitError
            message={t(errorMessageKey(entriesQuery.error))}
            onRetry={() => void entriesQuery.refetch()}
          />
        </>
      )
    }
    const { entries, totals } = entriesQuery.data
    if (entries.length === 0) {
      return (
        <>
          {repeatingRow}
          <div className="pt-6">
            <KvitEmpty emoji={receiptEmoji} title={t('budget.screen.emptyTitle')} message={t('expenses.emptyHint')} />
          </div>
        </>
      )
    }
    return (
      <>
        {totals.length === 0 ? (
          <>
            <p className={cn(balancesCardLook, 'mt-5 py-4 text-center font-semibold text-muted-foreground')}>
              {t('budget.screen.emptyTitle')}
            </p>
            {repeatingRow}
          </>
        ) : (
          <>
            <BudgetTotalCard totals={totals} />
            {repeatingRow}
            <CategoryTotals totals={totals} />
          </>
        )}
        <ul
          ref={entriesRef}
          aria-label={t('budget.screen.entries')}
          className="mt-5 flex flex-col gap-0.5 rounded-[20px] bg-card p-1.5 text-card-foreground"
        >
          {entries.map((entry) => (
            <PersonalEntryListItem key={entry.id} entry={entry} today={today} onOpen={setOpenedEntry} />
          ))}
        </ul>
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="min-h-12" />
      <KvitScreenTitle>{t('nav.budget')}</KvitScreenTitle>
      <MonthStrip
        months={budgetMonths(currentMonth)}
        currentMonth={currentMonth}
        selectedMonth={selectedMonth}
        onSelect={setSelectedMonth}
      />
      {isDesktop && <BudgetRoundAddButton onClick={() => setAddingStep('choice')} />}
      {renderMonth(renderRepeatingRow())}
      {!isDesktop && <BudgetFloatingAddButton onClick={() => setAddingStep('choice')} />}
      {addingStep === 'choice' && <BudgetAddChoiceSheet onClose={setAddingStep} />}
      {addingStep === 'Spending' && <PersonalSpendingSheet onClose={() => setAddingStep(null)} />}
      {addingStep === 'Income' && <PersonalIncomeSheet onClose={() => setAddingStep(null)} />}
      {isRepeatingListOpen && recurringQuery.data !== undefined && (
        <RecurringIncomeListSheet incomes={recurringQuery.data} today={today} onClose={closeRepeatingList} />
      )}
      {openedRecurringIncome !== null && (
        <PersonalIncomeSheet
          key={openedRecurringIncome.id}
          recurringIncome={openedRecurringIncome}
          onClose={() => setOpenedRecurringIncome(null)}
        />
      )}
      {openedEntry !== null &&
        (openedEntry.kind === 'Income' ? (
          <PersonalIncomeSheet key={openedEntry.id} entry={openedEntry} onClose={() => setOpenedEntry(null)} />
        ) : (
          <PersonalSpendingSheet key={openedEntry.id} entry={openedEntry} onClose={() => setOpenedEntry(null)} />
        ))}
    </KvitScreen>
  )
}
