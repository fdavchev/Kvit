import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import {
  getPersonalCategories,
  type PersonalCategory,
} from '@/core/services/personalCategories/personalCategoriesService'
import { personalCategoriesQueryKey } from '../personalCategoryCache'

export function usePersonalCategories(): UseQueryResult<PersonalCategory[]> {
  return useQuery({ queryKey: personalCategoriesQueryKey, queryFn: getPersonalCategories })
}
