import type { GroupBalances, MemberBalance, SuggestedPayment } from '@/core/services/balances/balancesService'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import type { GroupMember } from '@/core/services/groups/membersService'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import {
  expensePeople,
  personOf,
  type ExpensePerson,
  type FormerPerson,
} from '@/features/expenses/shared/expensePeople'
import type { PaymentToRecord } from '@/features/settlements/record/paymentToRecord'
import type { Currency } from '@/shared/utils/formatMoney'

export type OwedLine =
  | { kind: 'youOwe'; names: string[] }
  | { kind: 'oweYou'; names: string[] }
  | { kind: 'allEven' }

export type BalanceCardRow =
  | { kind: 'youPay'; key: string; other: ExpensePerson; amountMinor: number; payment: PaymentToRecord }
  | { kind: 'paysYou'; key: string; other: ExpensePerson; amountMinor: number; payment: PaymentToRecord }
  | { kind: 'youPaid'; key: string; other: ExpensePerson; amountMinor: number; settlementId: string }

export interface BalanceCardView {
  currency: Currency
  myBalanceMinor: number
  owedLine: OwedLine
  rows: BalanceCardRow[]
}

export interface NeedsYouView {
  settlementId: string
  payer: ExpensePerson
  receiverName: string | null
  amountMinor: number
  currency: Currency
  createdAt: string
}

export interface BetweenOthersView {
  key: string
  payer: ExpensePerson
  receiver: ExpensePerson
  amountMinor: number
  currency: Currency
  isWaiting: boolean
  canMarkPaid: boolean
}

export interface ConfirmedPaymentView {
  settlementId: string
  payer: ExpensePerson
  receiver: ExpensePerson
  amountMinor: number
  currency: Currency
  createdAt: string
  canDelete: boolean
}

export interface EveryoneRowView {
  person: ExpensePerson
  balanceMinor: number
}

export interface EveryoneCardView {
  currency: Currency
  rows: EveryoneRowView[]
}

export interface BalancesView {
  me: GroupMember
  needsYou: NeedsYouView[]
  cards: BalanceCardView[]
  betweenOthers: BetweenOthersView[]
  hasSuggestedPayments: boolean
  everyone: EveryoneCardView[]
  payments: ConfirmedPaymentView[]
  hasOtherMembers: boolean
  isKvit: boolean
  isEmpty: boolean
}

