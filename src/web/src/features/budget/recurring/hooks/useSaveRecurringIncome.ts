import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createRecurringIncome,
  editRecurringIncome,
  type NewRecurringIncome,
  type RecurringIncome,
  type RecurringIncomeFields,
} from '@/core/services/recurringIncomes/recurringIncomesService'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import type { PersonalEntryRun } from '../../entries/hooks/useAddPersonalEntry'
import { catchUpAfterChange } from '../recurringIncomeCache'

export interface RecurringIncomeEdit {
  recurringIncomeId: string
  fields: RecurringIncomeFields
}

export function useCreateRecurringIncome(oneAtATime: SaveOnce): PersonalEntryRun<NewRecurringIncome> {
  const queryClient = useQueryClient()
  const mutation = useMutation<RecurringIncome, Error, NewRecurringIncome>({
    mutationFn: createRecurringIncome,
    onSuccess: () => catchUpAfterChange(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (income, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(income, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}

export function useEditRecurringIncome(oneAtATime: SaveOnce): PersonalEntryRun<RecurringIncomeEdit> {
  const queryClient = useQueryClient()
  const mutation = useMutation<RecurringIncome, Error, RecurringIncomeEdit>({
    mutationFn: ({ recurringIncomeId, fields }) => editRecurringIncome(recurringIncomeId, fields),
    onSuccess: () => catchUpAfterChange(queryClient),
    onSettled: oneAtATime.finish,
  })

  return {
    run: (edit, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(edit, { onSuccess: onDone, onError: onRefused })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
