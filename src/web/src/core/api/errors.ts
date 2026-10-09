import { ApiError, notFoundStatus } from './apiClient'

const serverNotReadyStatuses = [502, 503, 504]
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
  'MONEY_CURRENCY_MISMATCH',
  'MONEY_NOT_ON_CURRENCY_STEP',
  'EXPENSE_AMOUNT_NOT_POSITIVE',
  'EXPENSE_SPLIT_NO_PARTICIPANTS',
  'EXPENSE_SPLIT_DUPLICATE_MEMBER',
  'EXPENSE_SPLIT_NEGATIVE_INPUT',
  'EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL',
  'EXPENSE_SPLIT_DOES_NOT_ADD_UP',
  'EXPENSE_SPLIT_NO_SHARES',
  'EXPENSE_NOT_FOUND',
  'EXPENSE_NOT_ALLOWED',
  'EXPENSE_NOT_DELETED',
  'EXPENSE_RESTORE_EXPIRED',
  'EXPENSE_TITLE_INVALID',
  'EXPENSE_NOTE_INVALID',
  'EXPENSE_DATE_INVALID',
  'EXPENSE_CURRENCY_INVALID',
  'EXPENSE_SPLIT_TYPE_INVALID',
  'EXPENSE_CATEGORY_INVALID',
  'EXPENSE_AMOUNT_TOO_LARGE',
  'EXPENSE_CLIENT_REQUEST_ID_USED',
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
    serverNotReadyStatuses.includes(error.httpStatus)
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
