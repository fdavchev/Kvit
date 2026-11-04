import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, within } from '@testing-library/react'
import { vi, type Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { routeObjects } from '@/core/router/router'
import type { Me } from '@/core/services/me/meService'
import { stubGroupApp } from './activityTestHelpers'
import { adminStatsPath, monthlyStats, novemberStats, weeklyStats, yearlyStats } from './adminStatsTestData'
import { asciiMinus } from './budgetLeftCardTestHelpers'
import { plainSpaces } from './expenseTestHelpers'
import { renderRoutesWithProviders } from './renderWithProviders'
import { jsonAnswer, type AnswerFactory } from './requestTestHelpers'
import { testAdminMe } from './testMe'
import { translated } from './translated'

const adminAddress = '/admin'

export type StatsAnswer = Response | Error | Promise<Response>

export type StatsAnswerFor = (url: string) => StatsAnswer

interface AdminStatsAppOptions {
  language?: Language
  path?: string
  me?: Me
  statsAnswerFor?: StatsAnswerFor
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

export function answerWith(body: unknown): StatsAnswerFor {
  return () => Response.json(body)
}

export function stubAdminStatsApp(options: AdminStatsAppOptions = {}): Mock<typeof fetch> {
  const language = options.language ?? 'en'
  const everythingElse = stubGroupApp({
    language,
    answers: { 'GET /api/me': jsonAnswer({ ...(options.me ?? testAdminMe), language }), ...options.answers },
  })
  const statsAnswerFor = options.statsAnswerFor ?? answerWith(novemberStats)
  const fetchMock = vi.fn<typeof fetch>(async (input, init) => {
    const url = String(input)
    if (!url.startsWith(adminStatsPath)) {
      return everythingElse(input, init)
    }
    const answer = await statsAnswerFor(url)
    if (answer instanceof Error) {
      throw answer
    }
    return answer
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export async function renderAdminStats(options: AdminStatsAppOptions = {}) {
  const fetchMock = stubAdminStatsApp(options)
  const rendered = await renderRoutesWithProviders(routeObjects, options.path ?? adminAddress, {
    language: options.language ?? 'en',
    seedCache: options.seedCache,
  })
  return { fetchMock, ...rendered }
}

export function statsRequests(fetchMock: Mock<typeof fetch>): string[] {
  return fetchMock.mock.calls.map(([input]) => String(input)).filter((url) => url.startsWith(adminStatsPath))
}

export function statsRequestCount(fetchMock: Mock<typeof fetch>, url: string): number {
  return statsRequests(fetchMock).filter((requested) => requested === url).length
}

export function requestedPaths(fetchMock: Mock<typeof fetch>): string[] {
  return [...new Set(fetchMock.mock.calls.map(([input]) => String(input).split('?')[0]))].sort()
}

export interface HeldAnswer {
  answerFor: StatsAnswerFor
  release: () => void
}

export function holdAnswerFor(heldUrl: string, answerAfterwards: unknown, otherwise: unknown = novemberStats): HeldAnswer {
  let release: () => void = () => {}
  const held = new Promise<Response>((resolve) => {
    release = () => resolve(Response.json(answerAfterwards))
  })
  return {
    answerFor: (url) => (url === heldUrl ? held : Response.json(otherwise)),
    release: () => release(),
  }
}

export function findStatsTitle(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('heading', { level: 1, name: translated(language, 'adminStats.title') })
}

export function findStatsCard(title: string): Promise<HTMLElement> {
  return screen.findByRole('region', { name: title })
}

export function statsCard(title: string): HTMLElement {
  return screen.getByRole('region', { name: title })
}

export function cardsContainer(): HTMLElement {
  const container = document.querySelector<HTMLElement>('[data-stats-cards]')
  if (container === null) {
    throw new Error('The screen has no element with the attribute data-stats-cards')
  }
  return container
}

export function queryCardsContainer(): HTMLElement | null {
  return document.querySelector<HTMLElement>('[data-stats-cards]')
}

export function cardTitlesInOrder(): string[] {
  return within(cardsContainer())
    .getAllByRole('region')
    .map((card) => within(card).getByRole('heading', { level: 2 }).textContent ?? '')
}

export function normalizedLine(text: string): string {
  return asciiMinus(plainSpaces(text)).replace(/\s+/g, ' ').trim()
}

export function lineText(element: Element): string {
  return normalizedLine(element.textContent ?? '')
}

export function totalOf(card: HTMLElement): string {
  const total = card.querySelector('[data-stats-total]')
  if (total === null) {
    throw new Error(`The card has no element with the attribute data-stats-total: ${card.textContent}`)
  }
  return lineText(total)
}

export function compareOf(card: HTMLElement): string | null {
  const compare = card.querySelector('[data-stats-compare]')
  return compare === null ? null : lineText(compare)
}

export function barsOf(card: HTMLElement): HTMLElement[] {
  const bars = card.querySelector('[data-stats-bars]')
  if (bars === null) {
    throw new Error(`The card has no element with the attribute data-stats-bars: ${card.textContent}`)
  }
  return Array.from(bars.children).map((bar) => {
    if (!(bar instanceof HTMLElement)) {
      throw new Error('Expected every bar to be an HTML element')
    }
    return bar
  })
}

export function barContainerOf(card: HTMLElement): HTMLElement {
  const bars = card.querySelector<HTMLElement>('[data-stats-bars]')
  if (bars === null) {
    throw new Error(`The card has no element with the attribute data-stats-bars: ${card.textContent}`)
  }
  return bars
}

export function barPercentsOf(card: HTMLElement): number[] {
  return barsOf(card).map((bar) => {
    const height = bar.style.height
    if (!height.endsWith('%')) {
      throw new Error(`Expected the height of a bar in per cent, got "${height}"`)
    }
    return Number.parseFloat(height)
  })
}

export function allTimeLine(): HTMLElement {
  const line = document.querySelector<HTMLElement>('[data-stats-all-time]')
  if (line === null) {
    throw new Error('The screen has no element with the attribute data-stats-all-time')
  }
  return line
}

const statsOfGrouping: Record<string, unknown> = {
  day: novemberStats,
  week: weeklyStats,
  month: monthlyStats,
  year: yearlyStats,
}

export const answerByGrouping: StatsAnswerFor = (url) => {
  const grouping = new URL(url, 'http://kvit.test').searchParams.get('by') ?? ''
  const body = statsOfGrouping[grouping]
  if (body === undefined) {
    throw new Error(`The test has no statistics for the grouping "${grouping}" in ${url}`)
  }
  return Response.json(body)
}

export function axisLabelsOf(card: HTMLElement): string[] {
  const axis = card.querySelector('[data-stats-axis]')
  if (axis === null) {
    throw new Error(`The card has no element with the attribute data-stats-axis: ${card.textContent}`)
  }
  return Array.from(axis.children).map((label) => lineText(label))
}

export function chip(name: string): HTMLElement {
  return screen.getByRole('button', { name })
}

export function isPressed(button: HTMLElement): boolean {
  return button.getAttribute('aria-pressed') === 'true'
}

export function isDisabledChip(button: HTMLElement): boolean {
  return button.hasAttribute('disabled') || button.getAttribute('aria-disabled') === 'true'
}

export function pressChip(name: string): void {
  fireEvent.click(chip(name))
}

export function setCustomDates(from: string, to: string): void {
  fireEvent.change(screen.getByLabelText(translated('en', 'adminStats.custom.from')), { target: { value: from } })
  fireEvent.change(screen.getByLabelText(translated('en', 'adminStats.custom.to')), { target: { value: to } })
}

export function statsUrl(range: { from?: string; to?: string }, by: string): string {
  const parts = [
    range.from === undefined ? null : `from=${range.from}`,
    range.to === undefined ? null : `to=${range.to}`,
    `by=${by}`,
  ].filter((part) => part !== null)
  return `${adminStatsPath}?${parts.join('&')}`
}

export function cardRegionsTitles(language: Language = 'en'): string[] {
  return [
    'adminStats.card.signUps',
    'adminStats.card.activePeople',
    'adminStats.card.joinedViaInvite',
    'adminStats.card.groupsCreated',
    'adminStats.card.expensesAdded',
    'adminStats.card.paymentsConfirmed',
  ].map((key) => translated(language, key))
}
