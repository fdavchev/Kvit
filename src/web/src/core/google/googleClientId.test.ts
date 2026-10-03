import { afterEach, describe, expect, it, vi } from 'vitest'
import { googleClientId } from './googleClientId'

const clientId = '742174824373-example.apps.googleusercontent.com'

describe('googleClientId', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('returns the client id from VITE_GOOGLE_CLIENT_ID', () => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', clientId)

    expect(googleClientId()).toBe(clientId)
  })

  it.each([
    ['is empty', ''],
    ['is only spaces', '   '],
    ['is missing', undefined],
  ])('throws an Error naming VITE_GOOGLE_CLIENT_ID when it %s', (_name, value) => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', value)

    expect(() => googleClientId()).toThrow('VITE_GOOGLE_CLIENT_ID')
  })
})
