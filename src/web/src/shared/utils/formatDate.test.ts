import { describe, expect, it } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { addDays, formatDayMonthYear, formatDayWithYear, formatExpenseDay, todayInTimeZone } from './formatDate'

const dateFormat = /^\d{4}-\d{2}-\d{2}$/

const shortMonths: [number, string, string][] = [
  [1, 'јан', 'Jan'],
  [2, 'фев', 'Feb'],
  [3, 'мар', 'Mar'],
  [4, 'апр', 'Apr'],
  [5, 'мај', 'May'],
  [6, 'јун', 'Jun'],
  [7, 'јул', 'Jul'],
  [8, 'авг', 'Aug'],
  [9, 'сеп', 'Sep'],
  [10, 'окт', 'Oct'],
  [11, 'ное', 'Nov'],
  [12, 'дек', 'Dec'],
]

const longMonths: [number, string, string][] = [
  [1, 'јануари', 'Jan'],
  [2, 'февруари', 'Feb'],
  [3, 'март', 'Mar'],
  [4, 'април', 'Apr'],
  [5, 'мај', 'May'],
  [6, 'јуни', 'Jun'],
  [7, 'јули', 'Jul'],
  [8, 'август', 'Aug'],
  [9, 'септември', 'Sep'],
  [10, 'октомври', 'Oct'],
  [11, 'ноември', 'Nov'],
  [12, 'декември', 'Dec'],
]

describe('todayInTimeZone', () => {
  it('answers the date as yyyy-MM-dd', () => {
    expect(todayInTimeZone('Europe/Skopje', new Date('2026-10-06T10:00:00Z'))).toMatch(dateFormat)
  })

  it.each([
    ['Europe/Skopje', '2026-10-06'],
    ['America/New_York', '2026-10-05'],
    ['UTC', '2026-10-05'],
  ])('answers 23:30 UTC on 5 Oct as the day in %s: %s', (timeZone, expectedDate) => {
    expect(todayInTimeZone(timeZone, new Date('2026-10-05T23:30:00Z'))).toBe(expectedDate)
  })

  it.each([
    ['2026-10-05T21:59:59Z', '2026-10-05'],
    ['2026-10-05T22:00:00Z', '2026-10-06'],
  ])('switches Skopje (summer time, UTC+2) to the next day exactly at local midnight: %s is %s', (instant, expectedDate) => {
    expect(todayInTimeZone('Europe/Skopje', new Date(instant))).toBe(expectedDate)
  })

  it.each([
    ['2026-10-25T22:30:00Z', '2026-10-25'],
    ['2026-10-25T23:30:00Z', '2026-10-26'],
  ])('follows the end of summer time in Skopje on 25 Oct 2026: %s is %s', (instant, expectedDate) => {
    expect(todayInTimeZone('Europe/Skopje', new Date(instant))).toBe(expectedDate)
  })

  it('rolls over the year in winter time', () => {
    expect(todayInTimeZone('Europe/Skopje', new Date('2026-12-31T23:30:00Z'))).toBe('2027-01-01')
    expect(todayInTimeZone('America/New_York', new Date('2027-01-01T03:30:00Z'))).toBe('2026-12-31')
  })

  it.each([
    ['Pacific/Kiritimati', '2026-10-05T12:00:00Z', '2026-10-06'],
    ['Pacific/Pago_Pago', '2026-10-06T05:00:00Z', '2026-10-05'],
  ])('works in the far time zone %s (%s is %s there)', (timeZone, instant, expectedDate) => {
    expect(todayInTimeZone(timeZone, new Date(instant))).toBe(expectedDate)
  })

  it('pads single-digit months and days with a zero', () => {
    expect(todayInTimeZone('Europe/Skopje', new Date('2026-01-05T10:00:00Z'))).toBe('2026-01-05')
  })

  it('knows the leap day', () => {
    expect(todayInTimeZone('Europe/Skopje', new Date('2028-02-29T10:00:00Z'))).toBe('2028-02-29')
  })

  it('stops with an error naming a time zone that does not exist', () => {
    expect(() => todayInTimeZone('Mars/Phobos', new Date('2026-10-06T10:00:00Z'))).toThrow(
      'Mars/Phobos',
    )
  })

  it('stops with an error when the date is not a real date', () => {
    expect(() => todayInTimeZone('Europe/Skopje', new Date('not a date'))).toThrow()
  })
})

