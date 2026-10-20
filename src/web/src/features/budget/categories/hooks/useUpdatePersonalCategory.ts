import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  updatePersonalCategory,
  type PersonalCategoryFields,
} from '@/core/services/personalCategories/personalCategoriesService'
import { refreshPersonalCategories } from '../personalCategoryCache'
import type { PersonalCategorySave } from './useCreatePersonalCategory'
import { useSaveOnce } from './useSaveOnce'

export function useUpdatePersonalCategory(categoryId: string): PersonalCategorySave<void> {
  const queryClient = useQueryClient()
  const saving = useSaveOnce()
  const mutation = useMutation({
    mutationFn: (fields: PersonalCategoryFields) => updatePersonalCategory(categoryId, fields),
    onSuccess: () => refreshPersonalCategories(queryClient),
    onSettled: saving.finish,
  })

  return {
    save: (fields, onSaved) => {
      if (saving.tryStart()) {
        mutation.mutate(fields, { onSuccess: onSaved })
      }
    },
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
