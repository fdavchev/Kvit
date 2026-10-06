import type { Language } from '@/core/i18n/language'

const dayMonthYearFormats: Record<Language, Intl.DateTimeFormat> = {
  en: new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'short', year: 'numeric' }),
  mk: new Intl.DateTimeFormat('mk', { day: 'numeric', month: 'long', year: 'numeric' }),
}

const minuteMs = 60_000
const hourMs = 60 * minuteMs
const dayMs = 24 * hourMs
const daysShownAsRelative = 7

const dayMonthYearParts: readonly Intl.DateTimeFormatPartTypes[] = ['day', 'month', 'year']
const dayMonthParts: readonly Intl.DateTimeFormatPartTypes[] = ['day', 'month']
const calendarDatePattern = /^\d{4}-\d{2}-\d{2}$/

export function formatDayMonthYear(isoDateTime: string, language: Language): string {
  const date = new Date(isoDateTime)
  if (Number.isNaN(date.getTime())) {
    throw new Error(`Expected a date and time to format, got "${isoDateTime}"`)
  }
  return joinParts(dayMonthYearFormats[language].formatToParts(date), dayMonthYearParts)
}

export function todayInTimeZone(timeZone: string, now: Date): string {
  if (Number.isNaN(now.getTime())) {
    throw new Error(`Expected a real moment to find today in ${timeZone}, got an invalid date`)
  }
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(now)
  return `${partValue(parts, 'year')}-${partValue(parts, 'month')}-${partValue(parts, 'day')}`
}

export function formatExpenseDay(date: string, today: string, language: Language): string {
  readCalendarDate(today)
  return formatCalendarDay(date, language, date.slice(0, 4) !== today.slice(0, 4))
}

export function formatDayWithYear(date: string, language: Language): string {
  return formatCalendarDay(date, language, true)
}

export function formatHowLongAgo(
  isoDateTime: string,
  now: Date,
  timeZone: string,
  language: Language,
): string {
  const moment = new Date(isoDateTime)
  if (Number.isNaN(moment.getTime())) {
    throw new Error(`Expected a date and time to say how long ago it was, got "${isoDateTime}"`)
  }
  const elapsedMs = Math.max(0, now.getTime() - moment.getTime())
  const relative = new Intl.RelativeTimeFormat(language)
  if (elapsedMs < hourMs) {
    return relative.format(-Math.floor(elapsedMs / minuteMs), 'minute')
  }
  if (elapsedMs < dayMs) {
    return relative.format(-Math.floor(elapsedMs / hourMs), 'hour')
  }
  if (elapsedMs < daysShownAsRelative * dayMs) {
    return relative.format(-Math.floor(elapsedMs / dayMs), 'day')
  }
  return formatExpenseDay(todayInTimeZone(timeZone, moment), todayInTimeZone(timeZone, now), language)
}

export function addDays(date: string, days: number): string {
  const moved = readCalendarDate(date)
  moved.setUTCDate(moved.getUTCDate() + days)
  return moved.toISOString().slice(0, 10)
}

function formatCalendarDay(date: string, language: Language, includesYear: boolean): string {
  const moment = readCalendarDate(date)
  const year = includesYear ? 'numeric' : undefined
  if (language === 'mk') {
    return new Intl.DateTimeFormat('mk', {
      day: 'numeric',
      month: 'short',
      year,
      timeZone: 'UTC',
    }).format(moment)
  }
  const parts = new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'short',
    year,
    timeZone: 'UTC',
  }).formatToParts(moment)
  return joinParts(parts, includesYear ? dayMonthYearParts : dayMonthParts)
}

function readCalendarDate(text: string): Date {
  const moment = new Date(`${text}T00:00:00Z`)
  if (
    !calendarDatePattern.test(text) ||
    Number.isNaN(moment.getTime()) ||
    moment.toISOString().slice(0, 10) !== text
  ) {
    throw new Error(`Expected a calendar date like 2026-10-06, got "${text}"`)
  }
  return moment
}

function joinParts(
  parts: Intl.DateTimeFormatPart[],
  order: readonly Intl.DateTimeFormatPartTypes[],
): string {
  return order.map((type) => partValue(parts, type)).join(' ')
}

function partValue(parts: Intl.DateTimeFormatPart[], type: Intl.DateTimeFormatPartTypes): string {
  const part = parts.find((candidate) => candidate.type === type)
  if (part === undefined) {
    throw new Error(`Intl did not write the ${type} of the date`)
  }
  return part.value
}
