import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { currentMonthInTimeZone, recentMonths } from '@/shared/utils/calendarMonth'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { PersonalSpendingSheet } from '../../entries/components/PersonalSpendingSheet'
import { usePersonalEntries } from '../../entries/hooks/usePersonalEntries'
import { BudgetFloatingAddButton, BudgetRoundAddButton } from './BudgetAddButton'
import { BudgetTotalCard } from './BudgetTotalCard'
import { CategoryTotals } from './CategoryTotals'
import { MonthStrip } from './MonthStrip'
import { PersonalEntryListItem } from './PersonalEntryListItem'

const shownMonthCount = 24
const receiptEmoji = '\u{1F9FE}'

export function BudgetScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const isDesktop = useIsDesktop()
  const [currentMonth] = useState<string>(() => currentMonthInTimeZone(me.timeZone, new Date()))
  const [selectedMonth, setSelectedMonth] = useState<string>(currentMonth)
  const [isAdding, setIsAdding] = useState<boolean>(false)
  const [openedEntry, setOpenedEntry] = useState<PersonalEntry | null>(null)
  const entriesQuery = usePersonalEntries(selectedMonth)
  const entriesRef = useListAnimation<HTMLUListElement>()

  function renderMonth() {
    if (entriesQuery.isPending) {
      return (
        <KvitLoading>
          <KvitSkeleton className="mt-5 h-28 rounded-[20px]" />
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
        <KvitError
          message={t(errorMessageKey(entriesQuery.error))}
          onRetry={() => void entriesQuery.refetch()}
        />
      )
    }
    const { entries, totals } = entriesQuery.data
    if (entries.length === 0) {
      return (
        <div className="pt-6">
          <KvitEmpty emoji={receiptEmoji} title={t('budget.screen.emptyTitle')} message={t('expenses.emptyHint')} />
        </div>
      )
    }
    const today = todayInTimeZone(me.timeZone, new Date())
    return (
      <>
        <BudgetTotalCard totals={totals} />
        <CategoryTotals totals={totals} />
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
        months={recentMonths(currentMonth, shownMonthCount)}
        currentMonth={currentMonth}
        selectedMonth={selectedMonth}
        onSelect={setSelectedMonth}
      />
      {isDesktop && <BudgetRoundAddButton onClick={() => setIsAdding(true)} />}
      {renderMonth()}
      {!isDesktop && <BudgetFloatingAddButton onClick={() => setIsAdding(true)} />}
      {isAdding && <PersonalSpendingSheet onClose={() => setIsAdding(false)} />}
      {openedEntry !== null && (
        <PersonalSpendingSheet key={openedEntry.id} entry={openedEntry} onClose={() => setOpenedEntry(null)} />
      )}
    </KvitScreen>
  )
}
