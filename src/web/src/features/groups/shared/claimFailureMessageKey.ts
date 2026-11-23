import { errorMessageKey, hasErrorCode } from '@/core/api/errors'

export function claimFailureMessageKey(error: unknown): string {
  return hasErrorCode(error, 'MEMBER_NOT_FOUND') ? 'members.nameJustTaken' : errorMessageKey(error)
}
