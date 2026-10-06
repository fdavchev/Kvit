import { describe, expect, it } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { macedonianDay } from '@/test/expenseTestHelpers'
import { formatExpenseDay, todayInTimeZone } from './formatDate'

const dateFormat = /^\d{4}-\d{2}-\d{2}$/

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
    ['2026-10-03', '2026-10-06', false],
    ['2026-01-05', '2026-10-06', false],
    ['2025-12-24', '2026-10-06', true],
    ['2027-01-02', '2026-10-06', true],
  ])('writes %s in Macedonian exactly as Intl writes it for the Macedonian language (today %s, with year: %s)', (date, today, includesYear) => {
    expect(formatExpenseDay(date, today, 'mk')).toBe(macedonianDay(date, includesYear))
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
