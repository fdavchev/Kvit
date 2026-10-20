import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import type { Me } from '@/core/services/me/meService'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { PersonalSpendingSheet } from '@/features/budget/entries/components/PersonalSpendingSheet'
import { personalEntriesQueryKey } from '@/features/budget/entries/personalEntryCache'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { amountField } from './expenseFormTestHelpers'
import { activePersonalCategories } from './personalCategoryTestData'
import {
  categoryListAnswer,
  findSheet,
  personalCategoriesGet,
  personalCategoryLabel,
  saveButtonIn,
  sheetField,
} from './personalCategoryTestHelpers'
import { renderElementWithProviders } from './renderWithProviders'
import { stubFetchByRequest, type AnswerFactory } from './requestTestHelpers'
import { seedMe, testMe } from './testMe'
import { translated } from './translated'

export const personalEntriesPost = `POST ${endpoints.personalEntries}`

export function personalEntryPut(entryId: string): string {
  return `PUT ${endpoints.personalEntry(entryId)}`
}

export function personalEntryDelete(entryId: string): string {
  return `DELETE ${endpoints.personalEntry(entryId)}`
}

export function personalEntryRestorePost(entryId: string): string {
  return `POST ${endpoints.personalEntryRestore(entryId)}`
}

export const seededMonth = '2026-10'

interface SheetOptions {
  entry?: PersonalEntry
  language?: Language
  me?: Me
  answers?: Record<string, AnswerFactory>
}

export async function renderSpendingSheet(options: SheetOptions = {}) {
  const language = options.language ?? 'en'
  const me = options.me ?? testMe
  const onClose = vi.fn<() => void>()
  const fetchMock = stubFetchByRequest({
    [personalCategoriesGet]: categoryListAnswer(() => activePersonalCategories),
    ...options.answers,
  })
  const rendered = await renderElementWithProviders(
    <PersonalSpendingSheet entry={options.entry} onClose={onClose} />,
    '/sheet',
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...me, language })(queryClient)
        queryClient.setQueryData(personalEntriesQueryKey(seededMonth), 'a month of entries')
      },
    },
  )
  return { ...rendered, fetchMock, onClose, language }
}

export async function findPickSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'budget.entries.pickCategory') })
}

export async function findDetailsSheet(category: PersonalCategory, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: personalCategoryLabel(language, category) })
}

export async function pickCategory(category: PersonalCategory, language: Language = 'en'): Promise<HTMLElement> {
  const pickSheet = await findPickSheet(language)
  fireEvent.click(await within(pickSheet).findByRole('button', { name: personalCategoryLabel(language, category) }))
  return findDetailsSheet(category, language)
}

export async function createCategoryFromGrid(name: string, language: Language = 'en'): Promise<void> {
  const pickSheet = await findPickSheet(language)
  fireEvent.click(await within(pickSheet).findByRole('button', { name: translated(language, 'budget.categories.new') }))
  const newSheet = await findSheet(language, 'budget.categories.newTitle')
  fireEvent.change(sheetField(newSheet, language, 'budget.categories.name'), { target: { value: name } })
  fireEvent.click(saveButtonIn(newSheet, language))
}

export async function expectAmountToStayFocused(language: Language = 'en'): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByRole('dialog', { name: translated(language, 'budget.categories.newTitle') })).toBeNull()
  })
  await new Promise((resolve) => setTimeout(resolve, 50))
  expect(document.activeElement).toBe(amountField(language))
}

export function dateChip(sheet: HTMLElement, name: string): HTMLElement {
  return within(sheet).getByRole('button', { name })
}

export function noteChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expense.note') })
}

export function categoryChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expense.category') })
}

export function deleteButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expense.delete') })
}

export function shownCurrency(sheet: HTMLElement, currency: Currency, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', {
    name: translated(language, 'expense.currencyToggle', { currency: currencyLabel(currency, language) }),
  })
}

export function categoryHeading(sheet: HTMLElement): HTMLElement {
  const heading = sheet.querySelector<HTMLElement>('[data-category-heading]')
  if (heading === null) {
    throw new Error('The sheet shows no category heading')
  }
  return heading
}

export function isEntriesCacheRefreshed(queryClient: Awaited<ReturnType<typeof renderSpendingSheet>>['queryClient']): boolean {
  return queryClient.getQueryState(personalEntriesQueryKey(seededMonth))?.isInvalidated === true
}

export async function waitForEveryDialogToClose(): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).toBeNull()
  })
}
