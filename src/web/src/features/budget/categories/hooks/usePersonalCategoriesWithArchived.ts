import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import {
  getPersonalCategoriesWithArchived,
  type PersonalCategory,
} from '@/core/services/personalCategories/personalCategoriesService'
import { personalCategoriesWithArchivedQueryKey } from '../personalCategoryCache'

export function usePersonalCategoriesWithArchived(): UseQueryResult<PersonalCategory[]> {
  return useQuery({
    queryKey: personalCategoriesWithArchivedQueryKey,
    queryFn: getPersonalCategoriesWithArchived,
  })
}
