import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  createRecurringIncome,
  editRecurringIncome,
  type NewRecurringIncome,
  type RecurringIncomeFields,
} from '@/core/services/recurringIncomes/recurringIncomesService'
import type { SaveRun } from '@/shared/hooks/saveRun'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { catchUpAfterChange, catchUpFailedMessageKey } from '../recurringIncomeCache'

export interface RecurringIncomeEdit {
  recurringIncomeId: string
  fields: RecurringIncomeFields
}

function useSaveThenCatchUp<Input>(
  save: (input: Input) => Promise<unknown>,
  oneAtATime: SaveOnce,
): SaveRun<Input> {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const mutation = useMutation<boolean, Error, Input>({
    mutationFn: async (input) => {
      await save(input)
      return catchUpAfterChange(queryClient)
    },
    onSettled: oneAtATime.finish,
  })

  return {
    run: (input, onDone, onRefused) => {
      if (oneAtATime.tryStart()) {
        mutation.mutate(input, {
          onSuccess: (isCaughtUp) => {
            onDone()
            if (!isCaughtUp) {
              toast.error(t(catchUpFailedMessageKey))
            }
          },
          onError: onRefused,
        })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}

export function useCreateRecurringIncome(oneAtATime: SaveOnce): SaveRun<NewRecurringIncome> {
  return useSaveThenCatchUp(createRecurringIncome, oneAtATime)
}

export function useEditRecurringIncome(oneAtATime: SaveOnce): SaveRun<RecurringIncomeEdit> {
  return useSaveThenCatchUp(
    ({ recurringIncomeId, fields }: RecurringIncomeEdit) => editRecurringIncome(recurringIncomeId, fields),
    oneAtATime,
  )
}
