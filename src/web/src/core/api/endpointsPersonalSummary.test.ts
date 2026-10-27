import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

describe('endpoints for the monthly summary', () => {
  it.each([
    ['2026-10', '/api/personal/summary?month=2026-10'],
    ['2026-01', '/api/personal/summary?month=2026-01'],
    ['2000-01', '/api/personal/summary?month=2000-01'],
  ])('builds the path personalSummary for the month %s as %s', (month, path) => {
    expect(endpoints.personalSummary(month)).toBe(path)
  })
})
