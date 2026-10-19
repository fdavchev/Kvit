import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { unarchivePersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { refreshPersonalCategories } from '../personalCategoryCache'

export function useUnarchivePersonalCategory(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: unarchivePersonalCategory,
    onSuccess: () => refreshPersonalCategories(queryClient),
  })
}
