import type { Language } from '@/core/i18n/language'
import { macedonianShortMonths, todayInTimeZone } from './formatDate'

interface CalendarMonth {
  year: number
  monthIndex: number
}

const monthsPerYear = 12
const calendarMonthPattern = /^(\d{4})-(\d{2})$/

const englishShortMonthFormat = new Intl.DateTimeFormat('en-US', { month: 'short', timeZone: 'UTC' })

export function currentMonthInTimeZone(timeZone: string, now: Date): string {
  return todayInTimeZone(timeZone, now).slice(0, 7)
}

export function addMonths(month: string, count: number): string {
  const { year, monthIndex } = readCalendarMonth(month)
  const movedIndex = year * monthsPerYear + monthIndex + count
  return calendarMonthText({
    year: Math.floor(movedIndex / monthsPerYear),
    monthIndex: movedIndex % monthsPerYear,
  })
}

export function recentMonths(lastMonth: string, count: number): string[] {
  return Array.from({ length: count }, (_unused, index) => addMonths(lastMonth, index - count + 1))
}

export function formatMonthLabel(month: string, currentMonth: string, language: Language): string {
  const { year, monthIndex } = readCalendarMonth(month)
  const showsYear = year !== readCalendarMonth(currentMonth).year || monthIndex === 0
  const name =
    language === 'mk'
      ? macedonianShortMonths[monthIndex]
      : englishShortMonthFormat.format(new Date(Date.UTC(year, monthIndex, 1)))
  return showsYear ? `${name} ${year}` : `${name}`
}

function readCalendarMonth(text: string): CalendarMonth {
  const match = calendarMonthPattern.exec(text)
  const monthNumber = Number(match?.[2])
  if (match === null || monthNumber < 1 || monthNumber > monthsPerYear) {
    throw new Error(`Expected a month like 2026-10, got "${text}"`)
  }
  return { year: Number(match[1]), monthIndex: monthNumber - 1 }
}

function calendarMonthText({ year, monthIndex }: CalendarMonth): string {
  return `${String(year).padStart(4, '0')}-${String(monthIndex + 1).padStart(2, '0')}`
}
