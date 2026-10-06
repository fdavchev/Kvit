import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect, type Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { startsWith } from './expenseTestHelpers'
import { requestsOf } from './requestTestHelpers'
import { translated } from './translated'

export type FormRowKey = 'paidBy' | 'split' | 'date' | 'category'

export type SplitTabKey = 'splitEqual' | 'splitExact' | 'splitPercentage' | 'splitShares'

const sheetTitleKeys: Record<FormRowKey, string> = {
  paidBy: 'expense.paidBy',
  split: 'expense.split',
  date: 'expense.pickDate',
  category: 'expense.pickCategory',
}

export function amountField(language: Language = 'en'): HTMLInputElement {
  return screen.getByLabelText<HTMLInputElement>(translated(language, 'expense.amount'))
}

export function typeAmount(text: string, language: Language = 'en'): void {
  fireEvent.change(amountField(language), { target: { value: text } })
}

export function titleField(language: Language = 'en'): HTMLInputElement {
  return screen.getByLabelText<HTMLInputElement>(translated(language, 'expense.titleField'))
}

export function typeTitle(text: string, language: Language = 'en'): void {
  fireEvent.change(titleField(language), { target: { value: text } })
}

export function noteLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'expense.note') })
}

export function noteField(language: Language = 'en'): HTMLTextAreaElement | HTMLInputElement {
  return screen.getByLabelText<HTMLTextAreaElement | HTMLInputElement>(translated(language, 'expense.note'))
}

export function formRow(key: FormRowKey, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: startsWith(translated(language, `expense.${key}`)) })
}

export function saveButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupSettings.save') })
}

export function currencyToggle(currency: string, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', {
    name: translated(language, 'expense.currencyToggle', { currency }),
  })
}

export function sheetOf(key: FormRowKey, language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, sheetTitleKeys[key]) })
}

export async function openSheet(key: FormRowKey, language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(formRow(key, language))
  return screen.findByRole('dialog', { name: translated(language, sheetTitleKeys[key]) })
}

export async function waitForSheetToClose(): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).toBeNull()
  })
}

export function personButton(sheet: HTMLElement, name: string): HTMLElement {
  return within(sheet).getByRole('button', { name: startsWith(name) })
}

export function personGroup(sheet: HTMLElement, name: string): HTMLElement {
  return within(sheet).getByRole('group', { name: startsWith(name) })
}

export function splitTab(sheet: HTMLElement, key: SplitTabKey, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('tab', { name: translated(language, `expense.${key}`) })
}

export function doneButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expense.done') })
}

export function isDisabled(element: HTMLElement): boolean {
  return element.matches(':disabled') || element.getAttribute('aria-disabled') === 'true'
}

export function typeInPerson(sheet: HTMLElement, name: string, text: string): void {
  fireEvent.change(within(personGroup(sheet, name)).getByRole('textbox'), {
    target: { value: text },
  })
}

export function sentBody(fetchMock: Mock<typeof fetch>, method: string, url: string, index = 0): Record<string, unknown> {
  const body = requestsOf(fetchMock, method, url)[index]?.body
  if (typeof body !== 'object' || body === null || Array.isArray(body)) {
    throw new Error(`Expected request number ${index} (${method} ${url}) to have a JSON object body, found ${JSON.stringify(body)}`)
  }
  return body as Record<string, unknown>
}

export function addDays(date: string, days: number): string {
  const moved = new Date(`${date}T00:00:00Z`)
  moved.setUTCDate(moved.getUTCDate() + days)
  return moved.toISOString().slice(0, 10)
}
