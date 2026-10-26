import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import { changeBudgetTotalCurrency } from '@/core/services/me/meService'
import type { Currency } from '@/shared/utils/formatMoney'

export function useChangeBudgetCurrency(): UseMutationResult<void, Error, Currency | null> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: changeBudgetTotalCurrency,
    onSuccess: (_answer, budgetTotalCurrency) => {
      updateCachedMe(queryClient, { budgetTotalCurrency })
    },
  })
}
