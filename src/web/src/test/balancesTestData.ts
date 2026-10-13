import type {
  CurrencyBalances,
  GroupBalances,
  MemberBalance,
  SuggestedPayment,
} from '@/core/services/balances/balancesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import { groupPath } from './expenseTestHelpers'
import {
  anaMember,
  anaUserId,
  bojanRemoved,
  filipMember,
  grandmaMember,
  markoMember,
  petarMember,
} from './memberTestData'

export const balancesPath = `${groupPath}/balances`
export const settlementsPath = `${groupPath}/settlements`

export function settlementPathOf(settlementId: string): string {
  return `${settlementsPath}/${settlementId}`
}

export const settlementIds = {
  markoToFilip: 'c0000000-0000-4000-8000-000000000001',
  anaToFilip: 'c0000000-0000-4000-8000-000000000002',
  markoToGrandma: 'c0000000-0000-4000-8000-000000000003',
  markoToAna: 'c0000000-0000-4000-8000-000000000004',
  anaToFilipSecond: 'c0000000-0000-4000-8000-000000000005',
  markoToFilipSecond: 'c0000000-0000-4000-8000-000000000006',
} as const

export const twoHoursBeforeNow = '2026-10-06T08:00:00Z'
export const oneDayBeforeNow = '2026-10-05T09:00:00Z'

export function balanceRow(
  member: Pick<GroupMember, 'id' | 'displayName'>,
  balanceMinor: number,
  isCurrent: boolean = true,
): MemberBalance {
  return { memberId: member.id, name: member.displayName, balanceMinor, isCurrent }
}

export const bojanFormerMember = { id: bojanRemoved.id, displayName: bojanRemoved.displayName }

export function paymentOf(
  from: Pick<GroupMember, 'id'>,
  to: Pick<GroupMember, 'id'>,
  amountMinor: number,
): SuggestedPayment {
  return { fromMemberId: from.id, toMemberId: to.id, amountMinor }
}

export function settlementOf(changes: Partial<Settlement>): Settlement {
  return {
    id: settlementIds.markoToFilip,
    fromMemberId: markoMember.id,
    fromName: 'Marko',
    toMemberId: filipMember.id,
    toName: 'Filip',
    amountMinor: 80000,
    currency: 'MKD',
    status: 'Pending',
    recordedByUserId: anaUserId,
    createdAt: twoHoursBeforeNow,
    resolvedAt: null,
    ...changes,
  }
}

export function balancesOf(currencies: CurrencyBalances[], isEveryoneKvit: boolean = false): GroupBalances {
  return { isEveryoneKvit, currencies }
}

export const markoToFilipPayment: SuggestedPayment = paymentOf(markoMember, filipMember, 80000)
export const anaToFilipPayment: SuggestedPayment = paymentOf(anaMember, filipMember, 70000)

export const greeceTripMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 150000),
    balanceRow(anaMember, -70000),
    balanceRow(markoMember, -80000),
    balanceRow(grandmaMember, 0),
  ],
  payments: [markoToFilipPayment, anaToFilipPayment],
}

export const greeceTripBalances: GroupBalances = balancesOf([greeceTripMkd])

export const greeceTripEur: CurrencyBalances = {
  currency: 'EUR',
  balances: [
    balanceRow(filipMember, -2000),
    balanceRow(anaMember, 2000),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 0),
  ],
  payments: [paymentOf(filipMember, anaMember, 2000)],
}

export const greeceTripInTwoCurrencies: GroupBalances = balancesOf([greeceTripMkd, greeceTripEur])

export const markoOwesGrandmaMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 0),
    balanceRow(anaMember, 0),
    balanceRow(markoMember, -80000),
    balanceRow(grandmaMember, 80000),
  ],
  payments: [paymentOf(markoMember, grandmaMember, 80000)],
}

export const markoOwesGrandmaBalances: GroupBalances = balancesOf([markoOwesGrandmaMkd])

export const markoOwesAnaMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 0),
    balanceRow(anaMember, 80000),
    balanceRow(markoMember, -80000),
    balanceRow(grandmaMember, 0),
  ],
  payments: [paymentOf(markoMember, anaMember, 80000)],
}

