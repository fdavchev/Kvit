import { hasErrorCode } from '@/core/api/errors'

const deadEndCodes: readonly string[] = ['GROUP_CLOSING', 'GROUP_FINISHED', 'INVITE_NOT_FOUND']

export function isJoinDeadEnd(error: Error | null): boolean {
  return deadEndCodes.some((code) => hasErrorCode(error, code))
}
