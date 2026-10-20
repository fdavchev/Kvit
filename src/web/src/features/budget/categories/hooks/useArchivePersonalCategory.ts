import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { archivePersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { refreshPersonalCategories } from '../personalCategoryCache'

export function useArchivePersonalCategory(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: archivePersonalCategory,
    onSuccess: () => refreshPersonalCategories(queryClient),
  })
}
