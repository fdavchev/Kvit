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
  const knownKey =
    error.errorCode === null
      ? undefined
      : errorCodeMessageKeys.get(error.errorCode)
  return knownKey ?? 'errors.generic'
}
