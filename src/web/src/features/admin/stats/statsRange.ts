import type { AdminStatsGrouping } from '@/core/services/adminStats/adminStatsService'
import { addMonths, monthOfDate } from '@/shared/utils/calendarMonth'
import { addDays, isCalendarDate } from '@/shared/utils/formatDate'

export const statsTimeZone = 'Europe/Skopje'

export const statsRangeChoices = ['days7', 'days30', 'thisMonth', 'months12', 'all', 'custom'] as const

export type StatsRangeChoice = (typeof statsRangeChoices)[number]

export interface StatsDates {
  from: string
  to: string
}

const longestDayGroupingDays = 92
const dayMs = 24 * 60 * 60 * 1000

export function thisMonthDates(today: string): StatsDates {
  return { from: `${monthOfDate(today)}-01`, to: today }
}

export function presetDates(range: Exclude<StatsRangeChoice, 'custom'>, today: string): StatsDates | null {
  switch (range) {
    case 'days7':
      return { from: addDays(today, -6), to: today }
    case 'days30':
      return { from: addDays(today, -29), to: today }
    case 'thisMonth':
      return thisMonthDates(today)
    case 'months12':
      return { from: `${addMonths(monthOfDate(today), -11)}-01`, to: today }
    case 'all':
      return null
  }
}

export function allowsDayGrouping(dates: StatsDates | null): boolean {
  return dates !== null && daysInRange(dates) <= longestDayGroupingDays
}

export function usualGrouping(range: StatsRangeChoice, dates: StatsDates | null): AdminStatsGrouping {
  if (range === 'months12' || range === 'all') {
    return 'month'
  }
  return allowsDayGrouping(dates) ? 'day' : 'week'
}

export function isUsableCustomRange(dates: StatsDates, today: string): boolean {
  return isCalendarDate(dates.from) && isCalendarDate(dates.to) && dates.from <= dates.to && dates.to <= today
}

function daysInRange({ from, to }: StatsDates): number {
  return (Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / dayMs + 1
}
