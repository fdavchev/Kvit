import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import type {
  MonthLimits,
  PersonalEntry,
  PersonalEntryKind,
} from '@/core/services/personalEntries/personalEntriesService'
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
import { usePersonalCategories } from '../../categories/hooks/usePersonalCategories'
import { convertedTotalsInSetting } from '../../convertedTotals'
import { PossibleDuplicateCard } from '../../duplicates/components/PossibleDuplicateCard'
import { duplicatePairKey, entryOfPair, mergedEntryLabel } from '../../duplicates/possibleDuplicates'
import { PersonalIncomeSheet } from '../../entries/components/PersonalIncomeSheet'
import { PersonalSpendingSheet } from '../../entries/components/PersonalSpendingSheet'
import { usePersonalEntries } from '../../entries/hooks/usePersonalEntries'
import { personalEntriesQueryKey } from '../../entries/personalEntryCache'
import type { LimitTarget } from '../../limits/budgetLimits'
import { BudgetLimitChoiceSheet } from '../../limits/components/BudgetLimitChoiceSheet'
import { BudgetLimits, type LimitFocusRequest } from '../../limits/components/BudgetLimits'
import { BudgetLimitSheet } from '../../limits/components/BudgetLimitSheet'
import { RecurringIncomeListSheet } from '../../recurring/components/RecurringIncomeListSheet'
import { RecurringIncomeRow, RecurringIncomeRowError } from '../../recurring/components/RecurringIncomeRow'
import { useCatchUpOnOpen } from '../../recurring/hooks/useCatchUpOnOpen'
import { useRecurringIncomes } from '../../recurring/hooks/useRecurringIncomes'
import { liveRecurringIncome } from '../../recurring/recurringIncomeCache'
import { BudgetSummaryRow } from '../../summary/components/BudgetSummaryRow'
import { BudgetAddChoiceSheet } from './BudgetAddChoiceSheet'
import { BudgetFloatingAddButton, BudgetRoundAddButton } from './BudgetAddButton'
import { spendingWithGroupShares } from '../groupShareTotals'
import { BudgetConvertedTotalCard, BudgetTotalCard } from './BudgetTotalCard'
import { CategoryTotals } from './CategoryTotals'
import { GroupSharesBlock } from './GroupSharesBlock'
import { MonthStrip } from './MonthStrip'
import { PersonalEntryListItem } from './PersonalEntryListItem'

const receiptEmoji = '\u{1F9FE}'

type AddingStep = 'choice' | PersonalEntryKind

const mostIncomesInSheet = 2

interface ClosedLimitSheet extends LimitFocusRequest {
  monthUpdatedAt: number
}

interface AnsweredPair {
  pairKey: string
}

