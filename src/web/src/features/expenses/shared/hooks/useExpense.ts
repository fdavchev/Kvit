import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getExpense, type ExpenseDetail } from '@/core/services/expenses/expensesService'
import { expenseQueryKey } from '../expenseCache'

export function useExpense(groupId: string, expenseId: string): UseQueryResult<ExpenseDetail> {
  return useQuery({
    queryKey: expenseQueryKey(groupId, expenseId),
    queryFn: () => getExpense(groupId, expenseId),
  })
}
