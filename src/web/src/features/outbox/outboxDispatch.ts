import type { QueryClient } from '@tanstack/react-query'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import {
  createExpense,
  createOneBill,
  type ExpenseInput,
  type OneBillInput,
} from '@/core/services/expenses/expensesService'
import { addPersonalEntry, type NewPersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { recordSettlement, type SettlementInput } from '@/core/services/settlements/settlementsService'
import { refreshPersonalEntries } from '@/features/budget/entries/personalEntryCache'
import { refreshAfterExpenseChange } from '@/features/expenses/shared/expenseCache'
import { refreshGroupList } from '@/features/groups/shared/groupCache'
import { refreshAfterSettlementChange } from '@/features/settlements/shared/settlementCache'

export async function sendQueuedItem(item: OutboxItem): Promise<unknown> {
  switch (item.kind) {
    case 'expense':
      return createExpense(groupIdOf(item), payloadOf<ExpenseInput>(item))
    case 'oneBill':
      return createOneBill(payloadOf<OneBillInput>(item))
    case 'personalEntry':
      return addPersonalEntry(payloadOf<NewPersonalEntry>(item))
    case 'settlement':
      return recordSettlement(groupIdOf(item), payloadOf<SettlementInput>(item))
    default:
      throw unknownKindError(item.kind, item.id)
  }
}

export async function refreshAfterQueuedItem(queryClient: QueryClient, item: OutboxItem): Promise<void> {
  switch (item.kind) {
    case 'expense':
      return refreshAfterExpenseChange(queryClient, groupIdOf(item))
    case 'oneBill':
      return refreshGroupList(queryClient)
    case 'personalEntry':
      return refreshPersonalEntries(queryClient)
    case 'settlement':
      return refreshAfterSettlementChange(queryClient, groupIdOf(item))
    default:
      throw unknownKindError(item.kind, item.id)
  }
}

function groupIdOf(item: OutboxItem): string {
  if (item.groupId === null) {
    throw new Error(`The waiting ${item.kind} ${item.id} has no group, so it cannot be sent`)
  }
  return item.groupId
}

function payloadOf<Payload extends { clientRequestId: string }>(item: OutboxItem): Payload {
  const { payload } = item
  if (typeof payload !== 'object' || payload === null || Array.isArray(payload)) {
    throw new Error(`The waiting ${item.kind} ${item.id} has a payload that is not an object: ${JSON.stringify(payload)}`)
  }
  if (!('clientRequestId' in payload) || payload.clientRequestId !== item.id) {
    throw new Error(
      `The waiting ${item.kind} ${item.id} has a payload whose clientRequestId is not the id of the item: ${JSON.stringify(payload)}`,
    )
  }
  return payload as Payload
}

function unknownKindError(kind: never, id: string): Error {
  return new Error(`The waiting item ${id} has the unknown kind ${String(kind)}`)
}
