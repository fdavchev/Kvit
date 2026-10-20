import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getRecurringIncomes, type RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { recurringIncomesQueryKey } from '../recurringIncomeCache'

export function useRecurringIncomes(): UseQueryResult<RecurringIncome[]> {
  return useQuery({ queryKey: recurringIncomesQueryKey, queryFn: getRecurringIncomes })
}
