import type { PersonalEntryKind } from '@/core/services/personalEntries/personalEntriesService'

export type OutboxItemKind = 'expense' | 'oneBill' | 'personalEntry' | 'settlement'

export type OutboxItemState = 'waiting' | 'sending' | 'failed'

export interface OutboxFailure {
  errorCode: string | null
  httpStatus: number | null
}

export interface OutboxItemDisplay {
  title?: string
  amountMinor?: number
  currency?: string
  date?: string
  categoryId?: string | null
  paidByName?: string
  payerName?: string
  receiverName?: string
  entryKind?: PersonalEntryKind
  groupName?: string
}

export interface OutboxItem {
  id: string
  kind: OutboxItemKind
  groupId: string | null
  payload: unknown
  display: OutboxItemDisplay
  createdAt: string
  state: OutboxItemState
  failure: OutboxFailure | null
}

export type OutboxItemChange = Partial<Pick<OutboxItem, 'state' | 'failure'>>
