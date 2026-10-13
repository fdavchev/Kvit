import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect, vi, type Mock } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { Currency } from '@/shared/utils/formatMoney'
import { balancesPath, settlementsPath } from './balancesTestData'
import { rowWithText, sectionNamed } from './balancesTestHelpers'
import { activityKey, expensesKey } from './settlementActionsTestData'
import { sentBody } from './expenseFormTestHelpers'
import { startsWith } from './expenseTestHelpers'
import { requestCount } from './requestTestHelpers'
import { translated } from './translated'

export const recordedSettlementKeys = [
  'amountMinor',
  'clientRequestId',
  'currency',
  'fromMemberId',
  'toMemberId',
]

export async function findCardsInLanguage(language: Language): Promise<HTMLElement[]> {
  return screen.findAllByRole('region', { name: startsWith(translated(language, 'balances.yourBalance')) })
}

export function allButtonsNamed(name: string): HTMLElement[] {
  return screen.queryAllByRole('button', { name: startsWith(name) })
}

export function buttonsNamedIn(container: HTMLElement, name: string): HTMLElement[] {
  return within(container).queryAllByRole('button', { name: startsWith(name) })
}

export function buttonNamedIn(container: HTMLElement, name: string): HTMLElement {
  return within(container).getByRole('button', { name: startsWith(name) })
}

export function betweenOthersRow(text: string): HTMLElement {
  return rowWithText(sectionNamed('Between others'), text)
}

export function needsYouCards(): HTMLElement[] {
  return within(sectionNamed('Needs you')).getAllByRole('listitem')
}

export async function openMarkPaid(row: HTMLElement, sheetTitle: string): Promise<HTMLElement> {
  fireEvent.click(buttonNamedIn(row, 'Mark paid'))
  return screen.findByRole('dialog', { name: sheetTitle })
}

export function sheetButton(sheet: HTMLElement, name: string): HTMLElement {
  return within(sheet).getByRole('button', { name })
}

export function isPressed(chip: HTMLElement): boolean {
  return chip.getAttribute('aria-pressed') === 'true'
}

export function neverAnsweringFetch(): Mock<typeof fetch> {
  const neverAnswering = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
  vi.stubGlobal('fetch', neverAnswering)
  return neverAnswering
}

export function sentBodies(fetchMock: Mock<typeof fetch>): Record<string, unknown>[] {
  return fetchMock.mock.calls.map(([, init]) => {
    const body: unknown = JSON.parse(String(init?.body))
    if (typeof body !== 'object' || body === null || Array.isArray(body)) {
      throw new Error(`Expected every request to have a JSON object body, found ${JSON.stringify(body)}`)
    }
    return body as Record<string, unknown>
  })
}

export function recordedBody(fetchMock: Mock<typeof fetch>, index = 0): Record<string, unknown> {
  return sentBody(fetchMock, 'POST', settlementsPath, index)
}

export async function expectRecorded(
  fetchMock: Mock<typeof fetch>,
  expected: {
    fromMemberId: string
    toMemberId: string
    amountMinor: number
    currency: Currency
  },
): Promise<void> {
  await waitFor(() => {
    expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
  })
  const body = recordedBody(fetchMock)
  expect(Object.keys(body).sort()).toEqual(recordedSettlementKeys)
  expect(body).toMatchObject(expected)
}

export async function expectGroupAskedAgain(
  fetchMock: Mock<typeof fetch>,
  queryClient: QueryClient,
): Promise<void> {
  await waitFor(() => {
    expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(2)
    expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(2)
  })
  await waitFor(() => {
    expect(queryClient.getQueryState(activityKey)?.isInvalidated).toBe(true)
    expect(queryClient.getQueryState(expensesKey)?.isInvalidated).toBe(true)
  })
}

export function expectGroupNotAskedAgain(
  fetchMock: Mock<typeof fetch>,
  queryClient: QueryClient,
): void {
  expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(1)
  expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(1)
  expect(queryClient.getQueryState(activityKey)?.isInvalidated).toBe(false)
  expect(queryClient.getQueryState(expensesKey)?.isInvalidated).toBe(false)
}

export function classTokens(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

export function tokensStartingWith(element: HTMLElement, prefixes: readonly string[]): string[] {
  return classTokens(element)
    .filter((token) => prefixes.some((prefix) => token.startsWith(prefix)))
    .sort()
}
