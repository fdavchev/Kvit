import { ApiError, notFoundStatus } from './apiClient'

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
const errorCodeMessageKeys = new Map<string, string>(
  translatedErrorCodes.map((code) => [code, `errors.${code}`]),
)

export function hasErrorCode(error: unknown, errorCode: string): boolean {
  return error instanceof ApiError && error.errorCode === errorCode
}

export function isNotFoundError(error: unknown): boolean {
  return error instanceof ApiError && error.httpStatus === notFoundStatus
}

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
    return error.httpStatus === notFoundStatus
      ? 'errors.GROUP_NOT_FOUND'
      : 'errors.generic'
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
