import type { TFunction } from 'i18next'
import type { Language } from '@/core/i18n/language'
import { countedText } from '@/core/i18n/pluralForm'

const englishDayMonthYearFormat = new Intl.DateTimeFormat('en-US', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

const macedonianShortMonths: readonly string[] = [
  'јан',
  'фев',
  'мар',
  'апр',
  'мај',
  'јун',
  'јул',
  'авг',
  'сеп',
  'окт',
  'ное',
  'дек',
]

const macedonianLongMonths: readonly string[] = [
  'јануари',
  'февруари',
  'март',
  'април',
  'мај',
  'јуни',
  'јули',
  'август',
  'септември',
  'октомври',
  'ноември',
  'декември',
]

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
  if (language === 'mk') {
    return `${date.getDate()} ${macedonianLongMonths[date.getMonth()]} ${date.getFullYear()}`
  }
  return joinParts(englishDayMonthYearFormat.formatToParts(date), dayMonthYearParts)
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

export function formatDayWithoutYear(date: string, language: Language): string {
  return formatCalendarDay(date, language, false)
}

export function formatHowLongAgo(
  isoDateTime: string,
  now: Date,
  timeZone: string,
  language: Language,
  t: TFunction,
): string {
  const moment = new Date(isoDateTime)
  if (Number.isNaN(moment.getTime())) {
    throw new Error(`Expected a date and time to say how long ago it was, got "${isoDateTime}"`)
  }
  const elapsedMs = Math.max(0, now.getTime() - moment.getTime())
  if (elapsedMs < minuteMs) {
    return t('time.justNow')
  }
  if (elapsedMs < hourMs) {
    return countedText(t, 'time.minutes', Math.floor(elapsedMs / minuteMs), language)
  }
  if (elapsedMs < dayMs) {
    return countedText(t, 'time.hours', Math.floor(elapsedMs / hourMs), language)
  }
  if (elapsedMs < daysShownAsRelative * dayMs) {
    return countedText(t, 'time.days', Math.floor(elapsedMs / dayMs), language)
  }
  return formatExpenseDay(todayInTimeZone(timeZone, moment), todayInTimeZone(timeZone, now), language)
}

export function addDays(date: string, days: number): string {
  const moved = readCalendarDate(date)
  moved.setUTCDate(moved.getUTCDate() + days)
  return moved.toISOString().slice(0, 10)
}

export function isCalendarDate(text: string): boolean {
  if (!calendarDatePattern.test(text)) {
    return false
  }
  const moment = new Date(`${text}T00:00:00Z`)
  return !Number.isNaN(moment.getTime()) && moment.toISOString().slice(0, 10) === text
}

function formatCalendarDay(date: string, language: Language, includesYear: boolean): string {
  const moment = readCalendarDate(date)
  if (language === 'mk') {
    const dayAndMonth = `${moment.getUTCDate()} ${macedonianShortMonths[moment.getUTCMonth()]}`
    return includesYear ? `${dayAndMonth} ${moment.getUTCFullYear()}` : dayAndMonth
  }
  const parts = new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'short',
    year: includesYear ? 'numeric' : undefined,
    timeZone: 'UTC',
  }).formatToParts(moment)
  return joinParts(parts, includesYear ? dayMonthYearParts : dayMonthParts)
}

function readCalendarDate(text: string): Date {
  if (!isCalendarDate(text)) {
    throw new Error(`Expected a calendar date like 2026-10-06, got "${text}"`)
  }
  return new Date(`${text}T00:00:00Z`)
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
