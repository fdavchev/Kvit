import { describe, expect, it } from 'vitest'
import { routes } from './routes'

describe('the route of the monthly summary', () => {
  it('has the route budgetSummary at /budget/summary', () => {
    expect(routes.budgetSummary).toBe('/budget/summary')
  })

  it('sits under the Budget tab like the repeating income screen', () => {
    expect(routes.budgetSummary.startsWith(`${routes.budget}/`)).toBe(true)
  })

  it.each([
    ['2026-10', '/budget/summary?month=2026-10'],
    ['2026-01', '/budget/summary?month=2026-01'],
    ['2024-11', '/budget/summary?month=2024-11'],
  ])('builds the route budgetSummaryOf for the month %s as %s', (month, path) => {
    expect(routes.budgetSummaryOf(month)).toBe(path)
  })
})
