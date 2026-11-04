import { describe, expect, it } from 'vitest'
import { earliestStatsDay, isUsableCustomRange, presetDates } from './statsRange'

const today = '2026-11-04'

describe('earliestStatsDay', () => {
  it('is 1 January 2026, the day the API starts counting from', () => {
    expect(earliestStatsDay).toBe('2026-01-01')
  })
})

describe('isUsableCustomRange', () => {
  it('accepts a start on 1 January 2026', () => {
    expect(isUsableCustomRange({ from: '2026-01-01', to: today }, today)).toBe(true)
  })

  it('refuses a start on 31 December 2025, the day before', () => {
    expect(isUsableCustomRange({ from: '2025-12-31', to: today }, today)).toBe(false)
  })

  it.each(['0002-11-04', '0020-11-04', '0202-11-04'])('refuses the start %s that the date field gives while the year is still being typed', (from) => {
    expect(isUsableCustomRange({ from, to: today }, today)).toBe(false)
  })

  it('refuses an end before 1 January 2026', () => {
    expect(isUsableCustomRange({ from: '2025-12-01', to: '2025-12-31' }, today)).toBe(false)
  })
})

describe('presetDates', () => {
  it.each([
    ['days7', '2026-01-03', { from: '2026-01-01', to: '2026-01-03' }],
    ['days30', '2026-01-15', { from: '2026-01-01', to: '2026-01-15' }],
    ['months12', '2026-11-04', { from: '2026-01-01', to: '2026-11-04' }],
  ] as const)('starts %s on 1 January 2026 at the earliest, on %s', (range, presetToday, dates) => {
    expect(presetDates(range, presetToday)).toEqual(dates)
  })
})
