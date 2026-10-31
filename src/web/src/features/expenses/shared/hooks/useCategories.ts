import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getCategories, type Category } from '@/core/services/categories/categoriesService'
import { categoriesQueryKey } from '../expenseCache'

export function useCategories(enabled: boolean = true): UseQueryResult<Category[]> {
  return useQuery({
    queryKey: categoriesQueryKey,
    queryFn: getCategories,
    staleTime: Infinity,
    gcTime: Infinity,
    enabled,
  })
}
