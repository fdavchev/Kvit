import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import {
  readList,
  readObject,
  readText,
  readTextOrNull,
  readYesNo,
} from '@/core/services/readFields'

export interface GroupMember {
  id: string
  name: string
  displayName: string
  userId: string | null
  isOwner: boolean
  isYou: boolean
  isNameOnly: boolean
  claimedName: string | null
  pictureUrl: string | null
}

export interface RemovedMember {
  id: string
  displayName: string
  pictureUrl: string | null
}

export interface GroupMembers {
  members: GroupMember[]
  removed: RemovedMember[]
  canClaimNames: boolean
}

export async function getMembers(groupId: string): Promise<GroupMembers> {
  return parseGroupMembers(await apiRequest(endpoints.groupMembers(groupId)))
}

export async function removeMember(groupId: string, memberId: string): Promise<void> {
  await apiRequest(endpoints.groupMember(groupId, memberId), { method: 'DELETE' })
}

export async function letBackIn(groupId: string, memberId: string): Promise<void> {
  await apiRequest(endpoints.groupMemberLetBackIn(groupId, memberId), { method: 'POST' })
}

export async function makeOwner(groupId: string, memberId: string): Promise<void> {
  await apiRequest(endpoints.groupOwner(groupId), jsonRequest('POST', { memberId }))
}

export async function claimName(groupId: string, memberId: string): Promise<void> {
  await apiRequest(endpoints.groupMemberClaim(groupId, memberId), { method: 'POST' })
}

export async function undoClaim(groupId: string, memberId: string): Promise<void> {
  await apiRequest(endpoints.groupMemberUndoClaim(groupId, memberId), { method: 'POST' })
}

export async function resetInviteLink(groupId: string): Promise<string> {
  return parseInviteToken(
    await apiRequest(endpoints.groupInviteReset(groupId), { method: 'POST' }),
  )
}

export async function undoResetInviteLink(groupId: string): Promise<string> {
  return parseInviteToken(
    await apiRequest(endpoints.groupInviteUndoReset(groupId), { method: 'POST' }),
  )
}

function parseGroupMembers(body: unknown): GroupMembers {
  const what = 'the members of the group'
  const fields = readObject(body, what)
  return {
    members: readList(fields, 'members', what).map(parseGroupMember),
    removed: readList(fields, 'removed', what).map(parseRemovedMember),
    canClaimNames: readYesNo(fields, 'canClaimNames', what),
  }
}

function parseGroupMember(row: unknown): GroupMember {
  const what = 'a member'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    name: readText(fields, 'name', what),
    displayName: readText(fields, 'displayName', what),
    userId: readTextOrNull(fields, 'userId', what),
    isOwner: readYesNo(fields, 'isOwner', what),
    isYou: readYesNo(fields, 'isYou', what),
    isNameOnly: readYesNo(fields, 'isNameOnly', what),
    claimedName: readTextOrNull(fields, 'claimedName', what),
    pictureUrl: readTextOrNull(fields, 'pictureUrl', what),
  }
}

function parseRemovedMember(row: unknown): RemovedMember {
  const what = 'a removed person'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    displayName: readText(fields, 'displayName', what),
    pictureUrl: readTextOrNull(fields, 'pictureUrl', what),
  }
}

function parseInviteToken(body: unknown): string {
  const what = 'the new invite link'
  return readText(readObject(body, what), 'inviteToken', what)
}