export function BudgetScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const isDesktop = useIsDesktop()
  const [currentMonth] = useState<string>(() => currentMonthInTimeZone(me.timeZone, new Date()))
  const [searchParams, setSearchParams] = useSearchParams()
  const months = budgetMonths(currentMonth)
  const monthInAddress = searchParams.get('month')
  const selectedMonth = monthInAddress !== null && months.includes(monthInAddress) ? monthInAddress : currentMonth
  const [addingStep, setAddingStep] = useState<AddingStep | null>(null)
  const [openedEntry, setOpenedEntry] = useState<PersonalEntry | null>(null)
  const [isRepeatingListOpen, setIsRepeatingListOpen] = useState<boolean>(false)
  const [openedRecurringIncome, setOpenedRecurringIncome] = useState<RecurringIncome | null>(null)
  const [handOnOpener, setHandOnOpener] = useState<Element | null>(null)
  const [limitsToChooseFrom, setLimitsToChooseFrom] = useState<MonthLimits | null>(null)
  const [openedLimit, setOpenedLimit] = useState<LimitTarget | null>(null)
  const [closedLimitSheet, setClosedLimitSheet] = useState<ClosedLimitSheet | null>(null)
  const [answeredPair, setAnsweredPair] = useState<AnsweredPair | null>(null)
  const focusedAfterPairRef = useRef<AnsweredPair | null>(null)
  const titleRef = useRef<HTMLHeadingElement>(null)
  const nextPairButtonRef = useRef<HTMLButtonElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const entriesQuery = usePersonalEntries(selectedMonth)
  const categoriesQuery = usePersonalCategories()
  const recurringQuery = useRecurringIncomes()
  const entriesRef = useListAnimation<HTMLUListElement>()
  useCatchUpOnOpen()
  const today = todayInTimeZone(me.timeZone, new Date())
  const shownPair = entriesQuery.data?.possibleDuplicates[0]
  const shownPairKey = shownPair === undefined ? null : duplicatePairKey(shownPair)

  useEffect(() => {
    if (answeredPair === null || answeredPair === focusedAfterPairRef.current || answeredPair.pairKey === shownPairKey) {
      return
    }
    focusedAfterPairRef.current = answeredPair
    const focusTarget = nextPairButtonRef.current ?? titleRef.current
    focusTarget?.focus()
  }, [answeredPair, shownPairKey])

  function selectMonth(month: string): void {
    setAnsweredPair(null)
    setSearchParams({ month }, { replace: true })
  }

  function startAdding(): void {
    setHandOnOpener(document.activeElement)
    setAddingStep('choice')
  }

  function openRepeating(incomes: readonly RecurringIncome[]): void {
    if (incomes.length > 0 && incomes.length <= mostIncomesInSheet) {
      setHandOnOpener(document.activeElement)
      setIsRepeatingListOpen(true)
      return
    }
    void navigate(routes.budgetRepeating)
  }

  function closeRepeatingList(editedIncome: RecurringIncome | null): void {
    setIsRepeatingListOpen(false)
    setOpenedRecurringIncome(editedIncome)
  }

  function startSettingLimit(limits: MonthLimits): void {
    setHandOnOpener(document.activeElement)
    setLimitsToChooseFrom(limits)
  }

  function openLimit(target: LimitTarget): void {
    setHandOnOpener(document.activeElement)
    setOpenedLimit(target)
  }

  function closeLimitChoice(chosen: LimitTarget | null): void {
    setLimitsToChooseFrom(null)
    setOpenedLimit(chosen)
  }

  function closeLimit(target: LimitTarget): void {
    const monthUpdatedAt = queryClient.getQueryState(personalEntriesQueryKey(selectedMonth))?.dataUpdatedAt ?? 0
    setOpenedLimit(null)
    setClosedLimitSheet({ target, opener: handOnOpener, monthUpdatedAt })
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
    function renderLoadError(error: Error, onRetry: () => void) {
      return (
        <>
          {repeatingRow}
          <KvitError message={t(errorMessageKey(error))} onRetry={onRetry} />
        </>
      )
    }

    if (entriesQuery.isPending) {
      return <BudgetMonthSkeleton />
    }
    if (entriesQuery.isError) {
      return renderLoadError(entriesQuery.error, () => void entriesQuery.refetch())
    }
    const { entries, totals, incomeTotals, groupShares, convertedTotals, limits, possibleDuplicates } =
      entriesQuery.data
    const sharesNeedCategories = groupShares.some((share) => share.categoryId !== null)
    if (sharesNeedCategories && categoriesQuery.isPending) {
      return <BudgetMonthSkeleton />
    }
    if (sharesNeedCategories && categoriesQuery.isError) {
      return renderLoadError(categoriesQuery.error, () => void categoriesQuery.refetch())
    }
    const categories = categoriesQuery.data ?? []
    const isClosedSheetMonthShown =
      closedLimitSheet !== null && entriesQuery.dataUpdatedAt >= closedLimitSheet.monthUpdatedAt
    const limitsPart = (
      <BudgetLimits
        limits={limits}
        focusRequest={isClosedSheetMonthShown ? closedLimitSheet : null}
        onOpenLimit={openLimit}
        onSetLimit={() => startSettingLimit(limits)}
        onFocusHandled={() => setClosedLimitSheet(null)}
      />
    )
    const summaryRow = <BudgetSummaryRow month={selectedMonth} />
    const firstPair = possibleDuplicates[0]
    const duplicateCard = firstPair !== undefined && (
      <PossibleDuplicateCard
        key={duplicatePairKey(firstPair)}
        pair={firstPair}
        entry={entryOfPair(firstPair, entries)}
        today={today}
        sameButtonRef={nextPairButtonRef}
        onPairDone={() => setAnsweredPair({ pairKey: duplicatePairKey(firstPair) })}
      />
    )
    if (entries.length === 0 && groupShares.length === 0) {
      return (
        <>
          {duplicateCard}
          {repeatingRow}
          {summaryRow}
          {limitsPart}
          <div className="pt-6">
            <KvitEmpty emoji={receiptEmoji} title={t('budget.screen.emptyTitle')} message={t('expenses.emptyHint')} />
          </div>
        </>
      )
    }
    const spending = spendingWithGroupShares(totals, groupShares, categories)
    const tileTotals = spending.filter((total) => total.categories.length > 0)
    const converted = convertedTotalsInSetting(convertedTotals, me.budgetTotalCurrency)
    return (
      <>
        {duplicateCard}
        {spending.length === 0 && incomeTotals.length === 0 ? (
          <>
            <p className={cn(balancesCardLook, 'mt-5 py-4 text-center font-semibold text-muted-foreground')}>
              {t('budget.screen.emptyTitle')}
            </p>
            {repeatingRow}
            {summaryRow}
            {limitsPart}
          </>
        ) : (
          <>
            {converted === null ? (
              <BudgetTotalCard totals={spending} incomeTotals={incomeTotals} />
            ) : (
              <BudgetConvertedTotalCard
                converted={converted}
                hasConvertedItems={
                  entries.some((entry) => entry.currency !== converted.currency) ||
                  groupShares.some((share) => share.currency !== converted.currency)
                }
              />
            )}
            {repeatingRow}
            {summaryRow}
            {limitsPart}
            {tileTotals.length > 0 && <CategoryTotals totals={tileTotals} />}
          </>
        )}
        {entries.length > 0 && (
          <ul
            ref={entriesRef}
            aria-label={t('budget.screen.entries')}
            className="mt-5 flex flex-col gap-0.5 rounded-[20px] bg-card p-1.5 text-card-foreground"
          >
            {entries.map((entry) => (
              <PersonalEntryListItem
                key={entry.id}
                entry={entry}
                today={today}
                mergedLabel={mergedEntryLabel(entry, groupShares, categories, t)}
                onOpen={setOpenedEntry}
              />
            ))}
          </ul>
        )}
        {groupShares.length > 0 && (
          <GroupSharesBlock groupShares={groupShares} categories={categories} today={today} />
        )}
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="min-h-12" />
      <KvitScreenTitle ref={titleRef}>{t('nav.budget')}</KvitScreenTitle>
      <MonthStrip
        months={months}
        currentMonth={currentMonth}
        selectedMonth={selectedMonth}
        onSelect={selectMonth}
      />
      {isDesktop && <BudgetRoundAddButton onClick={startAdding} />}
      {renderMonth(renderRepeatingRow())}
      {!isDesktop && <BudgetFloatingAddButton onClick={startAdding} />}
      {addingStep === 'choice' && <BudgetAddChoiceSheet onClose={setAddingStep} />}
      {addingStep === 'Spending' && <PersonalSpendingSheet opener={handOnOpener} onClose={() => setAddingStep(null)} />}
      {addingStep === 'Income' && <PersonalIncomeSheet opener={handOnOpener} onClose={() => setAddingStep(null)} />}
      {isRepeatingListOpen && recurringQuery.data !== undefined && (
        <RecurringIncomeListSheet incomes={recurringQuery.data} today={today} onClose={closeRepeatingList} />
      )}
      {openedRecurringIncome !== null && (
        <PersonalIncomeSheet
          key={openedRecurringIncome.id}
          recurringIncome={liveRecurringIncome(openedRecurringIncome, recurringQuery.data)}
          opener={handOnOpener}
          onClose={() => setOpenedRecurringIncome(null)}
        />
      )}
      {limitsToChooseFrom !== null && <BudgetLimitChoiceSheet limits={limitsToChooseFrom} onClose={closeLimitChoice} />}
      {openedLimit !== null && (
        <BudgetLimitSheet
          target={openedLimit}
          viewedMonth={selectedMonth}
          currentMonth={currentMonth}
          opener={handOnOpener}
          onClose={() => closeLimit(openedLimit)}
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

function BudgetMonthSkeleton() {
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
