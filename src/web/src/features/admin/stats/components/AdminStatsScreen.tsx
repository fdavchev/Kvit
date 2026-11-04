import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { adminStatsGroupings, type AdminStatsGrouping } from '@/core/services/adminStats/adminStatsService'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { KvitError } from '@/shared/components/KvitError'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { useAdminStats } from '../hooks/useAdminStats'
import {
  allowsDayGrouping,
  earliestStatsDay,
  isUsableCustomRange,
  presetDates,
  statsRangeChoices,
  statsTimeZone,
  thisMonthDates,
  usualGrouping,
  type StatsDates,
  type StatsRangeChoice,
} from '../statsRange'
import { AdminStatsCards } from './AdminStatsCards'

const skeletonCardCount = 6
const noDisabledGroupings: readonly AdminStatsGrouping[] = []
const dayGroupingOnly: readonly AdminStatsGrouping[] = ['day']

export function AdminStatsScreen() {
  const { t } = useTranslation()
  const [today] = useState<string>(() => todayInTimeZone(statsTimeZone, new Date()))
  const [range, setRange] = useState<StatsRangeChoice>('thisMonth')
  const [grouping, setGrouping] = useState<AdminStatsGrouping>('day')
  const [customDraft, setCustomDraft] = useState<StatsDates>(() => thisMonthDates(today))
  const [customDates, setCustomDates] = useState<StatsDates>(() => thisMonthDates(today))
  const dates = range === 'custom' ? customDates : presetDates(range, today)
  const statsQuery = useAdminStats({ ...dates, by: grouping })

  function pickRange(nextRange: StatsRangeChoice): void {
    if (nextRange === 'custom') {
      const startDates = range === 'custom' ? customDates : datesOnScreen()
      setCustomDraft(startDates)
      setCustomDates(startDates)
      setRange(nextRange)
      setGrouping(usualGrouping(nextRange, startDates))
      return
    }
    setRange(nextRange)
    setGrouping(usualGrouping(nextRange, presetDates(nextRange, today)))
  }

  function datesOnScreen(): StatsDates {
    const shown = statsQuery.data
    return shown === undefined ? thisMonthDates(today) : { from: shown.from, to: shown.to }
  }

  function changeCustomDates(draft: StatsDates): void {
    setCustomDraft(draft)
    if (isUsableCustomRange(draft, today)) {
      setCustomDates(draft)
      setGrouping(usualGrouping('custom', draft))
    }
  }

  function renderContent() {
    if (statsQuery.isPending || (statsQuery.isError && statsQuery.isFetching)) {
      return <StatsSkeleton />
    }
    if (statsQuery.isError) {
      return <KvitError message={t(errorMessageKey(statsQuery.error))} onRetry={() => void statsQuery.refetch()} />
    }
    return <AdminStatsCards stats={statsQuery.data} isUpdating={statsQuery.isFetching} />
  }

  return (
    <KvitScreen width="wide">
      <KvitScreenTitle>{t('adminStats.title')}</KvitScreenTitle>
      <div className="flex flex-col gap-4 pb-6">
        <KvitChoiceChips<StatsRangeChoice>
          label={t('adminStats.range.label')}
          labelClassName="sr-only"
          options={statsRangeChoices.map((choice) => ({ value: choice, label: t(`adminStats.range.${choice}`) }))}
          value={range}
          onChange={pickRange}
        />
        {range === 'custom' && (
          <CustomDates draft={customDraft} today={today} onChange={changeCustomDates} />
        )}
        <KvitChoiceChips<AdminStatsGrouping>
          label={t('adminStats.by.label')}
          options={adminStatsGroupings.map((choice) => ({ value: choice, label: t(`adminStats.by.${choice}`) }))}
          value={grouping}
          onChange={setGrouping}
          disabledValues={allowsDayGrouping(dates) ? noDisabledGroupings : dayGroupingOnly}
        />
      </div>
      {renderContent()}
    </KvitScreen>
  )
}

interface CustomDatesProps {
  draft: StatsDates
  today: string
  onChange: (draft: StatsDates) => void
}

function CustomDates({ draft, today, onChange }: CustomDatesProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-3">
        <DateField
          label={t('adminStats.custom.from')}
          value={draft.from}
          min={earliestStatsDay}
          max={today}
          onChange={(from) => onChange({ ...draft, from })}
        />
        <DateField
          label={t('adminStats.custom.to')}
          value={draft.to}
          min={earliestStatsDay}
          max={today}
          onChange={(to) => onChange({ ...draft, to })}
        />
      </div>
      {!isUsableCustomRange(draft, today) && <KvitInlineError message={t('adminStats.custom.invalid')} />}
    </div>
  )
}

interface DateFieldProps {
  label: string
  value: string
  min: string
  max: string
  onChange: (value: string) => void
}

function DateField({ label, value, min, max, onChange }: DateFieldProps) {
  const fieldId = useId()

  return (
    <div className="flex min-w-0 flex-1 basis-36 flex-col">
      <label htmlFor={fieldId} className="mb-2 text-[0.9375rem] font-bold">
        {label}
      </label>
      <input
        id={fieldId}
        type="date"
        value={value}
        min={min}
        max={max}
        onChange={(event) => onChange(event.target.value)}
        className="min-h-12 w-full min-w-0 rounded-[14px] border-2 border-field-border-raised bg-field px-3 text-[1.0625rem] font-semibold text-field-foreground focus:border-field-border-focus focus-visible:outline-offset-0"
      />
    </div>
  )
}

function StatsSkeleton() {
  return (
    <KvitLoading>
      <div className="flex flex-col gap-3">
        <KvitSkeleton className="h-12 rounded-[20px]" />
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {Array.from({ length: skeletonCardCount }, (_unused, index) => (
            <KvitSkeleton key={index} className="h-48 rounded-[20px]" />
          ))}
        </div>
      </div>
    </KvitLoading>
  )
}
