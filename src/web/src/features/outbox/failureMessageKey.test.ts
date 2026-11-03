import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'
import { failureMessageKey } from './failureMessageKey'

describe('failureMessageKey', () => {
  it.each([
    ['GROUP_FINISHED', 400, 'errors.GROUP_FINISHED'],
    ['GROUP_NOT_FOUND', 404, 'errors.GROUP_NOT_FOUND'],
    ['SETTLEMENT_NOT_ALLOWED', 403, 'errors.SETTLEMENT_NOT_ALLOWED'],
  ])('gives the text of the code %s that came with status %i', (errorCode, httpStatus, expectedKey) => {
    expect(failureMessageKey({ errorCode, httpStatus })).toBe(expectedKey)
  })

  it('gives the generic text for a 400 that came without a code', () => {
    expect(failureMessageKey({ errorCode: null, httpStatus: 400 })).toBe('errors.generic')
  })

  it('gives the generic text when neither a code nor a status was kept, because the item failed for a reason that is not an API answer', () => {
    expect(failureMessageKey({ errorCode: null, httpStatus: null })).toBe('errors.generic')
  })

  it('gives the group-not-found text for a 404 without a code, like every other 404 in the app', () => {
    expect(failureMessageKey({ errorCode: null, httpStatus: 404 })).toBe('errors.GROUP_NOT_FOUND')
  })

  it.each(['GROUP_FINISHED', 'GROUP_NOT_FOUND', 'SETTLEMENT_NOT_ALLOWED'])('has an English and a Macedonian text for the key of %s', (errorCode) => {
    const key = failureMessageKey({ errorCode, httpStatus: 400 })

    expect(translated('en', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe('')
  })
})
