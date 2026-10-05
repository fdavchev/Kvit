import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import {
  readList,
  readObject,
  readOneOf,
  readText,
  readTextList,
  readTextOrNull,
} from '@/core/services/readFields'

const inviteStatuses = ['Open', 'AlreadyMember', 'Removed'] as const

export type InviteStatus = (typeof inviteStatuses)[number]

export interface UnclaimedName {
  id: string
  name: string
}

export interface InvitePreview {
  status: InviteStatus
  groupId: string | null
  name: string
  emoji: string
  memberNames: string[]
  unclaimedNames: UnclaimedName[]
}

export async function previewInvite(token: string): Promise<InvitePreview> {
  return parseInvitePreview(
    await apiRequest(endpoints.invitePreview, jsonRequest('POST', { token })),
  )
}

export async function joinGroup(token: string, claimMemberId?: string): Promise<string> {
  const body = claimMemberId === undefined ? { token } : { token, claimMemberId }
  const what = 'the joined group'
  const answer = await apiRequest(endpoints.inviteJoin, jsonRequest('POST', body))
  return readText(readObject(answer, what), 'groupId', what)
}

function parseInvitePreview(body: unknown): InvitePreview {
  const what = 'the invite'
  const fields = readObject(body, what)
  return {
    status: readOneOf(fields, 'status', inviteStatuses, what),
    groupId: readTextOrNull(fields, 'groupId', what),
    name: readText(fields, 'name', what),
    emoji: readText(fields, 'emoji', what),
    memberNames: readTextList(fields, 'memberNames', what),
    unclaimedNames: readList(fields, 'unclaimedNames', what).map(parseUnclaimedName),
  }
}

function parseUnclaimedName(row: unknown): UnclaimedName {
  const what = 'a name to claim'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    name: readText(fields, 'name', what),
  }
}