describe('formatExpenseDay', () => {
  it.each([
    ['2026-10-03', '3 Oct'],
    ['2026-10-06', '6 Oct'],
    ['2026-10-05', '5 Oct'],
    ['2026-01-05', '5 Jan'],
    ['2026-12-31', '31 Dec'],
  ])('writes %s as "%s" in English when it is in the same year as today', (date, expected) => {
    expect(formatExpenseDay(date, '2026-10-06', 'en')).toBe(expected)
  })

  it.each([
    ['2025-12-24', '24 Dec 2025'],
    ['2025-10-06', '6 Oct 2025'],
    ['2027-01-02', '2 Jan 2027'],
    ['2000-01-01', '1 Jan 2000'],
  ])('writes %s with its year as "%s" in English when it is not in the year of today', (date, expected) => {
    expect(formatExpenseDay(date, '2026-10-06', 'en')).toBe(expected)
  })

  it('writes September with three letters, like the rate line "24 Sep 2026"', () => {
    expect(formatExpenseDay('2026-09-24', '2026-10-06', 'en')).toBe('24 Sep')
    expect(formatExpenseDay('2026-09-24', '2027-03-01', 'en')).toBe('24 Sep 2026')
  })

  it('decides about the year from the day passed as today, not from the clock', () => {
    expect(formatExpenseDay('2026-10-03', '2027-01-01', 'en')).toBe('3 Oct 2026')
    expect(formatExpenseDay('2026-10-03', '2026-01-01', 'en')).toBe('3 Oct')
  })

  it.each([
    ['2026-10-03', '3 окт'],
    ['2026-09-24', '24 сеп'],
    ['2026-01-05', '5 јан'],
    ['2026-12-31', '31 дек'],
  ])('writes %s as «%s» in Macedonian, with no dot, when it is in the same year as today', (date, expected) => {
    expect(formatExpenseDay(date, '2026-10-06', 'mk')).toBe(expected)
  })

  it.each([
    ['2025-12-24', '24 дек 2025', '2026-10-06'],
    ['2027-01-02', '2 јан 2027', '2026-10-06'],
    ['2026-09-24', '24 сеп 2026', '2027-03-01'],
  ])('writes %s with its year as «%s» in Macedonian, with no dot and no «г.», when today is %s', (date, expected, today) => {
    expect(formatExpenseDay(date, today, 'mk')).toBe(expected)
  })

  it.each(shortMonths)('writes month %i in Macedonian as «%s» and in English as «%s»', (month, macedonian, english) => {
    const date = `2026-${String(month).padStart(2, '0')}-15`

    expect(formatExpenseDay(date, '2026-10-06', 'mk')).toBe(`15 ${macedonian}`)
    expect(formatExpenseDay(date, '2026-10-06', 'en')).toBe(`15 ${english}`)
  })

  it.each(shortMonths)('writes month %i with its year in Macedonian as «%s» and in English as «%s»', (month, macedonian, english) => {
    const date = `2025-${String(month).padStart(2, '0')}-15`

    expect(formatExpenseDay(date, '2026-10-06', 'mk')).toBe(`15 ${macedonian} 2025`)
    expect(formatExpenseDay(date, '2026-10-06', 'en')).toBe(`15 ${english} 2025`)
  })

  it('writes a one-digit day without a leading zero in Macedonian', () => {
    expect(formatExpenseDay('2026-10-05', '2026-10-06', 'mk')).toBe('5 окт')
  })

  it.each(['2026-10-03', '2025-12-24'])('writes %s in Cyrillic letters only in Macedonian', (date) => {
    const text = formatExpenseDay(date, '2026-10-06', 'mk')

    expect(text).toMatch(/[Ѐ-ӿ]/)
    expect(text).not.toMatch(/[A-Za-z]/)
  })

  it.each(['en', 'mk'] as const satisfies readonly Language[])('stops with an error naming a text that is not a date (%s)', (language) => {
    expect(() => formatExpenseDay('tomorrow', '2026-10-06', language)).toThrow('tomorrow')
  })

  it.each(['en', 'mk'] as const satisfies readonly Language[])('stops with an error naming a today that is not a date (%s)', (language) => {
    expect(() => formatExpenseDay('2026-10-03', 'later', language)).toThrow('later')
  })
})

