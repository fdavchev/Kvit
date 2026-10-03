import { describe, expect, it } from 'vitest'
import { routes } from './routes'

describe('routes', () => {
  it.each([
    ['googleSignUp', '/signup/google'],
    ['setPassword', '/settings/set-password'],
    ['privacy', '/privacy'],
  ] as const)('has the route %s at %s', (name, path) => {
    expect(routes[name]).toBe(path)
  })
})
