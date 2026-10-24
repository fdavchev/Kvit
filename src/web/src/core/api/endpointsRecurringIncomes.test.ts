import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

const recurringIncomeId = 'a0000000-0000-4000-8000-000000000001'

describe('endpoints for repeating income', () => {
  it.each([
    ['recurringIncomes', '/api/personal/recurring-incomes'],
    ['recurringIncomesCatchUp', '/api/personal/recurring-incomes/catch-up'],
  ] as const)('has the path %s at %s', (name, path) => {
    expect(endpoints[name]).toBe(path)
  })

  it.each([
    ['recurringIncome', '/api/personal/recurring-incomes/a0000000-0000-4000-8000-000000000001'],
    ['recurringIncomeStop', '/api/personal/recurring-incomes/a0000000-0000-4000-8000-000000000001/stop'],
    ['recurringIncomeRestart', '/api/personal/recurring-incomes/a0000000-0000-4000-8000-000000000001/restart'],
  ] as const)('builds the path %s for a repeating income id as %s', (name, path) => {
    expect(endpoints[name](recurringIncomeId)).toBe(path)
  })
})
