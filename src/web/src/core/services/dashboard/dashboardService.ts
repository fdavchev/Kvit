import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { parseActivityEvent, type ActivityEvent } from '@/core/services/activity/activityService'
import {
  groupCurrencies,
  groupKinds,
  groupStatuses,
  type GroupKind,
  type GroupStatus,
} from '@/core/services/groups/groupsService'
import {
  readCount,
  readList,
  readObject,
  readOneOf,
  readText,
  readWholeNumber,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

const needsYouTypes = ['payment', 'closing'] as const

export interface DashboardAmount {
  currency: Currency
  amountMinor: number
}

export interface DashboardTotals {
  owed: DashboardAmount[]
  owe: DashboardAmount[]
}

export interface DashboardGroup {
  id: string
  kind: GroupKind
  name: string
  emoji: string
  status: GroupStatus
  balances: DashboardAmount[]
}

export interface DashboardPaymentItem {
  type: 'payment'
  groupId: string
  groupName: string
  settlementId: string
  fromName: string
  amountMinor: number
  currency: Currency
}

export interface DashboardClosingItem {
  type: 'closing'
  groupId: string
  groupName: string
  startedByName: string
}

export type DashboardNeedsYouItem = DashboardPaymentItem | DashboardClosingItem

export interface DashboardActivityRow extends ActivityEvent {
  groupId: string
  groupName: string
}

export interface DashboardPlusTarget {
  groupId: string
}

export interface Dashboard {
  totals: DashboardTotals
  groups: DashboardGroup[]
  needsYou: DashboardNeedsYouItem[]
  recentActivity: DashboardActivityRow[]
  plusTarget: DashboardPlusTarget | null
}

type AmountReader = (fields: Fields, name: string, what: string) => number

export async function getDashboard(): Promise<Dashboard> {
  return parseDashboard(await apiRequest(endpoints.dashboard))
}

export async function recordActive(): Promise<void> {
  await apiRequest(endpoints.meActive, { method: 'POST' })
}

function parseDashboard(body: unknown): Dashboard {
  const what = 'the dashboard'
  const fields = readObject(body, what)
  return {
    totals: parseTotals(fields.totals),
    groups: readList(fields, 'groups', what).map(parseGroup),
    needsYou: readList(fields, 'needsYou', what).map(parseNeedsYouItem),
    recentActivity: readList(fields, 'recentActivity', what).map(parseActivityRow),
    plusTarget: parsePlusTarget(fields.plusTarget),
  }
}

function parseTotals(value: unknown): DashboardTotals {
  const what = 'the totals of the dashboard'
  const fields = readObject(value, `"totals" of the dashboard`)
  return {
    owed: readAmounts(fields, 'owed', what, readCount),
    owe: readAmounts(fields, 'owe', what, readCount),
  }
}

function readAmounts(fields: Fields, name: string, what: string, readAmount: AmountReader): DashboardAmount[] {
  const amountWhat = `an amount in "${name}" of ${what}`
  return readList(fields, name, what).map((row) => {
    const amount = readObject(row, amountWhat)
    return {
      currency: readOneOf(amount, 'currency', groupCurrencies, amountWhat),
      amountMinor: readAmount(amount, 'amountMinor', amountWhat),
    }
  })
}

function parseGroup(row: unknown): DashboardGroup {
  const what = 'a group of the dashboard'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    kind: readOneOf(fields, 'kind', groupKinds, what),
    name: readText(fields, 'name', what),
    emoji: readText(fields, 'emoji', what),
    status: readOneOf(fields, 'status', groupStatuses, what),
    balances: readAmounts(fields, 'balances', what, readWholeNumber),
  }
}

function parseNeedsYouItem(row: unknown): DashboardNeedsYouItem {
  const what = 'a "needs you" item of the dashboard'
  const fields = readObject(row, what)
  const type = readOneOf(fields, 'type', needsYouTypes, what)
  const groupId = readText(fields, 'groupId', what)
  const groupName = readText(fields, 'groupName', what)
  if (type === 'closing') {
    return { type, groupId, groupName, startedByName: readText(fields, 'startedByName', what) }
  }
  return {
    type,
    groupId,
    groupName,
    settlementId: readText(fields, 'settlementId', what),
    fromName: readText(fields, 'fromName', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
  }
}

function parseActivityRow(row: unknown): DashboardActivityRow {
  const what = 'an activity row of the dashboard'
  const fields = readObject(row, what)
  return {
    ...parseActivityEvent(row),
    groupId: readText(fields, 'groupId', what),
    groupName: readText(fields, 'groupName', what),
  }
}

function parsePlusTarget(value: unknown): DashboardPlusTarget | null {
  if (value === null) {
    return null
  }
  const what = '"plusTarget" of the dashboard'
  return { groupId: readText(readObject(value, what), 'groupId', what) }
}
