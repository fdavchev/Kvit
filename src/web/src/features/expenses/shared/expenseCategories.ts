import type { Category } from '@/core/services/categories/categoriesService'

export function categoryOf(categories: readonly Category[], categoryId: string | null): Category | null {
  if (categoryId === null) {
    return null
  }
  const category = categories.find((candidate) => candidate.id === categoryId)
  if (category === undefined) {
    throw new Error(`Expected the category ${categoryId} to be one of the categories, found none`)
  }
  return category
}
