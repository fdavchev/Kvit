import { describe, expect, it } from 'vitest'
import { routes } from './routes'

describe('the route of the Budget tab with a month', () => {
  it('keeps the route budget at /budget, so the bottom bar tab opens the current month', () => {
    expect(routes.budget).toBe('/budget')
  })

  it.each([
    ['2026-10', '/budget?month=2026-10'],
    ['2026-01', '/budget?month=2026-01'],
    ['2024-11', '/budget?month=2024-11'],
  ])('builds the route budgetOf for the month %s as %s', (month, path) => {
    expect(routes.budgetOf(month)).toBe(path)
  })

  it('starts with the route budget, so it is the same screen', () => {
    expect(routes.budgetOf('2026-10').startsWith(`${routes.budget}?`)).toBe(true)
  })
})
