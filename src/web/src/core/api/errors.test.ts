import { afterEach, describe, expect, it, vi } from 'vitest'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import { ApiError } from './apiClient'
import { errorMessageKey } from './errors'

const mappedErrorCodes = [
  'AUTH_INVALID_CREDENTIALS',
  'AUTH_EMAIL_TAKEN',
  'AUTH_EMAIL_INVALID',
  'AUTH_PASSWORD_TOO_WEAK',
  'AUTH_DISPLAY_NAME_INVALID',
  'AUTH_LOCKED_OUT',
  'AUTH_NOT_SIGNED_IN',
  'TIME_ZONE_INVALID',
  'LANGUAGE_INVALID',
  'RATE_LIMITED',
  'AUTH_MUST_CHANGE_PASSWORD',
  'AUTH_CURRENT_PASSWORD_WRONG',
  'AUTH_PASSWORD_UNCHANGED',
  'AUTH_GOOGLE_TOKEN_INVALID',
  'AUTH_GOOGLE_EMAIL_NOT_VERIFIED',
  'AUTH_GOOGLE_NO_ACCOUNT',
  'AUTH_GOOGLE_EMAIL_TAKEN',
  'AUTH_USES_GOOGLE',
  'AUTH_PASSWORD_ALREADY_SET',
  'GROUP_NOT_FOUND',
  'GROUP_NOT_OWNER',
  'GROUP_NAME_INVALID',
  'GROUP_EMOJI_INVALID',
  'GROUP_CURRENCY_INVALID',
  'GROUP_NOT_DELETED',
  'GROUP_RESTORE_EXPIRED',
  'MEMBER_NAME_INVALID',
  'MEMBER_NAME_TAKEN',
  'MEMBER_OWNER_CANNOT_LEAVE',
  'MEMBER_NOT_FOUND',
  'MEMBER_IS_OWNER',
  'MEMBER_NOT_ACCOUNT',
  'MEMBER_ALREADY_OWNER',
  'MEMBER_CANNOT_CLAIM',
  'MEMBER_NOT_CLAIMED',
  'INVITE_NOT_FOUND',
  'INVITE_REMOVED',
  'INVITE_NOTHING_TO_UNDO',
]

const errorTexts: Record<'en' | 'mk', Record<string, unknown>> = {
  en: en.errors,
  mk: mk.errors,
}

describe('errorMessageKey', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('uses the network message when the server could not be reached', () => {
    const error = new ApiError('GET /api/health could not reach the server', {
      httpStatus: null,
    })

    expect(errorMessageKey(error)).toBe('errors.network')
  })

  it('uses the network message when the proxy answers 502 because the API is unreachable', () => {
    const error = new ApiError('GET /api/health failed with status 502', {
      httpStatus: 502,
    })

    expect(errorMessageKey(error)).toBe('errors.network')
  })

  it('uses the generic message for an error code with no translation', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError('GET /api/groups/1/members failed with status 404', {
      httpStatus: 404,
      errorCode: 'SOMETHING_UNKNOWN',
    })

    expect(errorMessageKey(error)).toBe('errors.generic')
  })

  it('logs an error code that has no translation key, naming the code', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError('POST /api/groups/1/owner failed with status 400', {
      httpStatus: 400,
      errorCode: 'ANOTHER_UNKNOWN_CODE',
    })

    errorMessageKey(error)

    expect(consoleError).toHaveBeenCalledWith(
      'No translation key is mapped for API error code "ANOTHER_UNKNOWN_CODE"',
      error,
    )
  })

  it('does not log when the answer carries no error code', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    errorMessageKey(new ApiError('GET /api/x failed with status 500', { httpStatus: 500 }))

    expect(consoleError).not.toHaveBeenCalled()
  })

  it('uses the generic message for an error that is not an ApiError', () => {
    expect(errorMessageKey(new Error('boom'))).toBe('errors.generic')
  })

  it.each(mappedErrorCodes)('maps %s to its own translation key without logging', (code) => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError('POST /api/x failed with status 400', {
      httpStatus: 400,
      errorCode: code,
    })

    expect(errorMessageKey(error)).toBe(`errors.${code}`)
    expect(consoleError).not.toHaveBeenCalled()
  })

  it.each(
    (['en', 'mk'] as const).flatMap((language) =>
      mappedErrorCodes.map((code) => [language, code] as const),
    ),
  )('has a non-empty %s text for %s', (language, code) => {
    expect(errorTexts[language][code]).toEqual(expect.stringMatching(/\S/))
  })
})
