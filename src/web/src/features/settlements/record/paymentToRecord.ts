import type { TFunction } from 'i18next'
import type { GroupMember } from '@/core/services/groups/membersService'
import type { ExpensePerson } from '@/features/expenses/shared/expensePeople'
import type { Currency } from '@/shared/utils/formatMoney'

export interface PaymentToRecord {
  payer: ExpensePerson
  receiver: ExpensePerson
  amountMinor: number
  currency: Currency
}

export function countsRightAway(
  payerMemberId: string,
  receiverMemberId: string,
  members: readonly GroupMember[],
): boolean {
  const me = members.find((member) => member.isYou)
  if (me === undefined) {
    throw new Error('Expected one member of the group to be you, found none')
  }
  const isReceiverNameOnly: boolean = members.some(
    (member) => member.id === receiverMemberId && member.isNameOnly,
  )
  if (receiverMemberId === me.id) {
    return true
  }
  if (payerMemberId === me.id) {
    return me.isOwner && isReceiverNameOnly
  }
  return isReceiverNameOnly
}

export function whenItCountsText(
  payerMemberId: string,
  receiverMemberId: string,
  receiverName: string,
  members: readonly GroupMember[],
  t: TFunction,
): string {
  return countsRightAway(payerMemberId, receiverMemberId, members)
    ? t('settlements.countsRightAway')
    : t('settlements.waitsUntil', { name: receiverName })
}