export const markoOwesAnaBalances: GroupBalances = balancesOf([markoOwesAnaMkd])

export const fourPeopleMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 120000),
    balanceRow(anaMember, -20000),
    balanceRow(markoMember, -60000),
    balanceRow(grandmaMember, -40000),
  ],
  payments: [
    paymentOf(markoMember, filipMember, 60000),
    paymentOf(grandmaMember, filipMember, 40000),
    paymentOf(anaMember, filipMember, 20000),
  ],
}

export const fourPeopleBalances: GroupBalances = balancesOf([fourPeopleMkd])

export const filipIsOwedByThreeMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 30000),
    balanceRow(anaMember, -10000),
    balanceRow(markoMember, -10000),
    balanceRow(grandmaMember, -10000),
  ],
  payments: [
    paymentOf(anaMember, filipMember, 10000),
    paymentOf(markoMember, filipMember, 10000),
    paymentOf(grandmaMember, filipMember, 10000),
  ],
}

export const filipIsOwedByFourMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 40000),
    balanceRow(anaMember, -10000),
    balanceRow(markoMember, -10000),
    balanceRow(grandmaMember, -10000),
    balanceRow({ id: petarMember.id, displayName: petarMember.displayName }, -10000),
  ],
  payments: [
    paymentOf(anaMember, filipMember, 10000),
    paymentOf(markoMember, filipMember, 10000),
    paymentOf(grandmaMember, filipMember, 10000),
    paymentOf(petarMember, filipMember, 10000),
  ],
}

export const anaOwesTwoMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 40000),
    balanceRow(anaMember, -70000),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 30000),
  ],
  payments: [paymentOf(anaMember, filipMember, 40000), paymentOf(anaMember, grandmaMember, 30000)],
}

export const anaOwesThreeMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 40000),
    balanceRow(anaMember, -90000),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 30000),
    balanceRow({ id: petarMember.id, displayName: petarMember.displayName }, 20000),
  ],
  payments: [
    paymentOf(anaMember, filipMember, 40000),
    paymentOf(anaMember, grandmaMember, 30000),
    paymentOf(anaMember, petarMember, 20000),
  ],
}

export const everyoneEvenMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 0),
    balanceRow(anaMember, 0),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 0),
  ],
  payments: [],
}

export const everyoneIsKvitBalances: GroupBalances = balancesOf([everyoneEvenMkd], true)

export const everyoneEvenWithPendingPaymentBalances: GroupBalances = balancesOf([everyoneEvenMkd], false)

export const noExpensesYetBalances: GroupBalances = balancesOf([], true)

export const bojanOwesFilipMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 30000),
    balanceRow(anaMember, 0),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 0),
    balanceRow(bojanFormerMember, -30000, false),
  ],
  payments: [paymentOf(bojanFormerMember, filipMember, 30000)],
}

export const bojanLeftAtZeroMkd: CurrencyBalances = {
  currency: 'MKD',
  balances: [
    balanceRow(filipMember, 80000),
    balanceRow(anaMember, -80000),
    balanceRow(markoMember, 0),
    balanceRow(grandmaMember, 0),
    balanceRow(bojanFormerMember, 0, false),
  ],
  payments: [paymentOf(anaMember, filipMember, 80000)],
}

export const markoPaidFilipPending: Settlement = settlementOf({})

export const anaPaidFilipPending: Settlement = settlementOf({
  id: settlementIds.anaToFilip,
  fromMemberId: anaMember.id,
  fromName: 'Ana',
  amountMinor: 50000,
  createdAt: oneDayBeforeNow,
})

export const markoPaidGrandmaPending: Settlement = settlementOf({
  id: settlementIds.markoToGrandma,
  toMemberId: grandmaMember.id,
  toName: 'Grandma',
})

export const markoPaidAnaPending: Settlement = settlementOf({
  id: settlementIds.markoToAna,
  toMemberId: anaMember.id,
  toName: 'Ana',
})
