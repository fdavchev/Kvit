import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import { changeBudgetTotalCurrency } from '@/core/services/me/meService'
import { refreshPersonalEntries } from '@/features/budget/entries/personalEntryCache'
import type { Currency } from '@/shared/utils/formatMoney'

export function useChangeBudgetCurrency(): UseMutationResult<void, Error, Currency | null> {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: changeBudgetTotalCurrency,
    scope: { id: 'budgetTotalCurrency' },
    onSuccess: (_answer, budgetTotalCurrency) => {
      updateCachedMe(queryClient, { budgetTotalCurrency })
      return refreshPersonalEntries(queryClient)
    },
    onError: (error) => {
      console.error('Changing the budget total currency failed', error)
      toast.error(t(errorMessageKey(error)))
    },
  })
}
