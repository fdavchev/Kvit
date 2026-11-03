import type { OutboxItem, OutboxItemDisplay, OutboxItemKind } from '@/core/outbox/outboxItem'
import type { ExpenseInput, OneBillInput } from '@/core/services/expenses/expensesService'
import type { NewPersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import type { SettlementInput } from '@/core/services/settlements/settlementsService'

interface ExpenseItemSource {
  groupId: string
  input: ExpenseInput
  paidByName: string
}

interface SettlementItemSource {
  groupId: string
  input: SettlementInput
  payerName: string
  receiverName: string
}

export function buildExpenseOutboxItem({ groupId, input, paidByName }: ExpenseItemSource): OutboxItem {
  return waitingItem('expense', input.clientRequestId, groupId, input, {
    title: input.title ?? undefined,
    amountMinor: input.amountMinor,
    currency: input.currency,
    date: input.expenseDate,
    categoryId: input.categoryId,
    paidByName,
  })
}

export function buildOneBillOutboxItem({ input }: { input: OneBillInput }): OutboxItem {
  const name = input.title ?? input.groupName ?? undefined
  return waitingItem('oneBill', input.clientRequestId, null, input, {
    title: name,
    groupName: name,
    amountMinor: input.amountMinor,
    currency: input.currency,
    date: input.expenseDate,
    categoryId: input.categoryId,
  })
}

export function buildPersonalEntryOutboxItem({ input }: { input: NewPersonalEntry }): OutboxItem {
  return waitingItem('personalEntry', input.clientRequestId, null, input, {
    title: input.note ?? undefined,
    amountMinor: input.amountMinor,
    currency: input.currency,
    date: input.entryDate,
    categoryId: input.kind === 'Spending' ? input.categoryId : null,
    entryKind: input.kind,
  })
}

export function buildSettlementOutboxItem({ groupId, input, payerName, receiverName }: SettlementItemSource): OutboxItem {
  return waitingItem('settlement', input.clientRequestId, groupId, input, {
    amountMinor: input.amountMinor,
    currency: input.currency,
    payerName,
    receiverName,
  })
}

function waitingItem(
  kind: OutboxItemKind,
  id: string,
  groupId: string | null,
  payload: unknown,
  display: OutboxItemDisplay,
): OutboxItem {
  return {
    id,
    kind,
    groupId,
    payload,
    display,
    createdAt: new Date().toISOString(),
    state: 'waiting',
    failure: null,
  }
}
