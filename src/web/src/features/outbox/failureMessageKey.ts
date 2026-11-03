import { ApiError } from '@/core/api/apiClient'
import { errorMessageKey } from '@/core/api/errors'
import type { OutboxFailure } from '@/core/outbox/outboxItem'

export function failureMessageKey(failure: OutboxFailure): string {
  if (failure.errorCode === null && failure.httpStatus === null) {
    return 'errors.generic'
  }
  return errorMessageKey(
    new ApiError(`The waiting change was refused with status ${failure.httpStatus} and code ${failure.errorCode}`, failure),
  )
}
