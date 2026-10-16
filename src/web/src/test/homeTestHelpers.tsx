import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, within } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Dashboard } from '@/core/services/dashboard/dashboardService'
import { HomeScreen } from '@/features/home/components/HomeScreen'
import { stubFetchThatNeverAnswers } from './apiTestHelpers'
import { dashboardPath, fullDashboard } from './dashboardTestData'
import { plainSpaces } from './expenseTestHelpers'
import { renderElementWithProviders } from './renderWithProviders'
import { jsonAnswer, stubFetchByRequest, type AnswerFactory } from './requestTestHelpers'
import { seedMe, testMe } from './testMe'
import { translated } from './translated'

export interface HomeAppOptions {
  language?: Language
  dashboard?: Dashboard
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

export interface LiveDashboard {
  current: Dashboard
}

export function liveDashboardAnswer(live: LiveDashboard): AnswerFactory {
  return () => Response.json(live.current)
}

async function renderHomeScreen(options: HomeAppOptions) {
  const language = options.language ?? 'en'
  return renderElementWithProviders(<HomeScreen />, routes.dashboard, {
    language,
    seedCache: (queryClient) => {
      seedMe({ ...testMe, language })(queryClient)
      options.seedCache?.(queryClient)
    },
  })
}

export async function renderHome(options: HomeAppOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    'GET /api/me': jsonAnswer({ ...testMe, language }),
    [`GET ${dashboardPath}`]: jsonAnswer(options.dashboard ?? fullDashboard),
    ...options.answers,
  })
  const rendered = await renderHomeScreen(options)
  return { fetchMock, ...rendered }
}

export async function renderHomeWaitingForServer(options: HomeAppOptions = {}) {
  stubFetchThatNeverAnswers()
  return renderHomeScreen(options)
}

export function plainTextOf(element: Element): string {
  return plainSpaces(element.textContent ?? '').trim()
}

export function textMatching(text: string): (content: string) => boolean {
  const wanted = plainSpaces(text)
  return (content) => plainSpaces(content).trim() === wanted
}

export function paragraphWithText(text: string): (content: string, element: Element | null) => boolean {
  const wanted = plainSpaces(text)
  return (_content, element) => element?.tagName === 'P' && plainTextOf(element) === wanted
}

export function regionNamed(name: string): HTMLElement {
  return screen.getByRole('region', { name })
}

export function findNeedsBar(language: Language = 'en'): HTMLElement {
  const barText = [
    translated(language, 'home.thingsNeedYou_one'),
    translated(language, 'home.thingsNeedYou_other'),
  ]
  const found = screen
    .getAllByRole('button')
    .find((button) => barText.some((text) => plainTextOf(button).includes(text)))
  if (found === undefined) {
    throw new Error(`Found no pinned Needs you bar; the buttons on the page are: ${screen.getAllByRole('button').map(plainTextOf).join(' | ')}`)
  }
  return found
}

export function openNeedsBar(language: Language = 'en'): void {
  fireEvent.click(findNeedsBar(language))
}

export function cardOf(sentence: string): HTMLElement {
  const textElement = screen.getByText(paragraphWithText(sentence))
  const card = textElement.closest('li')
  if (card === null) {
    throw new Error(`The text "${sentence}" is not inside a list item (the card)`)
  }
  return card
}

export function cardButton(card: HTMLElement, name: string): HTMLElement {
  return within(card).getByRole('button', { name })
}
