import type { TFunction } from 'i18next'
import type { Category } from '@/core/services/categories/categoriesService'
import { categoryNameKey } from './expenseCategories'

export const noValueText = '—'

export function expenseTitle(title: string | null, category: Category | null, t: TFunction): string {
  if (title !== null) {
    return title
  }
  return category === null ? noValueText : t(categoryNameKey(category))
}
