import { screen, within } from '@testing-library/react'
import { StrictMode } from 'react'
import { endpoints } from '@/core/api/endpoints'
import type { Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { BudgetScreen } from '@/features/budget/overview/components/BudgetScreen'
import { activePersonalCategories } from './personalCategoryTestData'
import { categoryListAnswer, personalCategoriesGet } from './personalCategoryTestHelpers'
import { monthAnswerBody, octoberMonth } from './personalEntryTestData'
import { catchUpAnswer, catchUpPost, recurringIncomesGet, recurringListAnswer } from './recurringIncomeTestData'
import { renderElementWithProviders } from './renderWithProviders'
import { jsonAnswer, stubFetchByRequest, type AnswerFactory } from './requestTestHelpers'
import { seedMe, testMe } from './testMe'
import { translated } from './translated'

export const budgetNow = new Date('2026-10-19T10:00:00Z')

export function personalEntriesGet(month: string): string {
  return `GET ${endpoints.personalEntriesOfMonth(month)}`
}

export function monthAnswer(month: PersonalEntryMonth): AnswerFactory {
  return () => Response.json(monthAnswerBody(month))
}

export function monthAnswers(months: readonly PersonalEntryMonth[]): Record<string, AnswerFactory> {
  return Object.fromEntries(months.map((month) => [personalEntriesGet(month.month), monthAnswer(month)]))
}

interface BudgetOptions {
  language?: Language
  me?: Me
  months?: readonly PersonalEntryMonth[]
  recurringIncomes?: readonly RecurringIncome[]
  answers?: Record<string, AnswerFactory>
  isStrictMode?: boolean
}

export async function renderBudget(options: BudgetOptions = {}) {
  const language = options.language ?? 'en'
  const me: Me = { ...(options.me ?? testMe), language }
  const fetchMock = stubFetchByRequest({
    'GET /api/me': jsonAnswer(me),
    [personalCategoriesGet]: categoryListAnswer(() => activePersonalCategories),
    ...monthAnswers(options.months ?? [octoberMonth]),
    [recurringIncomesGet]: recurringListAnswer(options.recurringIncomes ?? []),
    [catchUpPost]: catchUpAnswer(0),
    ...options.answers,
  })
  const screenElement = options.isStrictMode === true ? <StrictMode><BudgetScreen /></StrictMode> : <BudgetScreen />
  const rendered = await renderElementWithProviders(screenElement, routes.budget, {
    language,
    seedCache: seedMe(me),
  })
  return { fetchMock, language, ...rendered }
}

export function monthStrip(language: Language = 'en'): HTMLElement {
  return screen.getByRole('group', { name: translated(language, 'budget.screen.months') })
}

export function monthButton(label: string, language: Language = 'en'): HTMLElement {
  return within(monthStrip(language)).getByRole('button', { name: label })
}

export function findTotalCard(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: translated(language, 'budget.screen.spentThisMonth') })
}

export function findEntryList(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('list', { name: translated(language, 'budget.screen.entries') })
}

export function categoryTotalsRegion(language: Language = 'en'): HTMLElement {
  return screen.getByRole('region', { name: translated(language, 'budget.screen.categoryTotals') })
}

export function plusButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'budget.screen.add') })
}
