import type { QueryClient } from '@tanstack/react-query'
import type { GroupBalances } from '@/core/services/balances/balancesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import type { Me } from '@/core/services/me/meService'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import type { Currency } from '@/shared/utils/formatMoney'
import type { GroupAppOptions } from './activityTestHelpers'
import {
  balanceRow,
  balancesOf,
  balancesPath,
  paymentOf,
  settlementOf,
  settlementsPath,
} from './balancesTestData'
import { testGroupId, groupOf } from './groupTestData'
import {
  anaMember,
  anaUserId,
  anaViewMembers,
  filipMember,
  grandmaMember,
  markoMember,
  petarMember,
} from './memberTestData'
import { jsonAnswer, type AnswerFactory } from './requestTestHelpers'
import { testMe } from './testMe'

export const paymentIds = {
  first: 'd0000000-0000-4000-8000-000000000001',
  second: 'd0000000-0000-4000-8000-000000000002',
  third: 'd0000000-0000-4000-8000-000000000003',
  fourth: 'd0000000-0000-4000-8000-000000000004',
} as const

export const anaMe: Me = {
  ...testMe,
  id: anaUserId,
  displayName: 'Ana',
  email: 'ana@example.com',
}

export function anaSignedIn(options: GroupAppOptions = {}): GroupAppOptions {
  return {
    members: anaViewMembers,
    group: options.group ?? groupOf({ isOwner: false }),
    ...options,
    answers: {
      'GET /api/me': jsonAnswer({ ...anaMe, language: options.language ?? 'en' }),
      ...options.answers,
    },
  }
}

export function onePaymentBalances(
  from: GroupMember,
  to: GroupMember,
  amountMinor: number,
  currency: Currency = 'MKD',
): GroupBalances {
  const people = [filipMember, anaMember, markoMember, grandmaMember, petarMember]
  const balances = people.map((person) =>
    balanceRow(person, person.id === to.id ? amountMinor : person.id === from.id ? -amountMinor : 0),
  )
  return balancesOf([{ currency, balances, payments: [paymentOf(from, to, amountMinor)] }])
}

export const soloKvitBalances: GroupBalances = balancesOf(
  [{ currency: 'MKD', balances: [balanceRow(filipMember, 0)], payments: [] }],
  true,
)

export function pendingPaymentOf(
  from: GroupMember,
  to: GroupMember,
  amountMinor: number,
  changes: Partial<Settlement> = {},
): Settlement {
  return settlementOf({
    id: paymentIds.first,
    fromMemberId: from.id,
    fromName: from.displayName,
    toMemberId: to.id,
    toName: to.displayName,
    amountMinor,
    ...changes,
  })
}

export function confirmedPaymentOf(
  from: GroupMember,
  to: GroupMember,
  amountMinor: number,
  changes: Partial<Settlement> = {},
): Settlement {
  return pendingPaymentOf(from, to, amountMinor, {
    status: 'Confirmed',
    resolvedAt: '2026-10-06T08:30:00Z',
    ...changes,
  })
}

export interface LiveGroup {
  balances: GroupBalances
  settlements: Settlement[]
}

export function liveAnswers(live: LiveGroup): Record<string, AnswerFactory> {
  return {
    [`GET ${balancesPath}`]: () => Response.json(live.balances),
    [`GET ${settlementsPath}`]: () => Response.json({ settlements: live.settlements }),
  }
}

export const activityKey = ['groups', testGroupId, 'activity'] as const
export const expensesKey = ['groups', testGroupId, 'expenses'] as const

export function seedActivityAndExpenses(queryClient: QueryClient): void {
  queryClient.setQueryData(activityKey, [])
  queryClient.setQueryData(expensesKey, [])
}
