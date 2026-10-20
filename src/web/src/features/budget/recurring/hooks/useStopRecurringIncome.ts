import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { restartRecurringIncome, stopRecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'
import { catchUpAfterChange, refreshRecurringIncomes } from '../recurringIncomeCache'

export const recurringIncomeNotFoundCode = 'RECURRING_INCOME_NOT_FOUND'

export interface RecurringIncomeStop {
  stop: (recurringIncomeId: string, onNoLongerActive?: () => void) => void
  isPending: boolean
  isStopping: (recurringIncomeId: string) => boolean
}

export function useStopRecurringIncome(oneAtATime: SaveOnce): RecurringIncomeStop {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const stopMutation = useMutation<void, Error, string>({
    mutationFn: stopRecurringIncome,
    onSuccess: () => refreshRecurringIncomes(queryClient),
    onSettled: oneAtATime.finish,
  })
  const restartMutation = useMutation<void, Error, string>({
    mutationFn: restartRecurringIncome,
    onSuccess: () => catchUpAfterChange(queryClient),
  })

  function undoStop(recurringIncomeId: string): void {
    void restartMutation
      .mutateAsync(recurringIncomeId)
      .then(() => {
        toast.success(t('budget.entries.restored'))
      })
      .catch((error: unknown) => {
        toast.error(t(errorMessageKey(error)))
      })
  }

  function stop(recurringIncomeId: string, onNoLongerActive?: () => void): void {
    if (!oneAtATime.tryStart()) {
      return
    }
    stopMutation.mutate(recurringIncomeId, {
      onSuccess: () => {
        showUndoToast(t('budget.recurring.stopped'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: () => undoStop(recurringIncomeId),
        })
        onNoLongerActive?.()
      },
      onError: (error) => {
        toast.error(t(errorMessageKey(error)))
        if (hasErrorCode(error, recurringIncomeNotFoundCode)) {
          void refreshRecurringIncomes(queryClient)
          void refreshPersonalEntries(queryClient)
          onNoLongerActive?.()
        }
      },
    })
  }

  return {
    stop,
    isPending: stopMutation.isPending,
    isStopping: (recurringIncomeId) => stopMutation.isPending && stopMutation.variables === recurringIncomeId,
  }
}
