import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readList,
  readObject,
  readOneOf,
  readText,
  readWholeNumber,
  readYesNo,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export interface MemberBalance {
  memberId: string
  name: string
  balanceMinor: number
  isCurrent: boolean
}

export interface SuggestedPayment {
  fromMemberId: string
  toMemberId: string
  amountMinor: number
}

export interface CurrencyBalances {
  currency: Currency
  balances: MemberBalance[]
  payments: SuggestedPayment[]
}

export interface GroupBalances {
  isEveryoneKvit: boolean
  currencies: CurrencyBalances[]
}

export async function getBalances(groupId: string): Promise<GroupBalances> {
  const what = 'the balances of the group'
  const fields = readObject(await apiRequest(endpoints.groupBalances(groupId)), what)
  return {
    isEveryoneKvit: readYesNo(fields, 'isEveryoneKvit', what),
    currencies: readList(fields, 'currencies', what).map(parseCurrencyBalances),
  }
}

function parseCurrencyBalances(row: unknown): CurrencyBalances {
  const what = 'the balances in one currency'
  const fields = readObject(row, what)
  return {
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    balances: readList(fields, 'balances', what).map(parseMemberBalance),
    payments: readList(fields, 'payments', what).map(parseSuggestedPayment),
  }
}

function parseMemberBalance(row: unknown): MemberBalance {
  const what = 'the balance of a member'
  const fields = readObject(row, what)
  return {
    memberId: readText(fields, 'memberId', what),
    name: readText(fields, 'name', what),
    balanceMinor: readWholeNumber(fields, 'balanceMinor', what),
    isCurrent: readYesNo(fields, 'isCurrent', what),
  }
}

function parseSuggestedPayment(row: unknown): SuggestedPayment {
  const what = 'a suggested payment'
  const fields = readObject(row, what)
  return {
    fromMemberId: readText(fields, 'fromMemberId', what),
    toMemberId: readText(fields, 'toMemberId', what),
    amountMinor: readCount(fields, 'amountMinor', what),
  }
}