describe('formatDayWithYear', () => {
  it.each([
    ['2026-09-24', '24 Sep 2026', '24 сеп 2026'],
    ['2026-10-03', '3 Oct 2026', '3 окт 2026'],
    ['2000-01-01', '1 Jan 2000', '1 јан 2000'],
  ])('writes %s with its year, in English as "%s" and in Macedonian as «%s»', (date, english, macedonian) => {
    expect(formatDayWithYear(date, 'en')).toBe(english)
    expect(formatDayWithYear(date, 'mk')).toBe(macedonian)
  })

  it.each(languages)('stops with an error naming a text that is not a date (%s)', (language) => {
    expect(() => formatDayWithYear('last week', language)).toThrow('last week')
  })
})

describe('formatDayMonthYear', () => {
  it.each(longMonths)('writes month %i of a date and time in Macedonian as «%s» with the full month name and in English as «%s»', (month, macedonian, english) => {
    const moment = `2026-${String(month).padStart(2, '0')}-15T12:00:00Z`

    expect(formatDayMonthYear(moment, 'mk')).toBe(`15 ${macedonian} 2026`)
    expect(formatDayMonthYear(moment, 'en')).toBe(`15 ${english} 2026`)
  })

  it('writes «12 октомври 2026» for the restore-until date of a deleted item', () => {
    expect(formatDayMonthYear('2026-10-12T12:00:00Z', 'mk')).toBe('12 октомври 2026')
  })

  it('writes "12 Oct 2026" in English', () => {
    expect(formatDayMonthYear('2026-10-12T12:00:00Z', 'en')).toBe('12 Oct 2026')
  })

  it('writes September with three letters in English, never "Sept"', () => {
    expect(formatDayMonthYear('2026-09-24T12:00:00Z', 'en')).toBe('24 Sep 2026')
  })

  it('writes a one-digit day without a leading zero in Macedonian', () => {
    expect(formatDayMonthYear('2026-10-09T12:00:00Z', 'mk')).toBe('9 октомври 2026')
  })

  it('writes the Macedonian date in Cyrillic letters only', () => {
    expect(formatDayMonthYear('2026-10-12T12:00:00Z', 'mk')).not.toMatch(/[A-Za-z]/)
  })

  it.each(languages)('stops with an error naming a date and time that is not real (%s)', (language) => {
    expect(() => formatDayMonthYear('soon', language)).toThrow('soon')
  })
})

describe('addDays', () => {
  it.each([
    ['2026-10-06', -1, '2026-10-05'],
    ['2026-10-01', -1, '2026-09-30'],
    ['2026-12-31', 1, '2027-01-01'],
    ['2028-02-28', 1, '2028-02-29'],
    ['2026-10-06', 365, '2027-10-06'],
  ])('moves %s by %i days to %s', (date, days, expected) => {
    expect(addDays(date, days)).toBe(expected)
  })

  it('stops with an error naming a text that is not a date', () => {
    expect(() => addDays('tomorrow', 1)).toThrow('tomorrow')
  })
})
