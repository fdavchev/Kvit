import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import {
  readCount,
  readCountOrNull,
  readDate,
  readDateOrNull,
  readList,
  readObject,
  readOneOf,
  readPositiveNumber,
  readText,
  readTextOrNull,
  readYesNo,
  type Fields,
} from '@/core/services/readFields'
import type { Currency } from '@/shared/utils/formatMoney'

export const splitTypes = ['Equal', 'Exact', 'Percentage', 'Shares'] as const
const historyEntryTypes = ['ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored'] as const

export type SplitType = (typeof splitTypes)[number]
export type HistoryEntryType = (typeof historyEntryTypes)[number]

export interface ExpenseListRow {
  id: string
  title: string | null
  amountMinor: number
  currency: Currency
  expenseDate: string
  categoryId: string | null
  paidByMemberId: string
  paidByName: string
  yourShareMinor: number | null
  canEdit: boolean
}

export interface ExpenseShare {
  memberId: string
  name: string
  inputValue: number
  shareMinor: number
}

export interface ExpenseChange {
  field: string
  old: string
  new: string
}

export interface ExpenseHistoryEntry {
  type: HistoryEntryType
  actorName: string
  createdAt: string
  changes: ExpenseChange[] | null
}

export interface ExpenseDetail {
  id: string
  groupId: string
  title: string | null
  note: string | null
  amountMinor: number
  currency: Currency
  expenseDate: string
  categoryId: string | null
  paidByMemberId: string
  paidByName: string
  splitType: SplitType
  mkdPerEur: number
  rateDate: string
  createdByUserId: string
  createdByName: string
  createdAt: string
  updatedAt: string | null
  canEdit: boolean
  shares: ExpenseShare[]
  history: ExpenseHistoryEntry[]
}

export interface DeletedExpenseRow {
  id: string
  title: string | null
  amountMinor: number
  currency: Currency
  expenseDate: string
  categoryId: string | null
  paidByName: string
  deletedAt: string
  restorableUntil: string
  canEdit: boolean
}

export interface SplitShareInput {
  memberId: string
  inputValue: number
}

export interface ExpenseChanges {
  title: string | null
  note: string | null
  amountMinor: number
  currency: Currency
  expenseDate: string
  categoryId: string | null
  paidByMemberId: string
  splitType: SplitType
  shares: SplitShareInput[]
}

export interface ExpenseInput extends ExpenseChanges {
  clientRequestId: string
}

export async function getExpenses(groupId: string): Promise<ExpenseListRow[]> {
  const what = 'the expenses of the group'
  const fields = readObject(await apiRequest(endpoints.groupExpenses(groupId)), what)
  return readList(fields, 'expenses', what).map(parseExpenseListRow)
}

export async function getExpense(groupId: string, expenseId: string): Promise<ExpenseDetail> {
  return parseExpenseDetail(await apiRequest(endpoints.groupExpense(groupId, expenseId)))
}

export async function createExpense(groupId: string, input: ExpenseInput): Promise<ExpenseDetail> {
  return parseExpenseDetail(
    await apiRequest(endpoints.groupExpenses(groupId), jsonRequest('POST', input)),
  )
}

export async function updateExpense(
  groupId: string,
  expenseId: string,
  changes: ExpenseChanges,
): Promise<void> {
  await apiRequest(endpoints.groupExpense(groupId, expenseId), jsonRequest('PUT', changes))
}

export async function deleteExpense(groupId: string, expenseId: string): Promise<void> {
  await apiRequest(endpoints.groupExpense(groupId, expenseId), { method: 'DELETE' })
}

export async function restoreExpense(groupId: string, expenseId: string): Promise<void> {
  await apiRequest(endpoints.groupExpenseRestore(groupId, expenseId), { method: 'POST' })
}

