import { describe, expect, it } from 'vitest'
import { endpoints } from './endpoints'

describe('endpoints', () => {
  it.each([
    ['googleLogIn', '/api/auth/google'],
    ['googleSignUp', '/api/auth/google/sign-up'],
    ['setPassword', '/api/auth/set-password'],
  ] as const)('has the path %s at %s', (name, path) => {
    expect(endpoints[name]).toBe(path)
  })
})
