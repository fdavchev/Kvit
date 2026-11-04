import type { TFunction } from 'i18next'
import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Language } from '@/core/i18n/language'
import { pluralForm } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { AdminStats, AdminStatsAllTime, AdminStatsGrouping } from '@/core/services/adminStats/adminStatsService'
import { summaryChangeOf, type SummaryDirection } from '@/features/budget/summary/summaryChange'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { formatMonthWithYear, monthOfDate } from '@/shared/utils/calendarMonth'
import { cn } from '@/shared/utils/cn'
import { formatDayWithoutYear } from '@/shared/utils/formatDate'
import { formatGroupedDecimal } from '@/shared/utils/formatNumber'

interface AdminStatsCardsProps {
  stats: AdminStats
  isUpdating: boolean
}

interface StatsCardNumbers {
  key: string
  title: string
  total: number
  previousTotal: number | null
  counts: readonly number[]
  lines: readonly string[]
}

interface Comparison {
  text: string
  direction: SummaryDirection
}

const smallestBarPercent = 4

const allTimeNouns: Record<keyof AdminStatsAllTime, string> = {
  accounts: 'account',
  guests: 'guest',
  groups: 'group',
  expenses: 'expense',
  payments: 'payment',
}

const comparisonLooks: Record<SummaryDirection, string> = {
  up: 'text-positive',
  new: 'text-positive',
  down: 'text-negative',
  same: 'text-muted-foreground',
}

export function AdminStatsCards({ stats, isUpdating }: AdminStatsCardsProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const axisLabels = firstAndLastLabels(stats.buckets, stats.by, language)

  return (
    <div className="flex flex-col gap-3">
      <p data-stats-all-time className={cn(balancesCardLook, 'text-[0.9375rem] text-pretty')}>
        {allTimeLine(stats.allTime, language, t)}
      </p>
      <div
        data-stats-cards
        aria-busy={isUpdating}
        className={cn(
          'grid grid-cols-1 gap-3 transition-opacity duration-200 motion-reduce:transition-none lg:grid-cols-2',
          isUpdating && 'opacity-60',
        )}
      >
        {cardNumbers(stats, language, t).map((card) => (
          <StatsCard
            key={card.key}
            title={card.title}
            totalText={formatCount(card.total, language)}
            comparison={comparisonOf(card.total, card.previousTotal, t)}
            lines={card.lines}
            bars={barsOf(card.counts, stats.buckets)}
            axisLabels={axisLabels}
          />
        ))}
      </div>
    </div>
  )
}

interface Bar {
  bucket: string
  percent: number
}

interface StatsCardProps {
  title: string
  totalText: string
  comparison: Comparison | null
  lines: readonly string[]
  bars: readonly Bar[]
  axisLabels: readonly string[]
}

function StatsCard({ title, totalText, comparison, lines, bars, axisLabels }: StatsCardProps) {
  const headingId = useId()

  return (
    <section aria-labelledby={headingId} className={cn(balancesCardLook, 'flex flex-col')}>
      <div className="flex flex-wrap items-baseline justify-between gap-x-2 gap-y-0.5">
        <h2 id={headingId} className="text-[0.9375rem] font-bold">
          {title}
        </h2>
        {comparison !== null && (
          <p data-stats-compare className={cn('text-[0.8125rem] font-semibold', comparisonLooks[comparison.direction])}>
            {comparison.text}
          </p>
        )}
      </div>
      <p data-stats-total className="text-[2rem] leading-tight font-extrabold tabular-nums">
        {totalText}
      </p>
      {lines.map((line) => (
        <p key={line} className="mt-0.5 text-[0.8125rem] text-pretty text-muted-foreground">
          {line}
        </p>
      ))}
      <div data-stats-bars aria-hidden="true" className="mt-2.5 flex h-14 items-end gap-0.5">
        {bars.map((bar) => (
          <span
            key={bar.bucket}
            className="block min-w-0.5 flex-1 rounded-t-[3px] bg-primary"
            style={{ height: `${bar.percent}%` }}
          />
        ))}
      </div>
      <div data-stats-axis className="mt-1 flex justify-between gap-2 text-[0.75rem] text-muted-foreground">
        {axisLabels.map((label) => (
          <span key={label}>{label}</span>
        ))}
      </div>
    </section>
  )
}

