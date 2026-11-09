import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupStatuses, type GroupStatus } from '@/core/services/groups/groupsService'
import {
  readCount,
  readCountAboveZero,
  readDate,
  readDateOrNull,
  readObject,
  readOneOf,
  readText,
  readTextOrNull,
  readYesNo,
} from '@/core/services/readFields'

const ownerToolsLogActions = ['Suspended', 'Unsuspended', 'GroupClosed', 'GroupReopened', 'NameErased'] as const
const ownerToolsTargetKinds = ['person', 'group'] as const

export interface OwnerToolsPerson {
  id: string
  displayName: string
  email: string | null
  isGuest: boolean
  isSuspended: boolean
  isAdmin: boolean
  deletionPending: boolean
  isDeleted: boolean
  createdAt: string
  groupCount: number
}

export interface OwnerToolsGroup {
  id: string
  name: string
  emoji: string
  status: GroupStatus
  memberCount: number
  ownerName: string
  createdAt: string
  isDeleted: boolean
  closedByAdminAt: string | null
}

export interface OwnerToolsLogEntry {
  id: number
  createdAt: string
  adminName: string
  action: (typeof ownerToolsLogActions)[number]
  targetKind: (typeof ownerToolsTargetKinds)[number]
  targetLabel: string
  reason: string
}

const logLimit = 50

export async function findPeople(query: string): Promise<OwnerToolsPerson[]> {
  const body = await apiRequest(endpoints.adminToolsPeople(query))
  return readBodyList(body, 'the people found by the owner tools').map(parsePerson)
}

export async function suspendPerson(personId: string, reason: string): Promise<void> {
  await apiRequest(endpoints.adminToolsPersonSuspend(personId), jsonRequest('POST', { reason }))
}

export async function unsuspendPerson(personId: string, reason: string): Promise<void> {
  await apiRequest(endpoints.adminToolsPersonUnsuspend(personId), jsonRequest('POST', { reason }))
}

export async function eraseName(personId: string, reason: string): Promise<void> {
  await apiRequest(endpoints.adminToolsPersonEraseName(personId), jsonRequest('POST', { reason }))
}

export async function findGroup(groupId: string): Promise<OwnerToolsGroup> {
  return parseGroup(await apiRequest(endpoints.adminToolsGroup(groupId)))
}

export async function closeGroup(groupId: string, reason: string): Promise<void> {
  await apiRequest(endpoints.adminToolsGroupClose(groupId), jsonRequest('POST', { reason }))
}

export async function reopenGroup(groupId: string, reason: string): Promise<void> {
  await apiRequest(endpoints.adminToolsGroupReopen(groupId), jsonRequest('POST', { reason }))
}

export async function loadLog(): Promise<OwnerToolsLogEntry[]> {
  const body = await apiRequest(endpoints.adminToolsLog(logLimit))
  return readBodyList(body, 'the owner tools log').map(parseLogEntry)
}

function readBodyList(body: unknown, what: string): unknown[] {
  if (!Array.isArray(body)) {
    throw new Error(`Expected ${what} as a list, got ${JSON.stringify(body)}`)
  }
  return body
}

function parsePerson(row: unknown): OwnerToolsPerson {
  const what = 'a person found by the owner tools'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    displayName: readText(fields, 'displayName', what),
    email: readTextOrNull(fields, 'email', what),
    isGuest: readYesNo(fields, 'isGuest', what),
    isSuspended: readYesNo(fields, 'isSuspended', what),
    isAdmin: readYesNo(fields, 'isAdmin', what),
    deletionPending: readYesNo(fields, 'deletionPending', what),
    isDeleted: readYesNo(fields, 'isDeleted', what),
    createdAt: readDate(fields, 'createdAt', what),
    groupCount: readCount(fields, 'groupCount', what),
  }
}

function parseGroup(body: unknown): OwnerToolsGroup {
  const what = 'the group found by the owner tools'
  const fields = readObject(body, what)
  return {
    id: readText(fields, 'id', what),
    name: readText(fields, 'name', what),
    emoji: readText(fields, 'emoji', what),
    status: readOneOf(fields, 'status', groupStatuses, what),
    memberCount: readCount(fields, 'memberCount', what),
    ownerName: readText(fields, 'ownerName', what),
    createdAt: readDate(fields, 'createdAt', what),
    isDeleted: readYesNo(fields, 'isDeleted', what),
    closedByAdminAt: readDateOrNull(fields, 'closedByAdminAt', what),
  }
}

function parseLogEntry(row: unknown): OwnerToolsLogEntry {
  const what = 'an entry of the owner tools log'
  const fields = readObject(row, what)
  return {
    id: readCountAboveZero(fields, 'id', what),
    createdAt: readDate(fields, 'createdAt', what),
    adminName: readText(fields, 'adminName', what),
    action: readOneOf(fields, 'action', ownerToolsLogActions, what),
    targetKind: readOneOf(fields, 'targetKind', ownerToolsTargetKinds, what),
    targetLabel: readText(fields, 'targetLabel', what),
    reason: readText(fields, 'reason', what),
  }
}
