import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readDate,
  readDateOrNull,
  readList,
  readObject,
  readOneOf,
  readText,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export const settlementStatuses = ['Pending', 'Confirmed', 'Rejected', 'Cancelled'] as const

export type SettlementStatus = (typeof settlementStatuses)[number]

export interface Settlement {
  id: string
  fromMemberId: string
  fromName: string
  toMemberId: string
  toName: string
  amountMinor: number
  currency: Currency
  status: SettlementStatus
  recordedByUserId: string
  createdAt: string
  resolvedAt: string | null
}

export interface SettlementInput {
  clientRequestId: string
  fromMemberId: string
  toMemberId: string
  amountMinor: number
  currency: Currency
}

export async function getSettlements(groupId: string): Promise<Settlement[]> {
  const what = 'the payments of the group'
  const fields = readObject(await apiRequest(endpoints.groupSettlements(groupId)), what)
  return readList(fields, 'settlements', what).map(parseSettlement)
}

export async function recordSettlement(groupId: string, input: SettlementInput): Promise<Settlement> {
  return parseSettlement(await apiRequest(endpoints.groupSettlements(groupId), jsonRequest('POST', input)))
}

export async function confirmSettlement(groupId: string, settlementId: string): Promise<void> {
  await apiRequest(endpoints.groupSettlementConfirm(groupId, settlementId), { method: 'POST' })
}

export async function rejectSettlement(groupId: string, settlementId: string): Promise<void> {
  await apiRequest(endpoints.groupSettlementReject(groupId, settlementId), { method: 'POST' })
}

export async function cancelSettlement(groupId: string, settlementId: string): Promise<void> {
  await apiRequest(endpoints.groupSettlementCancel(groupId, settlementId), { method: 'POST' })
}

export async function deleteSettlement(groupId: string, settlementId: string): Promise<void> {
  await apiRequest(endpoints.groupSettlement(groupId, settlementId), { method: 'DELETE' })
}

export async function restoreSettlement(groupId: string, settlementId: string): Promise<void> {
  await apiRequest(endpoints.groupSettlementRestore(groupId, settlementId), { method: 'POST' })
}

function parseSettlement(row: unknown): Settlement {
  const what = 'a payment'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    fromMemberId: readText(fields, 'fromMemberId', what),
    fromName: readText(fields, 'fromName', what),
    toMemberId: readText(fields, 'toMemberId', what),
    toName: readText(fields, 'toName', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    status: readOneOf(fields, 'status', settlementStatuses, what),
    recordedByUserId: readText(fields, 'recordedByUserId', what),
    createdAt: readDate(fields, 'createdAt', what),
    resolvedAt: readDateOrNull(fields, 'resolvedAt', what),
  }
}
