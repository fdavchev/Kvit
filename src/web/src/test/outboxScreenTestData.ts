import type { OutboxFailure, OutboxItem, OutboxItemDisplay, OutboxItemState } from '@/core/outbox/outboxItem'
import { testGroupId } from './groupTestData'
import { testOutboxItem } from './outboxTestData'

export interface QueuedItemOptions {
  state?: OutboxItemState
  createdAt?: string
  groupId?: string
  failure?: OutboxFailure
  display?: Partial<OutboxItemDisplay>
}

export const otherGroupId = '9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d'

export const refusedByFinishedGroup: OutboxFailure = { errorCode: 'GROUP_FINISHED', httpStatus: 400 }

function stateAndFailure(options: QueuedItemOptions): Pick<OutboxItem, 'state' | 'failure'> {
  const state = options.state ?? 'waiting'
  return { state, failure: state === 'failed' ? (options.failure ?? refusedByFinishedGroup) : null }
}

export function queuedExpense(id: string, options: QueuedItemOptions = {}): OutboxItem {
  const display: OutboxItemDisplay = {
    title: 'Museum tickets',
    amountMinor: 150000,
    currency: 'MKD',
    date: '2026-10-06',
    categoryId: null,
    paidByName: 'Ana',
    ...options.display,
  }
  return testOutboxItem(id, {
    kind: 'expense',
    groupId: options.groupId ?? testGroupId,
    payload: {
      clientRequestId: id,
      title: display.title ?? null,
      note: null,
      amountMinor: display.amountMinor,
      currency: display.currency,
      expenseDate: display.date,
      categoryId: display.categoryId ?? null,
      paidByMemberId: 'member-1',
      splitType: 'Equal',
      shares: [{ memberId: 'member-1', inputValue: 1 }],
    },
    display,
    createdAt: options.createdAt ?? '2026-10-06T10:00:00.000Z',
    ...stateAndFailure(options),
  })
}

export function queuedPersonalEntry(id: string, options: QueuedItemOptions = {}): OutboxItem {
  const display: OutboxItemDisplay = {
    title: 'Bakery run',
    amountMinor: 70000,
    currency: 'MKD',
    date: '2026-10-19',
    categoryId: null,
    entryKind: 'Spending',
    ...options.display,
  }
  return testOutboxItem(id, {
    kind: 'personalEntry',
    groupId: null,
    payload: {
      clientRequestId: id,
      kind: display.entryKind,
      amountMinor: display.amountMinor,
      currency: display.currency,
      entryDate: display.date,
      categoryId: display.categoryId ?? null,
      note: display.title ?? null,
    },
    display,
    createdAt: options.createdAt ?? '2026-10-19T10:00:00.000Z',
    ...stateAndFailure(options),
  })
}

export function queuedSettlement(id: string, options: QueuedItemOptions = {}): OutboxItem {
  const display: OutboxItemDisplay = {
    amountMinor: 80000,
    currency: 'MKD',
    payerName: 'Marko',
    receiverName: 'Filip',
    ...options.display,
  }
  return testOutboxItem(id, {
    kind: 'settlement',
    groupId: options.groupId ?? testGroupId,
    payload: {
      clientRequestId: id,
      fromMemberId: 'member-1',
      toMemberId: 'member-2',
      amountMinor: display.amountMinor,
      currency: display.currency,
    },
    display,
    createdAt: options.createdAt ?? '2026-10-06T10:00:00.000Z',
    ...stateAndFailure({ failure: { errorCode: 'SETTLEMENT_NOT_ALLOWED', httpStatus: 403 }, ...options }),
  })
}

export function queuedOneBill(id: string, options: QueuedItemOptions = {}): OutboxItem {
  const display: OutboxItemDisplay = {
    title: 'Dinner at Rex',
    groupName: 'Dinner at Rex',
    amountMinor: 360000,
    currency: 'MKD',
    date: '2026-10-06',
    categoryId: null,
    ...options.display,
  }
  return testOutboxItem(id, {
    kind: 'oneBill',
    groupId: null,
    payload: {
      clientRequestId: id,
      title: display.title ?? null,
      groupName: display.groupName ?? null,
      amountMinor: display.amountMinor,
      currency: display.currency,
      expenseDate: display.date,
    },
    display,
    createdAt: options.createdAt ?? '2026-10-06T10:00:00.000Z',
    ...stateAndFailure(options),
  })
}

export function withoutDisplayFields(item: OutboxItem): OutboxItem {
  return { ...item, display: {} }
}
