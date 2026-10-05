import { describe, expect, it } from 'vitest'
import { routes } from './routes'

const groupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'

describe('routes', () => {
  it.each([
    ['googleSignUp', '/signup/google'],
    ['setPassword', '/settings/set-password'],
    ['privacy', '/privacy'],
    ['groups', '/groups'],
    ['newGroup', '/groups/new'],
    ['recentlyDeletedGroups', '/groups/recently-deleted'],
  ] as const)('has the route %s at %s', (name, path) => {
    expect(routes[name]).toBe(path)
  })

  it.each([
    ['group', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'],
    ['groupSettings', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/settings'],
  ] as const)('builds the route %s for a group id as %s', (name, path) => {
    expect(routes[name](groupId)).toBe(path)
  })
})
