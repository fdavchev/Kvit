import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

describe('the endpoint for the admin statistics', () => {
  it.each([
    [{ from: '2026-11-01', to: '2026-11-04', by: 'day' }, '/api/admin/stats?from=2026-11-01&to=2026-11-04&by=day'],
    [{ from: '2025-12-01', to: '2026-11-04', by: 'month' }, '/api/admin/stats?from=2025-12-01&to=2026-11-04&by=month'],
    [{ from: '2024-01-01', to: '2026-11-04', by: 'year' }, '/api/admin/stats?from=2024-01-01&to=2026-11-04&by=year'],
    [{ from: '2026-01-01', to: '2026-11-04', by: 'week' }, '/api/admin/stats?from=2026-01-01&to=2026-11-04&by=week'],
  ] as const)('builds adminStats for %j as %s', (request, path) => {
    expect(endpoints.adminStats(request)).toBe(path)
  })

  it('leaves out from and to when the range is "All", and keeps only the grouping', () => {
    expect(endpoints.adminStats({ by: 'month' })).toBe('/api/admin/stats?by=month')
  })

  it('leaves out to when only from is given', () => {
    expect(endpoints.adminStats({ from: '2026-11-01', by: 'week' })).toBe('/api/admin/stats?from=2026-11-01&by=week')
  })

  it('leaves out from when only to is given', () => {
    expect(endpoints.adminStats({ to: '2026-11-04', by: 'week' })).toBe('/api/admin/stats?to=2026-11-04&by=week')
  })
})
