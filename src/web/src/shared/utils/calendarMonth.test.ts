import { describe, expect, it } from 'vitest'
import { addMonths, currentMonthInTimeZone, formatMonthLabel, recentMonths } from './calendarMonth'

describe('currentMonthInTimeZone', () => {
  it('gives the month of the moment as year and month', () => {
    expect(currentMonthInTimeZone('Europe/Skopje', new Date('2026-10-19T10:00:00Z'))).toBe('2026-10')
  })

  it('is already the next month in Skopje half an hour before midnight in London on the last day', () => {
    const lastEveningOfOctober = new Date('2026-10-31T23:30:00Z')

    expect(currentMonthInTimeZone('Europe/Skopje', lastEveningOfOctober)).toBe('2026-11')
    expect(currentMonthInTimeZone('Europe/London', lastEveningOfOctober)).toBe('2026-10')
  })

  it('is still the old month in New York when it is already the next month in UTC', () => {
    expect(currentMonthInTimeZone('America/New_York', new Date('2026-11-01T02:00:00Z'))).toBe('2026-10')
    expect(currentMonthInTimeZone('UTC', new Date('2026-11-01T02:00:00Z'))).toBe('2026-11')
  })

  it('moves to January of the next year in Auckland while it is still December in UTC', () => {
    expect(currentMonthInTimeZone('Pacific/Auckland', new Date('2026-12-31T12:00:00Z'))).toBe('2027-01')
  })

  it('throws for an invalid moment, naming the time zone', () => {
    expect(() => currentMonthInTimeZone('Europe/Skopje', new Date('not a date'))).toThrow(/Europe\/Skopje/)
  })
})

describe('addMonths', () => {
  it.each([
    ['2026-10', -1, '2026-09'],
    ['2026-10', 1, '2026-11'],
    ['2026-01', -1, '2025-12'],
    ['2025-12', 1, '2026-01'],
    ['2026-10', -24, '2024-10'],
    ['2026-10', 0, '2026-10'],
  ])('moves %s by %i months to %s', (month, count, expected) => {
    expect(addMonths(month, count)).toBe(expected)
  })

  it.each(['2026-13', '2026-00', '2026-1', '26-10', '2026-10-01', 'October', ''])(
    'throws for "%s", which is not a month like 2026-10',
    (text) => {
      expect(() => addMonths(text, 1)).toThrow(`Expected a month like 2026-10, got "${text}"`)
    },
  )
})

describe('recentMonths', () => {
  it('lists the last 24 months, oldest first, ending at the given month', () => {
    const months = recentMonths('2026-10', 24)

    expect(months).toHaveLength(24)
    expect(months[0]).toBe('2024-11')
    expect(months.at(-1)).toBe('2026-10')
  })

  it('lists every month once, each the month after the one before it', () => {
    const months = recentMonths('2026-10', 24)

    for (let index = 1; index < months.length; index += 1) {
      expect(months[index]).toBe(addMonths(months[index - 1] ?? '', 1))
    }
  })

  it('crosses the turn of the year', () => {
    expect(recentMonths('2026-01', 3)).toEqual(['2025-11', '2025-12', '2026-01'])
  })

  it('lists only the given month when one month is asked for', () => {
    expect(recentMonths('2026-10', 1)).toEqual(['2026-10'])
  })
})

describe('formatMonthLabel', () => {
  const currentMonth = '2026-10'

  it.each([
    ['2026-02', 'Feb', 'фев'],
    ['2026-03', 'Mar', 'мар'],
    ['2026-04', 'Apr', 'апр'],
    ['2026-05', 'May', 'мај'],
    ['2026-06', 'Jun', 'јун'],
    ['2026-07', 'Jul', 'јул'],
    ['2026-08', 'Aug', 'авг'],
    ['2026-09', 'Sep', 'сеп'],
    ['2026-10', 'Oct', 'окт'],
  ])('shows %s of the current year by its short name only: %s / %s', (month, english, macedonian) => {
    expect(formatMonthLabel(month, currentMonth, 'en')).toBe(english)
    expect(formatMonthLabel(month, currentMonth, 'mk')).toBe(macedonian)
  })

  it('shows the year on January of the current year, where the year changes in the strip', () => {
    expect(formatMonthLabel('2026-01', currentMonth, 'en')).toBe('Jan 2026')
    expect(formatMonthLabel('2026-01', currentMonth, 'mk')).toBe('јан 2026')
  })

  it.each([
    ['2025-11', 'Nov 2025', 'ное 2025'],
    ['2025-12', 'Dec 2025', 'дек 2025'],
    ['2025-06', 'Jun 2025', 'јун 2025'],
    ['2024-11', 'Nov 2024', 'ное 2024'],
  ])('shows the year on %s, a month of another year: %s / %s', (month, english, macedonian) => {
    expect(formatMonthLabel(month, currentMonth, 'en')).toBe(english)
    expect(formatMonthLabel(month, currentMonth, 'mk')).toBe(macedonian)
  })

  it('shows the year on the current month when the current month is January', () => {
    expect(formatMonthLabel('2027-01', '2027-01', 'en')).toBe('Jan 2027')
  })

  it('throws for a month that is not like 2026-10', () => {
    expect(() => formatMonthLabel('2026-13', currentMonth, 'en')).toThrow('Expected a month like 2026-10, got "2026-13"')
  })
})
