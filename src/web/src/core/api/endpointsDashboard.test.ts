import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

describe('endpoints for the dashboard', () => {
  it.each([
    ['dashboard', '/api/dashboard'],
    ['meActive', '/api/me/active'],
  ] as const)('has the path %s at %s', (name, path) => {
    expect(endpoints[name]).toBe(path)
  })
})
