import type { TFunction } from 'i18next'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { categoryNameKey } from '@/shared/utils/categoryLooks'

export function personalCategoryName(category: PersonalCategory, t: TFunction): string {
  return category.isCustom ? category.name : t(categoryNameKey(category))
}
