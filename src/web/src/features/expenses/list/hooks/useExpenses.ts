import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getExpenses, type ExpenseListRow } from '@/core/services/expenses/expensesService'
import { expensesQueryKey } from '../../shared/expenseCache'

export function useExpenses(groupId: string): UseQueryResult<ExpenseListRow[]> {
  return useQuery({ queryKey: expensesQueryKey(groupId), queryFn: () => getExpenses(groupId) })
}