function cardNumbers(stats: AdminStats, language: Language, t: TFunction): StatsCardNumbers[] {
  const { signUps, activePeople } = stats
  const signUpLines = [
    t('adminStats.signUps.breakdown', {
      email: formatCount(signUps.email, language),
      google: formatCount(signUps.google, language),
      guest: formatCount(signUps.guest, language),
    }),
  ]
  if (signUps.guestsUpgraded > 0) {
    signUpLines.push(countLine(t, 'adminStats.signUps.upgraded', signUps.guestsUpgraded, language))
  }
  const activeLines: string[] = []
  if (activePeople.guests > 0) {
    activeLines.push(countLine(t, 'adminStats.active.guests', activePeople.guests, language))
  }
  if (stats.by !== 'day') {
    activeLines.push(countLine(t, 'adminStats.active.personDays', activePeople.personDays, language))
  }
  return [
    {
      key: 'signUps',
      title: t('adminStats.card.signUps'),
      total: signUps.total,
      previousTotal: signUps.previousTotal,
      counts: signUps.counts,
      lines: signUpLines,
    },
    {
      key: 'activePeople',
      title: t('adminStats.card.activePeople'),
      total: activePeople.people,
      previousTotal: activePeople.previousPeople,
      counts: activePeople.peopleCounts,
      lines: activeLines,
    },
    { key: 'joinedViaInvite', title: t('adminStats.card.joinedViaInvite'), ...stats.joinedViaInvite, lines: [] },
    { key: 'groupsCreated', title: t('adminStats.card.groupsCreated'), ...stats.groupsCreated, lines: [] },
    { key: 'expensesAdded', title: t('adminStats.card.expensesAdded'), ...stats.expensesAdded, lines: [] },
    { key: 'paymentsConfirmed', title: t('adminStats.card.paymentsConfirmed'), ...stats.paymentsConfirmed, lines: [] },
  ]
}

function comparisonOf(total: number, previousTotal: number | null, t: TFunction): Comparison | null {
  if (previousTotal === null || (total === 0 && previousTotal === 0)) {
    return null
  }
  const change = summaryChangeOf(total, previousTotal)
  if (change.direction === 'new') {
    return { text: t('adminStats.compare.new'), direction: 'new' }
  }
  if (change.direction === 'same' || change.percent === 0) {
    return { text: t('adminStats.compare.same'), direction: 'same' }
  }
  return { text: t(`adminStats.compare.${change.direction}`, { percent: String(change.percent) }), direction: change.direction }
}

function barsOf(counts: readonly number[], buckets: readonly string[]): Bar[] {
  const largest = Math.max(0, ...counts)
  return counts.map((count, index) => ({
    bucket: buckets[index],
    percent: largest === 0 ? smallestBarPercent : Math.max(smallestBarPercent, (count * 100) / largest),
  }))
}

function firstAndLastLabels(buckets: readonly string[], by: AdminStatsGrouping, language: Language): string[] {
  const labels = [buckets.at(0), buckets.at(-1)]
    .filter((bucket) => bucket !== undefined)
    .map((bucket) => bucketLabel(bucket, by, language))
  return [...new Set(labels)]
}

function bucketLabel(bucket: string, by: AdminStatsGrouping, language: Language): string {
  switch (by) {
    case 'day':
    case 'week':
      return formatDayWithoutYear(bucket, language)
    case 'month':
      return formatMonthWithYear(monthOfDate(bucket), language)
    case 'year':
      return bucket.slice(0, 4)
  }
}

function allTimeLine(allTime: AdminStatsAllTime, language: Language, t: TFunction): string {
  const countTexts = Object.fromEntries(
    (Object.keys(allTimeNouns) as (keyof AdminStatsAllTime)[]).map((name) => [
      name,
      t(`adminStats.count.${allTimeNouns[name]}_${pluralForm(allTime[name], language)}`, {
        replace: { count: formatCount(allTime[name], language) },
      }),
    ]),
  )
  return t('adminStats.allTime.line', countTexts)
}

function countLine(t: TFunction, key: string, count: number, language: Language): string {
  return t(key, { replace: { count: formatCount(count, language) } })
}

function formatCount(count: number, language: Language): string {
  return formatGroupedDecimal(String(count), language)
}
