import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect, type Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { formatMoney, type Currency } from '@/shared/utils/formatMoney'
import { plainSpaces, startsWith } from './expenseTestHelpers'
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

export function formRowBehindSheet(key: FormRowKey, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: startsWith(translated(language, `expense.${key}`)), hidden: true })
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

export function dateField(sheet: HTMLElement, language: Language = 'en'): HTMLInputElement {
  return within(sheet).getByLabelText<HTMLInputElement>(translated(language, 'expense.date'), {
    selector: 'input',
  })
}

export function typeDate(sheet: HTMLElement, text: string, language: Language = 'en'): void {
  fireEvent.change(dateField(sheet, language), { target: { value: text } })
}

export function todayChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expenses.today') })
}

export function yesterdayChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'expenses.yesterday') })
}

export function isDisabled(element: HTMLElement): boolean {
  return element.matches(':disabled') || element.getAttribute('aria-disabled') === 'true'
}

export function typeInPerson(sheet: HTMLElement, name: string, text: string): void {
  fireEvent.change(within(personGroup(sheet, name)).getByRole('textbox'), {
    target: { value: text },
  })
}

export function personBoxValue(sheet: HTMLElement, name: string): string {
  return within(personGroup(sheet, name)).getByRole<HTMLInputElement>('textbox').value
}

export function amountLeftText(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(
    translated(language, 'expense.amountLeft', { amount: formatMoney(amountMinor, currency, language) }),
  )
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

export interface AmountTextCase {
  name: string
  currency: Currency
  text: string
}

export const invalidAmountTextCases: AmountTextCase[] = [
  { name: 'a decimal in MKD', currency: 'MKD', text: '12.50' },
  { name: 'a thousands comma in MKD', currency: 'MKD', text: '1,200' },
  { name: 'letters in MKD', currency: 'MKD', text: 'abc' },
  { name: 'letters in EUR', currency: 'EUR', text: 'abc' },
  { name: 'three decimals in EUR', currency: 'EUR', text: '12.555' },
  { name: 'no digit before the point in EUR', currency: 'EUR', text: '.5' },
  { name: 'no digit before the point in MKD', currency: 'MKD', text: '.5' },
  { name: 'no digit after the point in EUR', currency: 'EUR', text: '5.' },
  { name: 'zero in MKD', currency: 'MKD', text: '0' },
  { name: 'zero in EUR', currency: 'EUR', text: '0.00' },
  { name: '13 digits in MKD', currency: 'MKD', text: '1234567890123' },
]

export const validAmountTextCases: AmountTextCase[] = [
  { name: 'whole denars in MKD', currency: 'MKD', text: '1200' },
  { name: 'whole euros in EUR', currency: 'EUR', text: '12' },
  { name: 'euros with two decimals', currency: 'EUR', text: '12.50' },
  { name: 'euros with a decimal comma', currency: 'EUR', text: '12,5' },
  { name: 'denars with spaces around them', currency: 'MKD', text: ' 1200 ' },
]

export const invalidFieldBorderClass = 'aria-invalid:border-destructive'

export function expectAmountMarkedInvalid(language: Language = 'en'): void {
  expect(amountField(language).getAttribute('aria-invalid')).toBe('true')
}

export function expectAmountNotMarkedInvalid(language: Language = 'en'): void {
  expect(amountField(language).getAttribute('aria-invalid')).not.toBe('true')
}
