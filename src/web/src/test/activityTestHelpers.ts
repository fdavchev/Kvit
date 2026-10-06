import type { QueryClient } from '@tanstack/react-query'
import { screen } from '@testing-library/react'
import type { Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { routeObjects } from '@/core/router/router'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { activityPath, numberEvents, type ActivityEventJson } from './activityTestData'
import {
  expenseRows,
  hotelDetail,
  hotelExpenseId,
  testCategories,
} from './expenseTestData'
import {
  categoriesPath,
  expensePathOf,
  expensesPath,
  groupPath,
  membersPath,
  plainSpaces,
} from './expenseTestHelpers'
import { testGroup } from './groupTestData'
import { ownerViewMembers } from './memberTestData'
import { renderRoutesWithProviders } from './renderWithProviders'
import {
  jsonAnswer,
  stubFetchByRequest,
  type AnswerFactory,
} from './requestTestHelpers'
import { testMe } from './testMe'
import { translated } from './translated'

export interface GroupAppOptions {
  language?: Language
  group?: Group
  members?: GroupMembers
  expenses?: ExpenseListRow[]
  events?: ActivityEventJson[]
  timeZone?: string
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

export function stubGroupApp(options: GroupAppOptions = {}): Mock<typeof fetch> {
  const group = options.group ?? testGroup
  const language = options.language ?? 'en'
  return stubFetchByRequest({
    'GET /api/me': jsonAnswer({
      ...testMe,
      language,
      timeZone: options.timeZone ?? testMe.timeZone,
    }),
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(options.members ?? ownerViewMembers),
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: options.expenses ?? expenseRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${activityPath}`]: jsonAnswer({ events: numberEvents(options.events ?? []) }),
    [`GET ${expensePathOf(hotelExpenseId)}`]: jsonAnswer(hotelDetail),
    ...options.answers,
  })
}

export async function renderGroupApp(path: string, options: GroupAppOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubGroupApp(options)
  const rendered = await renderRoutesWithProviders(routeObjects, path, {
    language,
    seedCache: options.seedCache,
  })
  return { fetchMock, ...rendered }
}

export function sentenceElement(text: string): HTMLElement {
  return screen.getByText(plainSpaces(text), { normalizer: plainSpaces })
}

export function activityRowOf(sentence: string): HTMLElement {
  let node: HTMLElement | null = sentenceElement(sentence)
  while (node !== null) {
    if (node.querySelector('img, [style*="--avatar-"]') !== null) {
      return node
    }
    node = node.parentElement
  }
  throw new Error(`Found no row with an avatar around the sentence "${sentence}"`)
}

export function expensesTab(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'expenses.title') })
}

export function activityTab(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'activity.title') })
}
