import type { Dashboard, DashboardActivityRow, DashboardGroup, DashboardNeedsYouItem } from '@/core/services/dashboard/dashboardService'
import { activityEventOf, type ActivityEventJson } from './activityTestData'
import { testGroupId } from './groupTestData'
import { anaUserId } from './memberTestData'

export const dashboardPath = '/api/dashboard'
export const activePath = '/api/me/active'

export const greeceGroupId = testGroupId
export const flatGroupId = '2a9f6c10-5d34-4b8e-8c71-0f3e2d1c4b5a'
export const longNameGroupId = '3b0a7d21-6e45-4c9f-9d82-1a4f3e2d5c6b'
export const dinnerBillGroupId = '4c1b8e32-7f56-4daf-8e93-2b5a4f3e6d7c'

export const markoPaymentSettlementId = 'c0000000-0000-4000-8000-000000000001'
export const secondPaymentSettlementId = 'c0000000-0000-4000-8000-000000000002'

export const longMacedonianGroupName = 'Роденденска-прослава-викенд-Охрид-со-многу-другари'

export const greeceDashboardGroup: DashboardGroup = {
  id: greeceGroupId,
  kind: 'Group',
  name: 'Greece trip',
  emoji: '\u{1F3D6}\u{FE0F}',
  status: 'Open',
  balances: [
    { currency: 'MKD', amountMinor: 185000 },
    { currency: 'EUR', amountMinor: 2000 },
  ],
}

export const flatDashboardGroup: DashboardGroup = {
  id: flatGroupId,
  kind: 'Group',
  name: 'Flat',
  emoji: '\u{1F3E0}',
  status: 'Closing',
  balances: [{ currency: 'MKD', amountMinor: -60000 }],
}

export const longNameDashboardGroup: DashboardGroup = {
  id: longNameGroupId,
  kind: 'Group',
  name: longMacedonianGroupName,
  emoji: '\u{1F382}',
  status: 'Open',
  balances: [],
}

export const dinnerBillDashboardGroup: DashboardGroup = {
  id: dinnerBillGroupId,
  kind: 'OneBill',
  name: 'Dinner',
  emoji: '\u{1F355}',
  status: 'Open',
  balances: [],
}

export const markoPaymentItem: DashboardNeedsYouItem = {
  type: 'payment',
  groupId: greeceGroupId,
  groupName: 'Greece trip',
  settlementId: markoPaymentSettlementId,
  fromName: 'Marko',
  amountMinor: 60000,
  currency: 'MKD',
}

export const danaPaymentItem: DashboardNeedsYouItem = {
  type: 'payment',
  groupId: greeceGroupId,
  groupName: 'Greece trip',
  settlementId: secondPaymentSettlementId,
  fromName: 'Dana',
  amountMinor: 1550,
  currency: 'EUR',
}

export const anaClosingItem: DashboardNeedsYouItem = {
  type: 'closing',
  groupId: flatGroupId,
  groupName: 'Flat',
  startedByName: 'Ana',
}

const activityNumber = '0f000000-0000-4000-8000-0000000000'

export function dashboardActivityRowOf(
  changes: Partial<ActivityEventJson>,
  groupId: string,
  groupName: string,
): DashboardActivityRow {
  return { ...activityEventOf(changes), groupId, groupName }
}

export const taxiAddedRow: DashboardActivityRow = dashboardActivityRowOf(
  {
    id: `${activityNumber}01`,
    type: 'ExpenseAdded',
    actorUserId: anaUserId,
    actorName: 'Ana',
    expenseId: 'e0000000-0000-4000-8000-000000000001',
    data: { title: 'Taxi', amountMinor: 90000, currency: 'MKD' },
    createdAt: '2026-10-06T08:00:00Z',
  },
  greeceGroupId,
  'Greece trip',
)

export const paymentRecordedRow: DashboardActivityRow = dashboardActivityRowOf(
  {
    id: `${activityNumber}02`,
    type: 'SettlementRecorded',
    actorName: 'Marko',
    data: {
      amountMinor: 60000,
      currency: 'MKD',
      fromMemberId: 'a1b2c3d4-0003-4aaa-8bbb-000000000003',
      toMemberId: 'a1b2c3d4-0001-4aaa-8bbb-000000000001',
      fromName: 'Marko',
      toName: 'Filip',
    },
    createdAt: '2026-10-06T05:00:00Z',
  },
  greeceGroupId,
  'Greece trip',
)

export const danaJoinedRow: DashboardActivityRow = dashboardActivityRowOf(
  {
    id: `${activityNumber}03`,
    type: 'MemberJoined',
    actorName: 'Dana',
    data: { name: 'Dana' },
    createdAt: '2026-10-05T09:00:00Z',
  },
  flatGroupId,
  'Flat',
)

export const expenseEditedRow: DashboardActivityRow = dashboardActivityRowOf(
  {
    id: `${activityNumber}04`,
    type: 'ExpenseEdited',
    actorName: 'Ana',
    expenseId: 'e0000000-0000-4000-8000-000000000002',
    changes: [{ field: 'amount', old: '90000', new: '120000' }],
    data: { currency: 'MKD' },
    createdAt: '2026-10-04T09:00:00Z',
  },
  longNameGroupId,
  longMacedonianGroupName,
)

export const fullDashboard: Dashboard = {
  totals: {
    owed: [
      { currency: 'MKD', amountMinor: 185000 },
      { currency: 'EUR', amountMinor: 2000 },
    ],
    owe: [{ currency: 'MKD', amountMinor: 60000 }],
  },
  groups: [greeceDashboardGroup, flatDashboardGroup, longNameDashboardGroup, dinnerBillDashboardGroup],
  needsYou: [anaClosingItem, markoPaymentItem],
  recentActivity: [taxiAddedRow, paymentRecordedRow, danaJoinedRow, expenseEditedRow],
  plusTarget: { groupId: greeceGroupId },
}

export const quietDashboard: Dashboard = { ...fullDashboard, needsYou: [] }

export const newUserDashboard: Dashboard = {
  totals: { owed: [], owe: [] },
  groups: [],
  needsYou: [],
  recentActivity: [],
  plusTarget: null,
}

export function dashboardOf(changes: Partial<Dashboard>): Dashboard {
  return { ...fullDashboard, ...changes }
}
