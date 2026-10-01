import { ApiError } from './apiClient'

const proxyCannotReachApiStatus = 502
const translatedErrorCodes = [
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
]
const errorCodeMessageKeys = new Map<string, string>(
  translatedErrorCodes.map((code) => [code, `errors.${code}`]),
)

export function errorMessageKey(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'errors.generic'
  }
  if (
    error.httpStatus === null ||
    error.httpStatus === proxyCannotReachApiStatus
  ) {
    return 'errors.network'
  }
  if (error.errorCode === null) {
    return 'errors.generic'
  }
  const knownKey = errorCodeMessageKeys.get(error.errorCode)
  if (knownKey === undefined) {
    console.error(
      `No translation key is mapped for API error code "${error.errorCode}"`,
      error,
    )
    return 'errors.generic'
  }
  return knownKey
}
