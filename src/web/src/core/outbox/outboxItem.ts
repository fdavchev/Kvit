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
