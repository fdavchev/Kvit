import { describe, expect, it } from 'vitest'
import { endAfterMonths } from './recurringIncomeRhythm'

describe('endAfterMonths', () => {
  it.each([
    ['2026-10-20', 3, '2027-01-19'],
    ['2026-10-20', 6, '2027-04-19'],
    ['2026-10-20', 12, '2027-10-19'],
    ['2026-08-01', 3, '2026-10-31'],
    ['2026-12-05', 1, '2027-01-04'],
  ])('ends %s plus %i months on the day before, %s, so it gives exactly that many monthly payments', (startsOn, months, endsOn) => {
    expect(endAfterMonths(startsOn, months)).toBe(endsOn)
  })

  it.each([
    ['2026-08-31', 3, '2026-11-29'],
    ['2026-08-31', 6, '2027-02-27'],
    ['2028-01-31', 1, '2028-02-28'],
    ['2026-08-31', 12, '2027-08-30'],
  ])('clamps a start at the end of the month like the payments do: %s plus %i months ends %s', (startsOn, months, endsOn) => {
    expect(endAfterMonths(startsOn, months)).toBe(endsOn)
  })
})
