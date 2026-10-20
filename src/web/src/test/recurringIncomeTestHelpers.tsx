import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import type { Currency } from '@/shared/utils/formatMoney'
import { renderGroupApp } from './activityTestHelpers'
import { describeRequest } from './apiTestHelpers'
import { budgetNow, monthAnswer, personalEntriesGet } from './budgetTestHelpers'
import { plainSpaces, startsWith } from './expenseTestHelpers'
import { octoberMonth } from './personalEntryTestData'
import {
  catchUpAnswer,
  catchUpPost,
  recurringIncomesGet,
  recurringListAnswer,
  recurringIncomesUrl,
} from './recurringIncomeTestData'
import type { AnswerFactory } from './requestTestHelpers'
import { translated } from './translated'

export type RepeatsState = 'Never' | 'Monthly' | 'Weekly'

export type EndsOptionKey = 'endsOptionNever' | 'endsIn3Months' | 'endsIn6Months' | 'endsIn1Year' | 'endsPickDate'

const repeatsStates: readonly RepeatsState[] = ['Never', 'Monthly', 'Weekly']

export const sheetNow = new Date('2026-10-20T10:00:00Z')

export function repeatsChipName(state: RepeatsState, language: Language = 'en'): string {
  return translated(language, `budget.income.repeats${state}`)
}

export function repeatsChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  const names = repeatsStates.map((state) => repeatsChipName(state, language))
  return within(sheet).getByRole('button', { name: (accessibleName) => names.includes(accessibleName) })
}

export function repeatsChipIn(sheet: HTMLElement, state: RepeatsState, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: repeatsChipName(state, language) })
}

export function tapRepeats(sheet: HTMLElement, times: number, language: Language = 'en'): void {
  for (let tap = 0; tap < times; tap += 1) {
    fireEvent.click(repeatsChip(sheet, language))
  }
}

export function startChip(sheet: HTMLElement, day: string, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'budget.income.startsOn', { day }) })
}

export function anyStartChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  const prefix = translated(language, 'budget.income.startsOn', { day: '' })
  return within(sheet).getByRole('button', { name: (accessibleName) => accessibleName.startsWith(prefix) })
}

export function endsChipName(day: string | null, language: Language = 'en'): string {
  return day === null
    ? translated(language, 'budget.income.endsNever')
    : translated(language, 'budget.income.endsOn', { day })
}

export function endsChip(sheet: HTMLElement, day: string | null, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: endsChipName(day, language) })
}

export function anyEndsChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  const withDay = translated(language, 'budget.income.endsOn', { day: '' })
  const never = translated(language, 'budget.income.endsNever')
  return within(sheet).getByRole('button', {
    name: (accessibleName) => accessibleName === never || accessibleName.startsWith(withDay),
  })
}

export function queryAnyEndsChip(sheet: HTMLElement, language: Language = 'en'): HTMLElement | null {
  const withDay = translated(language, 'budget.income.endsOn', { day: '' })
  const never = translated(language, 'budget.income.endsNever')
  return (
    within(sheet).queryByRole('button', {
      name: (accessibleName) => accessibleName === never || accessibleName.startsWith(withDay),
    }) ?? null
  )
}

export async function findEndsBox(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'budget.income.endsTitle') })
}

export function endsOption(box: HTMLElement, key: EndsOptionKey, language: Language = 'en'): HTMLElement {
  return within(box).getByRole('button', { name: translated(language, `budget.income.${key}`) })
}

export async function openEndsBox(sheet: HTMLElement, language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(anyEndsChip(sheet, language))
  return findEndsBox(language)
}

export async function chooseEnds(sheet: HTMLElement, key: EndsOptionKey, language: Language = 'en'): Promise<void> {
  const box = await openEndsBox(sheet, language)
  fireEvent.click(endsOption(box, key, language))
  await waitFor(() => {
    expectNoDialogNamed(translated(language, 'budget.income.endsTitle'))
  })
}

function expectNoDialogNamed(name: string): void {
  if (screen.queryByRole('dialog', { name }) !== null) {
    throw new Error(`The dialog "${name}" is still open`)
  }
}

export async function findDateSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'expense.pickDate') })
}

export function sentRecurringRequests(fetchMock: Mock<typeof fetch>): string[] {
  return fetchMock.mock.calls
    .map((call) => describeRequest(call))
    .filter((request) => request.url.startsWith(recurringIncomesUrl))
    .map((request) => `${request.method ?? 'GET'} ${request.url}`)
}

export function sentWritesToRecurringIncomes(fetchMock: Mock<typeof fetch>): string[] {
  return sentRecurringRequests(fetchMock).filter((request) => !request.startsWith('GET '))
}

export function signedMoneyText(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return plainSpaces(formatSignedMoney(amountMinor, currency, language))
}

export function repeatingRowName(language: Language = 'en'): RegExp {
  return startsWith(translated(language, 'budget.recurring.title'))
}

export async function findRepeatingRow(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('button', { name: repeatingRowName(language) })
}

export async function findRepeatingSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'budget.recurring.title') })
}

export function blocksIn(container: HTMLElement, language: Language = 'en'): HTMLElement[] {
  const list = within(container).getByRole('list', { name: translated(language, 'budget.recurring.title') })
  return within(list).getAllByRole('listitem')
}

export function blockButton(block: HTMLElement, key: 'expense.edit' | 'budget.recurring.stop', language: Language = 'en'): HTMLElement {
  return within(block).getByRole('button', { name: translated(language, key) })
}

export function weekdayName(dayOfWeek: number, language: Language = 'en'): string {
  const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
  return translated(language, `budget.recurring.weekdays.${names[dayOfWeek]}`)
}

export interface RepeatingScreenOptions {
  language?: Language
  recurringIncomes?: readonly RecurringIncome[]
  answers?: Record<string, AnswerFactory>
}

export async function renderRepeatingScreen(options: RepeatingScreenOptions = {}) {
  return renderGroupApp(routes.budgetRepeating, {
    language: options.language,
    answers: {
      [recurringIncomesGet]: recurringListAnswer(options.recurringIncomes ?? []),
      [catchUpPost]: catchUpAnswer(0),
      [personalEntriesGet('2026-10')]: monthAnswer(octoberMonth),
      ...options.answers,
    },
  })
}

export const repeatingScreenNow = budgetNow
