import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import {
  readCount,
  readList,
  readObject,
  readOneOf,
  readText,
  readTextOrNull,
  readYesNo,
  type Fields,
} from '@/core/services/readFields'

export const personalCategoryColors = [
  'orange',
  'green',
  'yellow',
  'blue',
  'pink',
  'purple',
  'slate',
  'red',
  'teal',
  'gray',
] as const

export type PersonalCategoryColor = (typeof personalCategoryColors)[number]

interface PersonalCategoryLook {
  id: string
  emoji: string
  color: PersonalCategoryColor
  sortOrder: number
  archived: boolean
}

export interface BuiltInPersonalCategory extends PersonalCategoryLook {
  isCustom: false
  key: string
  name: null
}

export interface CustomPersonalCategory extends PersonalCategoryLook {
  isCustom: true
  key: null
  name: string
}

export type PersonalCategory = BuiltInPersonalCategory | CustomPersonalCategory

export interface PersonalCategoryFields {
  name: string
  emoji: string
  color: PersonalCategoryColor
}

export async function getPersonalCategories(): Promise<PersonalCategory[]> {
  return parseCategoryList(await apiRequest(endpoints.personalCategories))
}

export async function getPersonalCategoriesWithArchived(): Promise<PersonalCategory[]> {
  return parseCategoryList(await apiRequest(endpoints.personalCategoriesWithArchived))
}

export async function createPersonalCategory(fields: PersonalCategoryFields): Promise<PersonalCategory> {
  return parseCategory(await apiRequest(endpoints.personalCategories, jsonRequest('POST', fields)))
}

export async function updatePersonalCategory(
  categoryId: string,
  fields: PersonalCategoryFields,
): Promise<void> {
  await apiRequest(endpoints.personalCategory(categoryId), jsonRequest('PUT', fields))
}

export async function archivePersonalCategory(categoryId: string): Promise<void> {
  await apiRequest(endpoints.personalCategoryArchive(categoryId), { method: 'POST' })
}

export async function unarchivePersonalCategory(categoryId: string): Promise<void> {
  await apiRequest(endpoints.personalCategoryUnarchive(categoryId), { method: 'POST' })
}

function parseCategoryList(body: unknown): PersonalCategory[] {
  const what = 'the personal categories'
  return readList(readObject(body, what), 'categories', what).map(parseCategory)
}

function parseCategory(row: unknown): PersonalCategory {
  const what = 'a personal category'
  const fields = readObject(row, what)
  const look: PersonalCategoryLook = {
    id: readText(fields, 'id', what),
    emoji: readText(fields, 'emoji', what),
    color: readOneOf(fields, 'color', personalCategoryColors, what),
    sortOrder: readCount(fields, 'sortOrder', what),
    archived: readYesNo(fields, 'archived', what),
  }
  if (readYesNo(fields, 'isCustom', what)) {
    readNothing(fields, 'key', what)
    return { ...look, isCustom: true, key: null, name: readText(fields, 'name', what) }
  }
  readNothing(fields, 'name', what)
  return { ...look, isCustom: false, key: readText(fields, 'key', what), name: null }
}

function readNothing(fields: Fields, name: string, what: string): void {
  if (readTextOrNull(fields, name, what) !== null) {
    throw new Error(`Expected "${name}" of ${what} to be null, got ${JSON.stringify(fields[name])}`)
  }
}
