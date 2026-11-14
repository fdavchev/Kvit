import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect, vi, type Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { routeObjects } from '@/core/router/router'
import type { Me } from '@/core/services/me/meService'
import { stubGroupApp } from './activityTestHelpers'
import { pressChip } from './adminStatsTestHelpers'
import { describeRequest, type SentRequest } from './apiTestHelpers'
import { typeInto } from './formTestHelpers'
import { ownerToolsPath } from './ownerToolsTestData'
import { renderRoutesWithProviders } from './renderWithProviders'
import { jsonAnswer, type AnswerFactory } from './requestTestHelpers'
import { testAdminMe } from './testMe'
import { translated } from './translated'

export const ownerToolsAddress = '/admin/tools'

export type ToolsAnswer = Response | Error | Promise<Response>

export type ToolsAnswerFor = (request: SentRequest) => ToolsAnswer

export type ToolsTab = 'people' | 'groups' | 'log'

export interface OwnerToolsAppOptions {
  language?: Language
  path?: string
  me?: Me
  answers?: Record<string, ToolsAnswerFor>
  appAnswers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

export function answerWith(body: unknown): ToolsAnswerFor {
  return () => Response.json(body)
}

export function noContent(): ToolsAnswerFor {
  return () => new Response(null, { status: 204 })
}

export function stubOwnerToolsApp(options: OwnerToolsAppOptions = {}): Mock<typeof fetch> {
  const language = options.language ?? 'en'
  const everythingElse = stubGroupApp({
    language,
    answers: { 'GET /api/me': jsonAnswer({ ...(options.me ?? testAdminMe), language }), ...options.appAnswers },
  })
  const fetchMock = vi.fn<typeof fetch>(async (input, init) => {
    const url = String(input)
    if (!url.startsWith(ownerToolsPath)) {
      return everythingElse(input, init)
    }
    const request = describeRequest([input, init])
    const key = `${request.method ?? 'GET'} ${url.split('?')[0]}`
    const answerFor = options.answers?.[key]
    if (answerFor === undefined) {
      throw new Error(`The test did not expect the owner tools request ${key}`)
    }
    const answer = await answerFor(request)
    if (answer instanceof Error) {
      throw answer
    }
    return answer
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export async function renderOwnerTools(options: OwnerToolsAppOptions = {}) {
  const fetchMock = stubOwnerToolsApp(options)
  const rendered = await renderRoutesWithProviders(routeObjects, options.path ?? ownerToolsAddress, {
    language: options.language ?? 'en',
    seedCache: options.seedCache,
  })
  return { fetchMock, ...rendered }
}

export function toolsRequests(fetchMock: Mock<typeof fetch>): SentRequest[] {
  return fetchMock.mock.calls
    .filter(([input]) => String(input).startsWith(ownerToolsPath))
    .map((call) => describeRequest(call))
}

export function toolsUrls(fetchMock: Mock<typeof fetch>, method: 'GET' | 'POST'): string[] {
  return toolsRequests(fetchMock)
    .filter((request) => (request.method ?? 'GET') === method)
    .map((request) => request.url)
}

export function requestsToUrl(fetchMock: Mock<typeof fetch>, method: 'GET' | 'POST', url: string): SentRequest[] {
  return toolsRequests(fetchMock).filter((request) => (request.method ?? 'GET') === method && request.url === url)
}

export function findToolsTitle(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('heading', { level: 1, name: translated(language, 'ownerTools.title') })
}

export function openTab(tab: ToolsTab, language: Language = 'en'): void {
  pressChip(translated(language, `ownerTools.tabs.${tab}`))
}

export function searchPeopleFor(text: string, language: Language = 'en'): void {
  typeInto(translated(language, 'ownerTools.people.searchLabel'), text)
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'ownerTools.people.search') }))
}

export function findGroupNumbered(text: string, language: Language = 'en'): void {
  typeInto(translated(language, 'ownerTools.groups.searchLabel'), text)
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'ownerTools.groups.search') }))
}

export function resultsList(): HTMLElement | null {
  return within(screen.getByRole('main')).queryByRole('list')
}

export function personCard(name: string): HTMLElement {
  const card = screen.getByText(name).closest('li')
  if (card === null) {
    throw new Error(`The name "${name}" is not inside a list item`)
  }
  return card
}

export async function findPersonCard(name: string): Promise<HTMLElement> {
  await screen.findByText(name)
  return personCard(name)
}

export function buttonNamesIn(container: HTMLElement): string[] {
  return within(container)
    .queryAllByRole('button')
    .map((button) => (button.textContent ?? '').trim())
}

export function buttonIn(container: HTMLElement, name: string): HTMLElement {
  return within(container).getByRole('button', { name })
}

export async function findGroupCard(name: string): Promise<HTMLElement> {
  return screen.findByRole('region', { name })
}

export function findSheet(titleKey: string, name: string, language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, titleKey, { name }) })
}

export function reasonFieldIn(sheet: HTMLElement, language: Language = 'en'): HTMLInputElement {
  return within(sheet).getByLabelText<HTMLInputElement>(translated(language, 'ownerTools.reasonLabel'))
}

export function typeReason(sheet: HTMLElement, text: string, language: Language = 'en'): void {
  fireEvent.change(reasonFieldIn(sheet, language), { target: { value: text } })
}

export function confirmButtonIn(sheet: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(sheet).getByRole<HTMLButtonElement>('button', { name: translated(language, 'ownerTools.confirm') })
}

export function cancelButtonIn(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'settlements.cancel') })
}

export async function waitForNoSheet(): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).toBeNull()
  })
}
