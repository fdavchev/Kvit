import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getDeletedExpenses, type DeletedExpenseRow } from '@/core/services/expenses/expensesService'
import { deletedExpensesQueryKey } from '../../shared/expenseCache'

export function useDeletedExpenses(groupId: string): UseQueryResult<DeletedExpenseRow[]> {
  return useQuery({
    queryKey: deletedExpensesQueryKey(groupId),
    queryFn: () => getDeletedExpenses(groupId),
  })
}
