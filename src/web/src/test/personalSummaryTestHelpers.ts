import type { QueryClient } from '@tanstack/react-query'
import { screen, within } from '@testing-library/react'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { PersonalSummary } from '@/core/services/personalSummary/personalSummaryService'
import { formatMonthLabel } from '@/shared/utils/calendarMonth'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { renderGroupApp } from './activityTestHelpers'
import { asciiMinus, incomeMoney } from './budgetLeftCardTestHelpers'
import { plainSpaces } from './expenseTestHelpers'
import { personalCategoriesWithArchived } from './personalCategoryTestData'
import { categoryListAnswer, personalCategoriesWithArchivedGet } from './personalCategoryTestHelpers'
import { octoberSummary } from './personalSummaryTestData'
import { jsonAnswer, type AnswerFactory } from './requestTestHelpers'
import { testMe } from './testMe'
import { translated } from './translated'

export function personalSummaryGet(month: string): string {
  return `GET ${endpoints.personalSummary(month)}`
}

export function summaryAnswer(summary: PersonalSummary): AnswerFactory {
  return () => Response.json(summary)
}

export function summaryAnswers(summaries: readonly PersonalSummary[]): Record<string, AnswerFactory> {
  return Object.fromEntries(summaries.map((summary) => [personalSummaryGet(summary.month), summaryAnswer(summary)]))
}

export function summaryPath(month: string): string {
  return `${routes.budgetSummary}?month=${month}`
}

interface SummaryScreenOptions {
  language?: Language
  path?: string
  summaries?: readonly PersonalSummary[]
  budgetTotalCurrency?: Currency | null
  timeZone?: string
  categories?: readonly PersonalCategory[]
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

export async function renderSummaryScreen(options: SummaryScreenOptions = {}) {
  const language = options.language ?? 'en'
  const categories = options.categories ?? personalCategoriesWithArchived
  return renderGroupApp(options.path ?? summaryPath('2026-10'), {
    language,
    timeZone: options.timeZone,
    seedCache: options.seedCache,
    answers: {
      'GET /api/me': jsonAnswer({
        ...testMe,
        language,
        timeZone: options.timeZone ?? testMe.timeZone,
        budgetTotalCurrency: options.budgetTotalCurrency ?? null,
      }),
      ...summaryAnswers(options.summaries ?? [octoberSummary]),
      [personalCategoriesWithArchivedGet]: categoryListAnswer(() => categories),
      ...options.answers,
    },
  })
}

export function summaryTitle(month: string, previousMonth: string, currentMonth: string, language: Language = 'en'): string {
  return translated(language, 'budget.summary.title', {
    month: formatMonthLabel(month, currentMonth, language),
    previousMonth: formatMonthLabel(previousMonth, currentMonth, language),
  })
}

export function findSummaryHeading(title: string): Promise<HTMLElement> {
  return screen.findByRole('heading', { level: 1, name: title })
}

export function currencyCardName(currency: Currency, language: Language = 'en'): string {
  return currencyLabel(currency, language)
}

export function convertedCardName(currency: Currency, language: Language = 'en'): string {
  return translated(language, currency === 'MKD' ? 'budget.card.inMkd' : 'budget.card.inEur')
}

export function findCurrencyCard(currency: Currency, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: currencyCardName(currency, language) })
}

export function queryCurrencyCard(currency: Currency, language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: currencyCardName(currency, language) })
}

export function findConvertedCard(currency: Currency, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: convertedCardName(currency, language) })
}

export function queryConvertedCard(currency: Currency, language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: convertedCardName(currency, language) })
}

export type TotalKind = 'spending' | 'income'

export function totalRow(card: HTMLElement, kind: TotalKind): HTMLElement {
  const row = card.querySelector<HTMLElement>(`[data-summary-total="${kind}"]`)
  if (row === null) {
    throw new Error(`The card has no element with data-summary-total="${kind}"`)
  }
  return row
}

export function queryTotalRow(card: HTMLElement, kind: TotalKind): HTMLElement | null {
  return card.querySelector<HTMLElement>(`[data-summary-total="${kind}"]`)
}

function partOf(scope: HTMLElement, attribute: string): HTMLElement {
  const part = scope.querySelector<HTMLElement>(`[${attribute}]`)
  if (part === null) {
    throw new Error(`Found no element with the attribute ${attribute} in: ${scope.textContent}`)
  }
  return part
}

export function thisAmountOf(scope: HTMLElement): HTMLElement {
  return partOf(scope, 'data-summary-this')
}

export function previousAmountOf(scope: HTMLElement): HTMLElement {
  return partOf(scope, 'data-summary-previous')
}

export function changeOf(scope: HTMLElement): HTMLElement {
  return partOf(scope, 'data-summary-change')
}

export function differenceOf(scope: HTMLElement): HTMLElement | null {
  return scope.querySelector<HTMLElement>('[data-summary-difference]')
}

export function nameOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-summary-name')
}

export function emojiOf(row: HTMLElement): HTMLElement {
  return partOf(row, 'data-summary-emoji')
}

export function amountReadout(amount: HTMLElement): string {
  return asciiMinus(plainSpaces((amount.querySelector('.sr-only') ?? amount).textContent ?? ''))
}

export function categoryList(card: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(card).getByRole('list', { name: translated(language, 'budget.screen.categoryTotals') })
}

export function queryCategoryList(card: HTMLElement, language: Language = 'en'): HTMLElement | null {
  return within(card).queryByRole('list', { name: translated(language, 'budget.screen.categoryTotals') })
}

export function categoryRows(card: HTMLElement, language: Language = 'en'): HTMLElement[] {
  return within(categoryList(card, language)).getAllByRole('listitem')
}

export function categoryNamesIn(card: HTMLElement, language: Language = 'en'): string[] {
  return categoryRows(card, language).map((row) => plainSpaces(nameOf(row).textContent ?? ''))
}

export function directionOf(change: HTMLElement): string | null {
  return change.getAttribute('data-summary-direction')
}

export function visibleText(element: HTMLElement): string {
  return asciiMinus(plainSpaces(element.textContent ?? ''))
}

export function percentText(percent: number, language: Language = 'en'): string {
  return plainSpaces(translated(language, 'budget.summary.percent', { percent: String(percent) }))
}

export function upText(percent: number, language: Language = 'en'): string {
  return translated(language, 'budget.summary.up', { percent: String(percent) })
}

export function downText(percent: number, language: Language = 'en'): string {
  return translated(language, 'budget.summary.down', { percent: String(percent) })
}

export function signedMoney(amountMinor: number, currency: Currency, language: Language = 'en'): string {
  return asciiMinus(incomeMoney(amountMinor, currency, language))
}
