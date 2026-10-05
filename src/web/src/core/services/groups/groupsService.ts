import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import {
  readCount,
  readDate,
  readList,
  readObject,
  readOneOf,
  readText,
  readYesNo,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export const groupCurrencies: readonly Currency[] = ['MKD', 'EUR']
const groupKinds = ['Group', 'OneBill'] as const
const groupStatuses = ['Open', 'Closing', 'Finished'] as const

export type GroupKind = (typeof groupKinds)[number]
export type GroupStatus = (typeof groupStatuses)[number]

export interface GroupListRow {
  id: string
  name: string
  emoji: string
  defaultCurrency: Currency
  memberCount: number
}

export interface DeletedGroupListRow extends GroupListRow {
  deletedAt: string
  restorableUntil: string
}

export interface GroupList {
  groups: GroupListRow[]
  finishedGroups: GroupListRow[]
  recentlyDeleted: DeletedGroupListRow[]
}

export interface Group {
  id: string
  kind: GroupKind
  name: string
  emoji: string
  defaultCurrency: Currency
  status: GroupStatus
  ownerUserId: string
  isOwner: boolean
  memberCount: number
  inviteToken: string
}

export interface GroupInput {
  name: string
  emoji: string
  currency: Currency
}

export async function getGroups(): Promise<GroupList> {
  return parseGroupList(await apiRequest(endpoints.groups))
}

export async function getGroup(groupId: string): Promise<Group> {
  return parseGroup(await apiRequest(endpoints.group(groupId)))
}

export async function createGroup(input: GroupInput): Promise<Group> {
  return parseGroup(await apiRequest(endpoints.groups, jsonRequest('POST', input)))
}

export async function updateGroup(groupId: string, input: GroupInput): Promise<void> {
  await apiRequest(endpoints.group(groupId), jsonRequest('PUT', input))
}

export async function deleteGroup(groupId: string): Promise<void> {
  await apiRequest(endpoints.group(groupId), { method: 'DELETE' })
}

export async function restoreGroup(groupId: string): Promise<void> {
  await apiRequest(endpoints.groupRestore(groupId), { method: 'POST' })
}

export async function leaveGroup(groupId: string): Promise<void> {
  await apiRequest(endpoints.groupLeave(groupId), { method: 'POST' })
}

export async function addMember(groupId: string, name: string): Promise<void> {
  await apiRequest(endpoints.groupMembers(groupId), jsonRequest('POST', { name }))
}

function parseGroupList(body: unknown): GroupList {
  const what = 'the group list'
  const fields = readObject(body, what)
  return {
    groups: readList(fields, 'groups', what).map((row) =>
      parseGroupListRow(row, 'an open group'),
    ),
    finishedGroups: readList(fields, 'finishedGroups', what).map((row) =>
      parseGroupListRow(row, 'a finished group'),
    ),
    recentlyDeleted: readList(fields, 'recentlyDeleted', what).map((row) =>
      parseDeletedGroupListRow(row),
    ),
  }
}

function parseGroupListRow(row: unknown, what: string): GroupListRow {
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    name: readText(fields, 'name', what),
    emoji: readText(fields, 'emoji', what),
    defaultCurrency: readOneOf(fields, 'defaultCurrency', groupCurrencies, what),
    memberCount: readCount(fields, 'memberCount', what),
  }
}

function parseDeletedGroupListRow(row: unknown): DeletedGroupListRow {
  const what = 'a deleted group'
  const fields = readObject(row, what)
  return {
    ...parseGroupListRow(row, what),
    deletedAt: readDate(fields, 'deletedAt', what),
    restorableUntil: readDate(fields, 'restorableUntil', what),
  }
}

function parseGroup(body: unknown): Group {
  const what = 'the group'
  const fields = readObject(body, what)
  return {
    id: readText(fields, 'id', what),
    kind: readOneOf(fields, 'kind', groupKinds, what),
    name: readText(fields, 'name', what),
    emoji: readText(fields, 'emoji', what),
    defaultCurrency: readOneOf(fields, 'defaultCurrency', groupCurrencies, what),
    status: readOneOf(fields, 'status', groupStatuses, what),
    ownerUserId: readText(fields, 'ownerUserId', what),
    isOwner: readYesNo(fields, 'isOwner', what),
    memberCount: readCount(fields, 'memberCount', what),
    inviteToken: readText(fields, 'inviteToken', what),
  }
}
