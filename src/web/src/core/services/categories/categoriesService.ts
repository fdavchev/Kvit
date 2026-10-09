import { apiRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { readCount, readList, readObject, readText } from '@/core/services/readFields'

export interface Category {
  id: string
  key: string
  emoji: string
  color: string
  sortOrder: number
}

export async function getCategories(): Promise<Category[]> {
  const what = 'the categories'
  const fields = readObject(await apiRequest(endpoints.categories), what)
  return readList(fields, 'categories', what).map(parseCategory)
}

function parseCategory(row: unknown): Category {
  const what = 'a category'
  const fields = readObject(row, what)
  return {
    id: readText(fields, 'id', what),
    key: readText(fields, 'key', what),
    emoji: readText(fields, 'emoji', what),
    color: readText(fields, 'color', what),
    sortOrder: readCount(fields, 'sortOrder', what),
  }
}
