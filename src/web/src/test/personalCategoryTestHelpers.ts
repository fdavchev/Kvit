import { screen, within } from '@testing-library/react'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { AnswerFactory } from './requestTestHelpers'
import { translated } from './translated'

export const personalCategoriesGet = `GET ${endpoints.personalCategories}`
export const personalCategoriesWithArchivedGet = `GET ${endpoints.personalCategoriesWithArchived}`
export const personalCategoryCreatePost = `POST ${endpoints.personalCategories}`

export function personalCategoryUpdatePut(categoryId: string): string {
  return `PUT ${endpoints.personalCategory(categoryId)}`
}

export function personalCategoryArchivePost(categoryId: string): string {
  return `POST ${endpoints.personalCategoryArchive(categoryId)}`
}

export function personalCategoryUnarchivePost(categoryId: string): string {
  return `POST ${endpoints.personalCategoryUnarchive(categoryId)}`
}

export function categoryListAnswer(read: () => readonly PersonalCategory[]): AnswerFactory {
  return () => Response.json({ categories: read() })
}

export function personalCategoryLabel(language: Language, category: PersonalCategory): string {
  return category.isCustom ? category.name : translated(language, `categories.${category.key}`)
}

export async function findSheet(language: Language, titleKey: string): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, titleKey) })
}

export function sheetField(sheet: HTMLElement, language: Language, labelKey: string): HTMLInputElement {
  return within(sheet).getByLabelText<HTMLInputElement>(translated(language, labelKey))
}

export function colorGroup(sheet: HTMLElement, language: Language): HTMLElement {
  return within(sheet).getByRole('group', { name: translated(language, 'budget.categories.color') })
}

export function emojiPickGroup(sheet: HTMLElement, language: Language): HTMLElement {
  return within(sheet).getByRole('group', { name: translated(language, 'budget.categories.emojiPicks') })
}

export function saveButtonIn(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'groupSettings.save') })
}

export function waitForAnyTimers(): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, 20)
  })
}
