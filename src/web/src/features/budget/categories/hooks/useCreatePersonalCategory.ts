import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createPersonalCategory,
  type PersonalCategory,
  type PersonalCategoryFields,
} from '@/core/services/personalCategories/personalCategoriesService'
import { refreshPersonalCategories } from '../personalCategoryCache'
import { useSaveOnce } from './useSaveOnce'

export interface PersonalCategorySave<Saved> {
  save: (fields: PersonalCategoryFields, onSaved: (saved: Saved) => void) => void
  isPending: boolean
  error: Error | null
}

export function useCreatePersonalCategory(): PersonalCategorySave<PersonalCategory> {
  const queryClient = useQueryClient()
  const saving = useSaveOnce()
  const mutation = useMutation({
    mutationFn: createPersonalCategory,
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
