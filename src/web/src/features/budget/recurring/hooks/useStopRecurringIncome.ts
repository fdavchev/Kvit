import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { restartRecurringIncome, stopRecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'
import { catchUpAfterChange, catchUpFailedMessageKey, refreshRecurringIncomes } from '../recurringIncomeCache'

export const recurringIncomeNotFoundCode = 'RECURRING_INCOME_NOT_FOUND'

export interface RecurringIncomeStop {
  stop: (recurringIncomeId: string, onNoLongerActive?: () => void) => void
  isPending: boolean
  isStopping: (recurringIncomeId: string) => boolean
}

interface StopRequest {
  recurringIncomeId: string
  onNoLongerActive?: () => void
}

export function useStopRecurringIncome(oneAtATime: SaveOnce): RecurringIncomeStop {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isMountedRef = useRef<boolean>(false)
  const restartMutation = useMutation<boolean, Error, string>({
    mutationFn: async (recurringIncomeId) => {
      await restartRecurringIncome(recurringIncomeId)
      return catchUpAfterChange(queryClient)
    },
  })
  const stopMutation = useMutation<void, Error, StopRequest>({
    mutationFn: ({ recurringIncomeId }) => stopRecurringIncome(recurringIncomeId),
    onSuccess: async (_nothing, { recurringIncomeId, onNoLongerActive }) => {
      showUndoToast(t('budget.recurring.stopped'), {
        danger: true,
        undoLabel: t('common.undo'),
        onUndo: () => undoStop(recurringIncomeId),
      })
      if (isMountedRef.current) {
        onNoLongerActive?.()
      }
      await refreshRecurringIncomes(queryClient)
    },
    onError: (error, { onNoLongerActive }) => {
      toast.error(t(errorMessageKey(error)))
      if (hasErrorCode(error, recurringIncomeNotFoundCode)) {
        void refreshRecurringIncomes(queryClient)
        void refreshPersonalEntries(queryClient)
        if (isMountedRef.current) {
          onNoLongerActive?.()
        }
      }
    },
    onSettled: oneAtATime.finish,
  })

  useEffect(() => {
    isMountedRef.current = true
    return () => {
      isMountedRef.current = false
    }
  }, [])

  function undoStop(recurringIncomeId: string): void {
    void restartMutation
      .mutateAsync(recurringIncomeId)
      .then((isCaughtUp) => {
        toast.success(t('budget.entries.restored'))
        if (!isCaughtUp) {
          toast.error(t(catchUpFailedMessageKey))
        }
      })
      .catch((error: unknown) => {
        toast.error(t(errorMessageKey(error)))
      })
  }

  function stop(recurringIncomeId: string, onNoLongerActive?: () => void): void {
    if (oneAtATime.tryStart()) {
      stopMutation.mutate({ recurringIncomeId, onNoLongerActive })
    }
  }

  return {
    stop,
    isPending: stopMutation.isPending,
    isStopping: (recurringIncomeId) =>
      stopMutation.isPending && stopMutation.variables?.recurringIncomeId === recurringIncomeId,
  }
}
