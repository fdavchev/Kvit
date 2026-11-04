import type { AdminStats } from '@/core/services/adminStats/adminStatsService'

export const adminStatsNow = new Date('2026-11-04T10:00:00Z')

export const adminStatsPath = '/api/admin/stats'

export const defaultStatsUrl = `${adminStatsPath}?from=2026-11-01&to=2026-11-04&by=day`

export const novemberStats: AdminStats = {
  from: '2026-11-01',
  to: '2026-11-04',
  by: 'day',
  previousFrom: '2026-10-28',
  previousTo: '2026-10-31',
  buckets: ['2026-11-01', '2026-11-02', '2026-11-03', '2026-11-04'],
  allTime: { accounts: 58, guests: 6, groups: 14, expenses: 412, payments: 96 },
  signUps: {
    total: 9,
    previousTotal: 6,
    counts: [2, 3, 0, 4],
    email: 4,
    google: 3,
    guest: 2,
    guestsUpgraded: 1,
  },
  activePeople: {
    people: 21,
    previousPeople: 21,
    personDays: 21,
    previousPersonDays: 21,
    guests: 4,
    peopleCounts: [5, 6, 4, 6],
    personDayCounts: [5, 6, 4, 6],
  },
  joinedViaInvite: { total: 8, previousTotal: 10, counts: [1, 2, 3, 2] },
  groupsCreated: { total: 3, previousTotal: 0, counts: [0, 1, 2, 0] },
  expensesAdded: { total: 78, previousTotal: 52, counts: [10, 40, 0, 28] },
  paymentsConfirmed: { total: 12, previousTotal: 12, counts: [3, 3, 3, 3] },
}

export type EventSection = 'joinedViaInvite' | 'groupsCreated' | 'expensesAdded' | 'paymentsConfirmed'

export function statsWith(changes: Partial<AdminStats>): AdminStats {
  return { ...novemberStats, ...changes }
}

export function statsWithEvents(section: EventSection, total: number, previousTotal: number | null, counts: number[]): AdminStats {
  return statsWith({ [section]: { total, previousTotal, counts } })
}

export function statsWithSignUps(changes: Partial<AdminStats['signUps']>): AdminStats {
  return statsWith({ signUps: { ...novemberStats.signUps, ...changes } })
}

export function statsWithActivePeople(changes: Partial<AdminStats['activePeople']>): AdminStats {
  return statsWith({ activePeople: { ...novemberStats.activePeople, ...changes } })
}

export const weeklyStats: AdminStats = statsWith({
  by: 'week',
  buckets: ['2026-10-26', '2026-11-02'],
  signUps: { ...novemberStats.signUps, counts: [5, 4] },
  activePeople: {
    ...novemberStats.activePeople,
    personDays: 59,
    previousPersonDays: 50,
    peopleCounts: [12, 14],
    personDayCounts: [30, 29],
  },
  joinedViaInvite: { ...novemberStats.joinedViaInvite, counts: [3, 5] },
  groupsCreated: { ...novemberStats.groupsCreated, counts: [1, 2] },
  expensesAdded: { ...novemberStats.expensesAdded, counts: [30, 48] },
  paymentsConfirmed: { ...novemberStats.paymentsConfirmed, counts: [6, 6] },
})

export const monthlyStats: AdminStats = statsWith({
  from: '2025-12-01',
  to: '2026-11-04',
  by: 'month',
  previousFrom: '2024-12-01',
  previousTo: '2025-11-04',
  buckets: ['2026-10-01', '2026-11-01'],
  signUps: { ...novemberStats.signUps, counts: [5, 4] },
  activePeople: {
    ...novemberStats.activePeople,
    personDays: 59,
    previousPersonDays: 50,
    peopleCounts: [12, 14],
    personDayCounts: [30, 29],
  },
  joinedViaInvite: { ...novemberStats.joinedViaInvite, counts: [3, 5] },
  groupsCreated: { ...novemberStats.groupsCreated, counts: [1, 2] },
  expensesAdded: { ...novemberStats.expensesAdded, counts: [30, 48] },
  paymentsConfirmed: { ...novemberStats.paymentsConfirmed, counts: [6, 6] },
})

export const yearlyStats: AdminStats = statsWith({
  by: 'year',
  buckets: ['2025-01-01', '2026-01-01'],
  signUps: { ...novemberStats.signUps, counts: [5, 4] },
  activePeople: {
    ...novemberStats.activePeople,
    personDays: 59,
    previousPersonDays: 50,
    peopleCounts: [12, 14],
    personDayCounts: [30, 29],
  },
  joinedViaInvite: { ...novemberStats.joinedViaInvite, counts: [3, 5] },
  groupsCreated: { ...novemberStats.groupsCreated, counts: [1, 2] },
  expensesAdded: { ...novemberStats.expensesAdded, counts: [30, 48] },
  paymentsConfirmed: { ...novemberStats.paymentsConfirmed, counts: [6, 6] },
})

export const allTimeStats: AdminStats = statsWith({
  from: '2026-02-12',
  to: '2026-11-04',
  by: 'month',
  previousFrom: null,
  previousTo: null,
  buckets: ['2026-10-01', '2026-11-01'],
  signUps: { ...novemberStats.signUps, previousTotal: null, counts: [5, 4] },
  activePeople: {
    ...novemberStats.activePeople,
    previousPeople: null,
    previousPersonDays: null,
    peopleCounts: [12, 14],
    personDayCounts: [30, 29],
  },
  joinedViaInvite: { total: 8, previousTotal: null, counts: [3, 5] },
  groupsCreated: { total: 3, previousTotal: null, counts: [1, 2] },
  expensesAdded: { total: 78, previousTotal: null, counts: [30, 48] },
  paymentsConfirmed: { total: 12, previousTotal: null, counts: [6, 6] },
})

export function statsWithDailyBuckets(count: number): AdminStats {
  const firstDay = Date.UTC(2026, 7, 4)
  const dayInMilliseconds = 24 * 60 * 60 * 1000
  const buckets = Array.from({ length: count }, (_, index) => new Date(firstDay + index * dayInMilliseconds).toISOString().slice(0, 10))
  const counts = buckets.map((_, index) => (index % 7 === 3 ? 0 : (index * 13) % 40))
  return statsWith({
    from: buckets[0],
    to: buckets[count - 1],
    buckets,
    signUps: { ...novemberStats.signUps, counts },
    activePeople: { ...novemberStats.activePeople, peopleCounts: counts, personDayCounts: counts },
    joinedViaInvite: { ...novemberStats.joinedViaInvite, counts },
    groupsCreated: { ...novemberStats.groupsCreated, counts },
    expensesAdded: { ...novemberStats.expensesAdded, counts },
    paymentsConfirmed: { ...novemberStats.paymentsConfirmed, counts },
  })
}
