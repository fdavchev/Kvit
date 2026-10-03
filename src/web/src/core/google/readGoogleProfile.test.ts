import { describe, expect, it } from 'vitest'
import { fakeIdToken, testGoogleProfile } from '@/test/googleTestHelpers'
import { readGoogleProfile } from './readGoogleProfile'

const urlSafeProfile = { name: '>>>???>>>', email: 'a@b.mk' }

function tokenWithPayloadText(payloadText: string): string {
  const payload = btoa(payloadText).replaceAll('=', '')
  return `header.${payload}.signature`
}

describe('readGoogleProfile', () => {
  it('reads the name and email from the token payload', () => {
    const profile = readGoogleProfile(fakeIdToken({ name: 'Ana Ivanova', email: 'ana@example.com' }))

    expect(profile).toEqual({ name: 'Ana Ivanova', email: 'ana@example.com' })
  })

  it('keeps Macedonian letters intact', () => {
    const profile = readGoogleProfile(fakeIdToken(testGoogleProfile))

    expect(profile).toEqual(testGoogleProfile)
  })

  it.each(['Ќ', 'Ќа', 'Ќаџ'])('reads the name %s whatever padding its encoded payload needs', (name) => {
    const profile = readGoogleProfile(fakeIdToken({ name, email: 'k@example.com' }))

    expect(profile.name).toBe(name)
  })

  it('reads a payload that uses the URL-safe characters - and _', () => {
    const token = fakeIdToken(urlSafeProfile)
    expect(token.split('.')[1]).toMatch(/[-_]/)

    expect(readGoogleProfile(token)).toEqual(urlSafeProfile)
  })

  it('ignores the other claims in the payload', () => {
    const profile = readGoogleProfile(
      fakeIdToken({ ...testGoogleProfile, sub: '1234567890', picture: 'https://example.com/p.png' }),
    )

    expect(profile).toEqual(testGoogleProfile)
  })

  it.each([
    ['is empty', ''],
    ['has one part', 'not-a-token'],
    ['has two parts', 'header.payload'],
    ['has four parts', 'header.payload.signature.extra'],
  ])('throws an Error when the token %s', (_name, token) => {
    expect(() => readGoogleProfile(token)).toThrow(Error)
  })

  it('throws an Error when the payload is not valid base64', () => {
    expect(() => readGoogleProfile('header.!!!.signature')).toThrow(Error)
  })

  it('throws an Error when the payload is not JSON', () => {
    expect(() => readGoogleProfile(tokenWithPayloadText('plain text, not JSON'))).toThrow(Error)
  })

  it('throws an Error naming "name" when the payload has no name', () => {
    expect(() => readGoogleProfile(fakeIdToken({ email: 'ana@example.com' }))).toThrow('name')
  })

  it('throws an Error naming "email" when the payload has no email', () => {
    expect(() => readGoogleProfile(fakeIdToken({ name: 'Ana Ivanova' }))).toThrow('email')
  })
})
