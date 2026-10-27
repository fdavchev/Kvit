import { screen, within } from '@testing-library/react'
import { toast } from 'sonner'
import { vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import { money, readableText } from './budgetLeftCardTestHelpers'
import {
  dinnerPair,
  duplicateAnswersUrl,
  groceriesPair,
  taxiPair,
  type DuplicatePairFixture,
} from './budgetDuplicateTestData'
import { monthAnswerBody } from './personalEntryTestData'
import type { AnswerFactory } from './requestTestHelpers'
import { translated } from './translated'

export const duplicateAnswersPut = `PUT ${duplicateAnswersUrl}`
export const duplicateAnswersDelete = `DELETE ${duplicateAnswersUrl}`

export function duplicateCardName(language: Language = 'en'): string {
  return translated(language, 'budget.duplicates.title')
}

export function findDuplicateCard(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: duplicateCardName(language) })
}

export function queryDuplicateCard(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: duplicateCardName(language) })
}

export function sameButton(card: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(card).getByRole<HTMLButtonElement>('button', { name: translated(language, 'budget.duplicates.same') })
}

export function newButton(card: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(card).getByRole<HTMLButtonElement>('button', { name: translated(language, 'budget.duplicates.new') })
}

export interface DuplicateSentenceParts {
  entry: string
  entryAmount: string
  entryDay: string
  expense: string
  group: string
  shareAmount: string
  expenseDay: string
}

export function duplicateSentence(language: Language, parts: DuplicateSentenceParts): string {
  return translated(language, 'budget.duplicates.body', { ...parts })
}

export function sentenceOfPair(
  language: Language,
  pair: DuplicatePairFixture,
  names: { entry: string; expense: string; entryDay: string; expenseDay: string },
): string {
  return duplicateSentence(language, {
    entry: names.entry,
    entryAmount: money(pair.entryAmountMinor, pair.currency, language),
    entryDay: names.entryDay,
    expense: names.expense,
    group: pair.groupName,
    shareAmount: money(pair.shareMinor, pair.currency, language),
    expenseDay: names.expenseDay,
  })
}

const seventeenthOfOctober: Record<Language, string> = { en: '17 Oct', mk: '17 окт' }

export function dinnerSentence(language: Language = 'en'): string {
  return sentenceOfPair(language, dinnerPair, {
    entry: 'Dinner out',
    expense: 'Dinner',
    entryDay: seventeenthOfOctober[language],
    expenseDay: translated(language, 'expenses.yesterday'),
  })
}

export function taxiSentence(language: Language = 'en'): string {
  return sentenceOfPair(language, taxiPair, {
    entry: 'Taxi ride',
    expense: 'Taxi',
    entryDay: translated(language, 'expenses.yesterday'),
    expenseDay: translated(language, 'expenses.yesterday'),
  })
}

export function groceriesSentence(language: Language = 'en'): string {
  const groceries = translated(language, 'categories.groceries')
  return sentenceOfPair(language, groceriesPair, {
    entry: groceries,
    expense: groceries,
    entryDay: seventeenthOfOctober[language],
    expenseDay: translated(language, 'expenses.today'),
  })
}

export function cardText(card: HTMLElement): string {
  return readableText(card)
}

export function mergedLabel(language: Language, expense: string, group: string): string {
  return translated(language, 'budget.duplicates.mergedWith', { expense, group })
}

export function mergedFallbackLabel(language: Language = 'en'): string {
  return translated(language, 'budget.duplicates.mergedWithGroupExpense')
}

export function notTheSameButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'budget.duplicates.notTheSame') })
}

export function queryNotTheSameButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement | null {
  return within(sheet).queryByRole('button', { name: translated(language, 'budget.duplicates.notTheSame') })
}

export function countedAgainText(language: Language = 'en'): string {
  return translated(language, 'budget.duplicates.countedAgain')
}

export function entryRowContaining(list: HTMLElement, text: string): HTMLElement {
  const row = within(list)
    .getAllByRole('listitem')
    .find((candidate) => readableText(candidate).includes(text))
  if (row === undefined) {
    throw new Error(`The entry list has no row with the text "${text}"`)
  }
  return row
}

export function entryRowButton(row: HTMLElement): HTMLElement {
  return within(row).getByRole('button')
}

export function opacityPercentOf(row: HTMLElement): number | null {
  const container = [row, ...Array.from(row.querySelectorAll<HTMLElement>('button'))].find((candidate) =>
    candidate.className.split(/\s+/).some((name) => /^opacity-\d+$/.test(name)),
  )
  if (container === undefined) {
    return null
  }
  const className = container.className.split(/\s+/).find((name) => /^opacity-\d+$/.test(name))
  return Number(className?.slice('opacity-'.length))
}

export function errorToastTexts(): unknown[] {
  return vi.mocked(toast.error).mock.calls.map(([text]) => text)
}

export function shownToastMessages(): unknown[] {
  return [...vi.mocked(toast).mock.calls, ...vi.mocked(toast.success).mock.calls].map(([text]) => text)
}

export interface AnswerThatChangesTheMonth {
  monthAnswer: AnswerFactory
  acceptAnswer: AnswerFactory
  refuseAnswer: (respond: () => Response) => AnswerFactory
}

export function monthThatChangesWhenAnswered(
  before: PersonalEntryMonth,
  after: PersonalEntryMonth,
): AnswerThatChangesTheMonth {
  let current = before
  return {
    monthAnswer: () => Response.json(monthAnswerBody(current)),
    acceptAnswer: () => {
      current = after
      return new Response(null, { status: 204 })
    },
    refuseAnswer: (respond) => () => {
      current = after
      return respond()
    },
  }
}