export function balancesView(
  balances: GroupBalances,
  settlements: readonly Settlement[],
  members: readonly GroupMember[],
  myUserId: string,
): BalancesView {
  const me = members.find((member) => member.isYou)
  if (me === undefined) {
    throw new Error('Expected one member of the group to be you, found none')
  }
  const people = expensePeople(members, formerPeopleOf(balances, settlements))
  const pending = settlements.filter((settlement) => settlement.status === 'Pending')
  const pendingPairs = new Set<string>(
    pending.map((settlement) => pairKey(settlement.fromMemberId, settlement.toMemberId, settlement.currency)),
  )
  const myPending = pending.filter((settlement) => settlement.fromMemberId === me.id)
  const isNameOnly = (memberId: string): boolean =>
    members.some((member) => member.id === memberId && member.isNameOnly)
  const myself = personOf(people, me.id)

  const needsYou: NeedsYouView[] = pending
    .filter((settlement) => settlement.toMemberId === me.id || (me.isOwner && isNameOnly(settlement.toMemberId)))
    .map((settlement): NeedsYouView => ({
      settlementId: settlement.id,
      payer: personOf(people, settlement.fromMemberId),
      receiverName: settlement.toMemberId === me.id ? null : settlement.toName,
      amountMinor: settlement.amountMinor,
      currency: settlement.currency,
      createdAt: settlement.createdAt,
    }))

  const cardCurrencies: Currency[] = groupCurrencies.filter(
    (currency) =>
      balances.currencies.some((entry) => entry.currency === currency) ||
      myPending.some((settlement) => settlement.currency === currency),
  )

  const cards: BalanceCardView[] = cardCurrencies.map((currency): BalanceCardView => {
    const entry = balances.currencies.find((candidate) => candidate.currency === currency)
    const payments: SuggestedPayment[] = entry?.payments ?? []
    const myPayments = payments.filter(
      (payment) => payment.fromMemberId === me.id || payment.toMemberId === me.id,
    )
    const suggestedRows: BalanceCardRow[] = myPayments
      .filter((payment) => !pendingPairs.has(pairKey(payment.fromMemberId, payment.toMemberId, currency)))
      .map((payment): BalanceCardRow => {
        if (payment.fromMemberId === me.id) {
          const receiver = personOf(people, payment.toMemberId)
          return {
            kind: 'youPay',
            key: `pay-${payment.toMemberId}`,
            other: receiver,
            amountMinor: payment.amountMinor,
            payment: { payer: myself, receiver, amountMinor: payment.amountMinor, currency },
          }
        }
        const payer = personOf(people, payment.fromMemberId)
        return {
          kind: 'paysYou',
          key: `get-${payment.fromMemberId}`,
          other: payer,
          amountMinor: payment.amountMinor,
          payment: { payer, receiver: myself, amountMinor: payment.amountMinor, currency },
        }
      })
    const pendingRows: BalanceCardRow[] = myPending
      .filter((settlement) => settlement.currency === currency)
      .map((settlement): BalanceCardRow => ({
        kind: 'youPaid',
        key: settlement.id,
        other: personOf(people, settlement.toMemberId),
        amountMinor: settlement.amountMinor,
        settlementId: settlement.id,
      }))
    return {
      currency,
      myBalanceMinor: entry === undefined ? 0 : myBalanceOf(entry.balances, me.id, currency),
      owedLine: owedLineOf(myPayments, me.id, people),
      rows: [...suggestedRows, ...pendingRows],
    }
  })

  const betweenOthers: BetweenOthersView[] = balances.currencies.flatMap((entry) =>
    entry.payments
      .filter((payment) => payment.fromMemberId !== me.id && payment.toMemberId !== me.id)
      .map((payment): BetweenOthersView => {
        const isWaiting: boolean = pendingPairs.has(pairKey(payment.fromMemberId, payment.toMemberId, entry.currency))
        return {
          key: `${entry.currency}-${payment.fromMemberId}-${payment.toMemberId}`,
          payer: personOf(people, payment.fromMemberId),
          receiver: personOf(people, payment.toMemberId),
          amountMinor: payment.amountMinor,
          currency: entry.currency,
          isWaiting,
          canMarkPaid:
            me.isOwner && !isWaiting && (isNameOnly(payment.fromMemberId) || isNameOnly(payment.toMemberId)),
        }
      }),
  )

  const everyone: EveryoneCardView[] = balances.currencies.map((entry): EveryoneCardView => ({
    currency: entry.currency,
    rows: entry.balances
      .filter((row) => row.isCurrent || row.balanceMinor !== 0)
      .map((row): EveryoneRowView => ({ person: personOf(people, row.memberId), balanceMinor: row.balanceMinor })),
  }))

  const payments: ConfirmedPaymentView[] = settlements
    .filter((settlement) => settlement.status === 'Confirmed')
    .sort((first, second) => Date.parse(second.createdAt) - Date.parse(first.createdAt))
    .map((settlement): ConfirmedPaymentView => ({
      settlementId: settlement.id,
      payer: personOf(people, settlement.fromMemberId),
      receiver: personOf(people, settlement.toMemberId),
      amountMinor: settlement.amountMinor,
      currency: settlement.currency,
      createdAt: settlement.createdAt,
      canDelete: me.isOwner || settlement.recordedByUserId === myUserId,
    }))

  return {
    me,
    needsYou,
    cards,
    betweenOthers,
    hasSuggestedPayments: balances.currencies.some((entry) => entry.payments.length > 0),
    everyone,
    payments,
    hasOtherMembers: members.some((member) => !member.isYou),
    isKvit: balances.isEveryoneKvit && balances.currencies.length > 0,
    isEmpty: cards.length === 0 && needsYou.length === 0,
  }
}

function formerPeopleOf(balances: GroupBalances, settlements: readonly Settlement[]): FormerPerson[] {
  const fromBalances: FormerPerson[] = balances.currencies.flatMap((entry) =>
    entry.balances.map((row) => ({ memberId: row.memberId, name: row.name })),
  )
  const fromSettlements: FormerPerson[] = settlements.flatMap((settlement) => [
    { memberId: settlement.fromMemberId, name: settlement.fromName },
    { memberId: settlement.toMemberId, name: settlement.toName },
  ])
  return [...fromBalances, ...fromSettlements]
}

function myBalanceOf(rows: readonly MemberBalance[], myMemberId: string, currency: Currency): number {
  const row = rows.find((candidate) => candidate.memberId === myMemberId)
  if (row === undefined) {
    throw new Error(`Expected a balance for you (member ${myMemberId}) in ${currency}, found none`)
  }
  return row.balanceMinor
}

function owedLineOf(
  myPayments: readonly SuggestedPayment[],
  myMemberId: string,
  people: readonly ExpensePerson[],
): OwedLine {
  const iPay = myPayments.filter((payment) => payment.fromMemberId === myMemberId)
  if (iPay.length > 0) {
    return { kind: 'youOwe', names: iPay.map((payment) => personOf(people, payment.toMemberId).name) }
  }
  const paysMe = myPayments.filter((payment) => payment.toMemberId === myMemberId)
  if (paysMe.length > 0) {
    return { kind: 'oweYou', names: paysMe.map((payment) => personOf(people, payment.fromMemberId).name) }
  }
  return { kind: 'allEven' }
}

function pairKey(fromMemberId: string, toMemberId: string, currency: Currency): string {
  return `${fromMemberId}|${toMemberId}|${currency}`
}
