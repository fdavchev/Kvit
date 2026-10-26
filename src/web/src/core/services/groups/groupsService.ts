import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import {
  readCount,
  readDate,
  readDateOrNull,
  readList,
  readObject,
  readOneOf,
  readText,
  readYesNo,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export const groupCurrencies: readonly Currency[] = ['MKD', 'EUR']
export const groupKinds = ['Group', 'OneBill'] as const
export const groupStatuses = ['Open', 'Closing', 'Finished'] as const

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

export interface ClosingPerson {
  id: string
  name: string
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
  closingStartedAt: string | null
  closingEndsAt: string | null
  closingConfirmedBy: ClosingPerson[]
  closingWaitingFor: ClosingPerson[]
  canStartClosing: boolean
  countsInBudget: boolean
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

export async function startClosing(groupId: string): Promise<void> {
  await apiRequest(endpoints.groupClosing(groupId), { method: 'POST' })
}

export async function confirmClosing(groupId: string): Promise<void> {
  await apiRequest(endpoints.groupClosingConfirm(groupId), { method: 'POST' })
}

export async function objectToClosing(groupId: string, reason?: string): Promise<void> {
  await apiRequest(endpoints.groupClosingObject(groupId), jsonRequest('POST', { reason: reason ?? null }))
}

export async function unlockGroup(groupId: string): Promise<void> {
  await apiRequest(endpoints.groupUnlock(groupId), { method: 'POST' })
}

export async function addMember(groupId: string, name: string): Promise<void> {
  await apiRequest(endpoints.groupMembers(groupId), jsonRequest('POST', { name }))
}

export async function changeCountsInBudget(groupId: string, countsInBudget: boolean): Promise<void> {
  await apiRequest(endpoints.groupCountsInBudget(groupId), jsonRequest('PUT', { countsInBudget }))
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
    closingStartedAt: readDateOrNull(fields, 'closingStartedAt', what),
    closingEndsAt: readDateOrNull(fields, 'closingEndsAt', what),
    closingConfirmedBy: readClosingPeople(fields, 'closingConfirmedBy', what),
    closingWaitingFor: readClosingPeople(fields, 'closingWaitingFor', what),
    canStartClosing: readYesNo(fields, 'canStartClosing', what),
    countsInBudget: readYesNo(fields, 'countsInBudget', what),
  }
}

function readClosingPeople(fields: Fields, name: string, what: string): ClosingPerson[] {
  const personWhat = `a person in "${name}" of ${what}`
  return readList(fields, name, what).map((row) => {
    const person = readObject(row, personWhat)
    return {
      id: readText(person, 'id', personWhat),
      name: readText(person, 'name', personWhat),
    }
  })
}
