import { apiRequest } from '@/core/api/apiClient'
import { endpoints, type AdminStatsRequest } from '@/core/api/endpoints'
import {
  readCount,
  readCountOrNull,
  readList,
  readObject,
  readOneOf,
  readText,
  type Fields,
} from '@/core/services/readFields'
import { isCalendarDate } from '@/shared/utils/formatDate'

export const adminStatsGroupings = ['day', 'week', 'month', 'year'] as const

export type AdminStatsGrouping = (typeof adminStatsGroupings)[number]

export type AdminStatsQuery = AdminStatsRequest & { by: AdminStatsGrouping }

export interface AdminStatsAllTime {
  accounts: number
  guests: number
  groups: number
  expenses: number
  payments: number
}

export interface AdminStatsEvents {
  total: number
  previousTotal: number | null
  counts: number[]
}

export interface AdminStatsSignUps extends AdminStatsEvents {
  email: number
  google: number
  guest: number
  guestsUpgraded: number
}

export interface AdminStatsActivePeople {
  people: number
  previousPeople: number | null
  personDays: number
  previousPersonDays: number | null
  guests: number
  peopleCounts: number[]
  personDayCounts: number[]
}

export interface AdminStats {
  from: string
  to: string
  by: AdminStatsGrouping
  previousFrom: string | null
  previousTo: string | null
  buckets: string[]
  allTime: AdminStatsAllTime
  signUps: AdminStatsSignUps
  activePeople: AdminStatsActivePeople
  joinedViaInvite: AdminStatsEvents
  groupsCreated: AdminStatsEvents
  expensesAdded: AdminStatsEvents
  paymentsConfirmed: AdminStatsEvents
}

const what = 'the admin statistics'

export async function getAdminStats(query: AdminStatsQuery): Promise<AdminStats> {
  const fields = readObject(await apiRequest(endpoints.adminStats(query)), what)
  const stats: AdminStats = {
    from: readCalendarDate(fields, 'from', what),
    to: readCalendarDate(fields, 'to', what),
    by: readOneOf(fields, 'by', adminStatsGroupings, what),
    previousFrom: fields.previousFrom === null ? null : readCalendarDate(fields, 'previousFrom', what),
    previousTo: fields.previousTo === null ? null : readCalendarDate(fields, 'previousTo', what),
    buckets: readList(fields, 'buckets', what).map(checkBucket),
    allTime: parseAllTime(readSection(fields, 'allTime')),
    signUps: parseSignUps(readSection(fields, 'signUps')),
    activePeople: parseActivePeople(readSection(fields, 'activePeople')),
    joinedViaInvite: parseEvents(readSection(fields, 'joinedViaInvite'), 'joinedViaInvite'),
    groupsCreated: parseEvents(readSection(fields, 'groupsCreated'), 'groupsCreated'),
    expensesAdded: parseEvents(readSection(fields, 'expensesAdded'), 'expensesAdded'),
    paymentsConfirmed: parseEvents(readSection(fields, 'paymentsConfirmed'), 'paymentsConfirmed'),
  }
  checkOneCountPerBucket(stats)
  return stats
}

function checkOneCountPerBucket(stats: AdminStats): void {
  const countLists: [string, readonly number[]][] = [
    ['signUps.counts', stats.signUps.counts],
    ['activePeople.peopleCounts', stats.activePeople.peopleCounts],
    ['activePeople.personDayCounts', stats.activePeople.personDayCounts],
    ['joinedViaInvite.counts', stats.joinedViaInvite.counts],
    ['groupsCreated.counts', stats.groupsCreated.counts],
    ['expensesAdded.counts', stats.expensesAdded.counts],
    ['paymentsConfirmed.counts', stats.paymentsConfirmed.counts],
  ]
  for (const [name, counts] of countLists) {
    if (counts.length !== stats.buckets.length) {
      throw new Error(
        `Expected "${name}" of ${what} to hold one count for each of the ${stats.buckets.length} buckets, got ${counts.length}`,
      )
    }
  }
}

function readSection(fields: Fields, name: string): Fields {
  return readObject(fields[name], `"${name}" of ${what}`)
}

function parseAllTime(fields: Fields): AdminStatsAllTime {
  const part = `"allTime" of ${what}`
  return {
    accounts: readCount(fields, 'accounts', part),
    guests: readCount(fields, 'guests', part),
    groups: readCount(fields, 'groups', part),
    expenses: readCount(fields, 'expenses', part),
    payments: readCount(fields, 'payments', part),
  }
}

function parseEvents(fields: Fields, name: string): AdminStatsEvents {
  const part = `"${name}" of ${what}`
  return {
    total: readCount(fields, 'total', part),
    previousTotal: readCountOrNull(fields, 'previousTotal', part),
    counts: readCountList(fields, 'counts', part),
  }
}

function parseSignUps(fields: Fields): AdminStatsSignUps {
  const part = `"signUps" of ${what}`
  return {
    ...parseEvents(fields, 'signUps'),
    email: readCount(fields, 'email', part),
    google: readCount(fields, 'google', part),
    guest: readCount(fields, 'guest', part),
    guestsUpgraded: readCount(fields, 'guestsUpgraded', part),
  }
}

function parseActivePeople(fields: Fields): AdminStatsActivePeople {
  const part = `"activePeople" of ${what}`
  return {
    people: readCount(fields, 'people', part),
    previousPeople: readCountOrNull(fields, 'previousPeople', part),
    personDays: readCount(fields, 'personDays', part),
    previousPersonDays: readCountOrNull(fields, 'previousPersonDays', part),
    guests: readCount(fields, 'guests', part),
    peopleCounts: readCountList(fields, 'peopleCounts', part),
    personDayCounts: readCountList(fields, 'personDayCounts', part),
  }
}

function readCalendarDate(fields: Fields, name: string, part: string): string {
  const date = readText(fields, name, part)
  if (!isCalendarDate(date)) {
    throw new Error(`Expected "${name}" of ${part} to be a calendar date like 2026-11-04, got "${date}"`)
  }
  return date
}

function checkBucket(bucket: unknown): string {
  if (typeof bucket !== 'string' || !isCalendarDate(bucket)) {
    throw new Error(`Expected every item of "buckets" in ${what} to be a calendar date, got ${JSON.stringify(bucket)}`)
  }
  return bucket
}

function readCountList(fields: Fields, name: string, part: string): number[] {
  return readList(fields, name, part).map((count) => {
    if (typeof count !== 'number' || !Number.isSafeInteger(count) || count < 0) {
      throw new Error(`Expected every item of "${name}" in ${part} to be a whole number of 0 or more, got ${JSON.stringify(count)}`)
    }
    return count
  })
}
