import type { QueryClient } from '@tanstack/react-query'
import { catchUpRecurringIncomes, type RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { personalEntriesFamilyKey, refreshPersonalEntries } from '../entries/personalEntryCache'

export const recurringIncomesQueryKey = ['personal', 'recurring-incomes'] as const

export const catchUpFailedMessageKey = 'budget.recurring.catchUpFailed'

export function liveRecurringIncome(
  opened: RecurringIncome,
  incomes: readonly RecurringIncome[] | undefined,
): RecurringIncome {
  return incomes?.find((income) => income.id === opened.id) ?? opened
}

export function refreshRecurringIncomes(queryClient: QueryClient): Promise<void> {
  return queryClient.invalidateQueries({ queryKey: recurringIncomesQueryKey })
}

export async function refreshEntriesMadeByCatchUp(queryClient: QueryClient): Promise<void> {
  await queryClient.cancelQueries({ queryKey: personalEntriesFamilyKey })
  await refreshPersonalEntries(queryClient)
}

async function refreshAfterCatchUp(queryClient: QueryClient): Promise<void> {
  await queryClient.cancelQueries({ queryKey: recurringIncomesQueryKey })
  await Promise.all([refreshEntriesMadeByCatchUp(queryClient), refreshRecurringIncomes(queryClient)])
}

export async function catchUpOnOpen(queryClient: QueryClient): Promise<void> {
  const created = await catchUpRecurringIncomes()
  if (created > 0) {
    await refreshAfterCatchUp(queryClient)
  }
}

export async function catchUpAfterChange(queryClient: QueryClient): Promise<boolean> {
  let isCaughtUp = true
  try {
    await catchUpRecurringIncomes()
  } catch (error) {
    console.error('The catch-up after saving a repeating income failed; the change itself was saved', error)
    isCaughtUp = false
  }
  await refreshAfterCatchUp(queryClient)
  return isCaughtUp
}
