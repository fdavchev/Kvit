import { ApiError } from './apiClient'

const proxyCannotReachApiStatus = 502
const errorCodeMessageKeys = new Map<string, string>()

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
