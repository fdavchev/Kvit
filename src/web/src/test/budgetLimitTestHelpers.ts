import { fireEvent, screen, within } from '@testing-library/react'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import { renderBudget } from './budgetTestHelpers'
import { readableText } from './budgetLeftCardTestHelpers'
import { personalCategoriesWithArchived } from './personalCategoryTestData'
import { monthAnswerBody } from './personalEntryTestData'
import { categoryListAnswer, personalCategoriesWithArchivedGet } from './personalCategoryTestHelpers'
import type { AnswerFactory } from './requestTestHelpers'
import { translated } from './translated'

export const monthLimitPut = `PUT ${endpoints.personalLimitsMonth}`
export const monthLimitDelete = `DELETE ${endpoints.personalLimitsMonth}`

export function categoryLimitPut(categoryId: string): string {
  return `PUT ${endpoints.personalLimitsCategory(categoryId)}`
}

export function categoryLimitDelete(categoryId: string): string {
  return `DELETE ${endpoints.personalLimitsCategory(categoryId)}`
}

type BudgetOptions = Parameters<typeof renderBudget>[0]

export function renderBudgetWithLimits(options: BudgetOptions = {}) {
  const withArchivedAnswer: Record<string, AnswerFactory> = {
    [personalCategoriesWithArchivedGet]: categoryListAnswer(() => personalCategoriesWithArchived),
  }
  return renderBudget({ ...options, answers: { ...withArchivedAnswer, ...options.answers } })
}

export interface LimitRowText {
  name: string
  amounts: string
  over: string | null
  converted: string | null
}

export function findLimitsCard(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: translated(language, 'budget.limits.title') })
}

export function queryLimitsCard(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: translated(language, 'budget.limits.title') })
}

export function limitRows(card: HTMLElement): HTMLElement[] {
  return within(card).getAllByRole('listitem')
}

function partOf(row: HTMLElement, attribute: string): HTMLElement {
  const part = row.querySelector<HTMLElement>(`[${attribute}]`)
  if (part === null) {
    throw new Error(`The limit row has no element with the attribute ${attribute}`)
  }
  return part
}

function optionalPartOf(row: HTMLElement, attribute: string): HTMLElement | null {
  return row.querySelector<HTMLElement>(`[${attribute}]`)
}

export function limitNameOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-limit-name')
}

export function limitAmountsOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-limit-amounts')
}

export function limitOverOf(row: HTMLElement): HTMLElement | null {
  return optionalPartOf(row, 'data-limit-over')
}

export function limitConvertedNoteOf(row: HTMLElement): HTMLElement | null {
  return optionalPartOf(row, 'data-limit-converted')
}

export function limitBarOf(row: HTMLElement): HTMLElement {
  return within(row).getByRole('progressbar')
}

export function limitFillOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-limit-fill')
}

export function limitRowButton(row: HTMLElement): HTMLElement {
  return within(row).getByRole('button')
}

export function limitRowText(row: HTMLElement): LimitRowText {
  const over = limitOverOf(row)
  const converted = limitConvertedNoteOf(row)
  return {
    name: readableText(limitNameOf(row)),
    amounts: readableText(limitAmountsOf(row)),
    over: over === null ? null : readableText(over),
    converted: converted === null ? null : readableText(converted),
  }
}

export function limitRowNamed(card: HTMLElement, name: string): HTMLElement {
  const row = limitRows(card).find((candidate) => readableText(limitNameOf(candidate)) === name)
  if (row === undefined) {
    throw new Error(`The Limits card has no row named "${name}"`)
  }
  return row
}

export function setLimitButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'budget.limits.setLimit') })
}

export function querySetLimitButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'budget.limits.setLimit') })
}

export function limitRowButtonNamed(name: string, language: Language = 'en'): HTMLElement {
  const card = queryLimitsCard(language)
  if (card === null) {
    throw new Error('The Limits card is not on the screen')
  }
  return limitRowButton(limitRowNamed(card, name))
}

export function setLimitButtonInCard(language: Language = 'en'): HTMLElement {
  const card = queryLimitsCard(language)
  if (card === null) {
    throw new Error('The Limits card is not on the screen')
  }
  return within(card).getByRole('button', { name: translated(language, 'budget.limits.setLimit') })
}

export function wholeMonthName(language: Language = 'en'): string {
  return translated(language, 'budget.limits.wholeMonth')
}

export function limitSheetTitle(name: string, language: Language = 'en'): string {
  return translated(language, 'budget.limits.sheetTitle', { name })
}

export function findLimitSheet(name: string, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: limitSheetTitle(name, language) })
}

export function findChooseSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'budget.limits.chooseTitle') })
}

export function removeLimitButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'budget.limits.remove') })
}

export function queryRemoveLimitButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement | null {
  return within(sheet).queryByRole('button', { name: translated(language, 'budget.limits.remove') })
}

export async function openLimitSheetFromRow(card: HTMLElement, name: string, language: Language = 'en'): Promise<HTMLElement> {
  const button = limitRowButton(limitRowNamed(card, name))
  button.focus()
  fireEvent.click(button)
  return findLimitSheet(name, language)
}

export function hasTapHeightOfAtLeast44(element: Element): boolean {
  return element.className.split(/\s+/).some((name) => /^min-h-(1[1-9]|[2-9]\d)$/.test(name))
}

export function limitAmountField(sheet: HTMLElement, language: Language = 'en'): HTMLInputElement {
  return within(sheet).getByLabelText<HTMLInputElement>(translated(language, 'expense.amount'))
}

export function typeLimit(sheet: HTMLElement, text: string, language: Language = 'en'): void {
  fireEvent.change(limitAmountField(sheet, language), { target: { value: text } })
}

export function saveLimitButton(sheet: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(sheet).getByRole<HTMLButtonElement>('button', { name: translated(language, 'groupSettings.save') })
}

export interface MonthThatChanges {
  monthAnswer: AnswerFactory
  acceptChange: AnswerFactory
}

export function monthThatChanges(before: PersonalEntryMonth, after: PersonalEntryMonth): MonthThatChanges {
  let current = before
  return {
    monthAnswer: () => Response.json(monthAnswerBody(current)),
    acceptChange: () => {
      current = after
      return new Response(null, { status: 204 })
    },
  }
}