export async function getDeletedExpenses(groupId: string): Promise<DeletedExpenseRow[]> {
  const what = 'the deleted expenses of the group'
  const fields = readObject(await apiRequest(endpoints.groupExpensesDeleted(groupId)), what)
  return readList(fields, 'expenses', what).map(parseDeletedExpenseRow)
}

function parseExpenseListRow(row: unknown): ExpenseListRow {
  const what = 'an expense'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    title: readTextOrNull(fields, 'title', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    expenseDate: readDate(fields, 'expenseDate', what),
    categoryId: readTextOrNull(fields, 'categoryId', what),
    paidByMemberId: readText(fields, 'paidByMemberId', what),
    paidByName: readText(fields, 'paidByName', what),
    yourShareMinor: readCountOrNull(fields, 'yourShareMinor', what),
    canEdit: readYesNo(fields, 'canEdit', what),
  }
}

function parseExpenseDetail(body: unknown): ExpenseDetail {
  const what = 'the expense'
  const fields = readObject(body, what)
  return {
    id: readText(fields, 'id', what),
    groupId: readText(fields, 'groupId', what),
    title: readTextOrNull(fields, 'title', what),
    note: readTextOrNull(fields, 'note', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    expenseDate: readDate(fields, 'expenseDate', what),
    categoryId: readTextOrNull(fields, 'categoryId', what),
    paidByMemberId: readText(fields, 'paidByMemberId', what),
    paidByName: readText(fields, 'paidByName', what),
    splitType: readOneOf(fields, 'splitType', splitTypes, what),
    mkdPerEur: readPositiveNumber(fields, 'mkdPerEur', what),
    rateDate: readDate(fields, 'rateDate', what),
    createdByUserId: readText(fields, 'createdByUserId', what),
    createdByName: readText(fields, 'createdByName', what),
    createdAt: readDate(fields, 'createdAt', what),
    updatedAt: readDateOrNull(fields, 'updatedAt', what),
    canEdit: readYesNo(fields, 'canEdit', what),
    shares: readList(fields, 'shares', what).map(parseExpenseShare),
    history: readList(fields, 'history', what).map(parseHistoryEntry),
  }
}

function parseExpenseShare(row: unknown): ExpenseShare {
  const what = 'a person in the split'
  const fields = readObject(row, what)
  return {
    memberId: readText(fields, 'memberId', what),
    name: readText(fields, 'name', what),
    inputValue: readCount(fields, 'inputValue', what),
    shareMinor: readCount(fields, 'shareMinor', what),
  }
}

function parseHistoryEntry(row: unknown): ExpenseHistoryEntry {
  const what = 'a history entry'
  const fields = readObject(row, what)
  return {
    type: readOneOf(fields, 'type', historyEntryTypes, what),
    actorName: readText(fields, 'actorName', what),
    createdAt: readDate(fields, 'createdAt', what),
    changes: readChanges(fields, what),
  }
}

function readChanges(fields: Fields, what: string): ExpenseChange[] | null {
  if (fields.changes === null) {
    return null
  }
  return readList(fields, 'changes', what).map(parseChange)
}

function parseChange(row: unknown): ExpenseChange {
  const what = 'a change'
  const fields = readObject(row, what)
  return {
    field: readText(fields, 'field', what),
    old: readText(fields, 'old', what),
    new: readText(fields, 'new', what),
  }
}

function parseDeletedExpenseRow(row: unknown): DeletedExpenseRow {
  const what = 'a deleted expense'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    title: readTextOrNull(fields, 'title', what),
    amountMinor: readCount(fields, 'amountMinor', what),
    currency: readOneOf(fields, 'currency', groupCurrencies, what),
    expenseDate: readDate(fields, 'expenseDate', what),
    categoryId: readTextOrNull(fields, 'categoryId', what),
    paidByName: readText(fields, 'paidByName', what),
    deletedAt: readDate(fields, 'deletedAt', what),
    restorableUntil: readDate(fields, 'restorableUntil', what),
    canEdit: readYesNo(fields, 'canEdit', what),
  }
}
