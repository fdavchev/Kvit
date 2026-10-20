import { describe, expect, it } from 'vitest'
import { budgetMonths, earliestBudgetDate } from './budgetMonths'

describe('budgetMonths', () => {
  it('lists the 24 months that end with the given month, oldest first', () => {
    const months = budgetMonths('2026-10')

    expect(months).toHaveLength(24)
    expect(months[0]).toBe('2024-11')
    expect(months[23]).toBe('2026-10')
  })
})

describe('earliestBudgetDate', () => {
  it.each([
    ['2026-10-19', '2024-11-01'],
    ['2026-10-01', '2024-11-01'],
    ['2026-01-31', '2024-02-01'],
    ['2026-12-31', '2025-01-01'],
  ])('is the first day of the oldest month the strip shows on %s', (today, earliest) => {
    expect(earliestBudgetDate(today)).toBe(earliest)
  })

  it('starts the month that the strip shows first', () => {
    expect(earliestBudgetDate('2026-10-19')).toBe(`${budgetMonths('2026-10')[0]}-01`)
  })
})
