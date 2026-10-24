import type { TFunction } from 'i18next'
import type { Language } from '@/core/i18n/language'
import type {
  RecurringIncomeFields,
  RecurringIncomeFrequency,
} from '@/core/services/recurringIncomes/recurringIncomesService'
import { addMonthsToDate } from '@/shared/utils/calendarMonth'
import { addDays, dayOfMonthOfDate, formatDayOrdinal, weekdayOfDate } from '@/shared/utils/formatDate'

export type RecurringIncomeDays = Pick<RecurringIncomeFields, 'frequency' | 'dayOfMonth' | 'dayOfWeek'>

const weekdayKeys: readonly string[] = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
const shortestMonthDays = 28

export function recurringDaysFromStart(frequency: RecurringIncomeFrequency, startsOn: string): RecurringIncomeDays {
  return frequency === 'Monthly'
    ? { frequency, dayOfMonth: dayOfMonthOfDate(startsOn), dayOfWeek: null }
    : { frequency, dayOfMonth: null, dayOfWeek: weekdayOfDate(startsOn) }
}

export function endAfterMonths(startsOn: string, months: number): string {
  return addDays(addMonthsToDate(startsOn, months), -1)
}

export function recurringRhythmText(days: RecurringIncomeDays, language: Language, t: TFunction): string {
  if (days.frequency === 'Monthly') {
    if (days.dayOfMonth === null) {
      throw new Error('A Monthly repeating income has no day of the month')
    }
    const key = days.dayOfMonth > shortestMonthDays ? 'budget.recurring.monthlyMonthEnd' : 'budget.recurring.monthly'
    return t(key, { ordinal: formatDayOrdinal(days.dayOfMonth, language) })
  }
  const weekdayKey = days.dayOfWeek === null ? undefined : weekdayKeys[days.dayOfWeek]
  if (weekdayKey === undefined) {
    throw new Error(`A Weekly repeating income has no weekday from 0 to 6, got ${days.dayOfWeek}`)
  }
  return t('budget.recurring.weekly', { weekday: t(`budget.recurring.weekdays.${weekdayKey}`) })
}
