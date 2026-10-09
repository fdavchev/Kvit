import type { QueryClient } from '@tanstack/react-query'
import { screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { expect, vi } from 'vitest'
import { createQueryClient } from '@/core/api/queryClient'
import type { Language } from '@/core/i18n/language'
import type { Category } from '@/core/services/categories/categoriesService'
import { testNow } from './expenseTestData'
import { testGroupId } from './groupTestData'
import { createTestPersister } from './testPersister'
import { translated } from './translated'

export const groupPath = `/api/groups/${testGroupId}`
export const membersPath = `${groupPath}/members`
export const expensesPath = `${groupPath}/expenses`
export const deletedExpensesPath = `${expensesPath}/deleted`
export const categoriesPath = '/api/categories'

export const uuidFormat = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/

export function expensePathOf(expenseId: string): string {
  return `${expensesPath}/${expenseId}`
}

export function expenseRestorePathOf(expenseId: string): string {
  return `${expensePathOf(expenseId)}/restore`
}

export function restoreRealRetryPolicy(queryClient: QueryClient): void {
  const current = queryClient.getDefaultOptions()
  queryClient.setDefaultOptions({
    ...current,
    queries: { ...current.queries, retry: createQueryClient(createTestPersister()).getDefaultOptions().queries?.retry },
  })
}

export function freezeTime(): void {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(testNow)
}

export function unfreezeTime(): void {
  vi.useRealTimers()
}

export function plainSpaces(text: string): string {
  return text.replace(/\u00a0/g, ' ')
}

export function categoryName(language: Language, category: Category): string {
  return translated(language, `categories.${category.key}`)
}

export function isoSecondsAgo(seconds: number): string {
  return new Date(testNow.getTime() - seconds * 1000).toISOString()
}

export function elementWithAll(fragments: readonly string[], maxExtraLength: number): HTMLElement {
  const plainFragments = fragments.map((fragment) => plainSpaces(fragment))
  const maxLength = plainFragments.reduce((total, fragment) => total + fragment.length, maxExtraLength)

  function holdsAll(element: Element): boolean {
    const text = plainSpaces(element.textContent ?? '')
    return plainFragments.every((fragment) => text.includes(fragment))
  }

  const found = Array.from(document.body.querySelectorAll<HTMLElement>('*')).filter(
    (element) =>
      holdsAll(element) &&
      plainSpaces(element.textContent ?? '').length <= maxLength &&
      !Array.from(element.children).some((child) => holdsAll(child)),
  )
  if (found.length === 0) {
    throw new Error(
      `Found no element that holds ${JSON.stringify(fragments)} in at most ${maxLength} characters`,
    )
  }
  return found[0]
}

export function escapeForRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

export function startsWith(text: string): RegExp {
  return new RegExp(`^${escapeForRegExp(text)}`)
}

export function isBefore(first: Node, second: Node): boolean {
  return (first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0
}

export async function findErrorText(text: string): Promise<void> {
  await waitFor(() => {
    const isInAlert: boolean = screen
      .queryAllByRole('alert')
      .some((alert) => (alert.textContent ?? '').includes(text))
    const isInToast: boolean = vi
      .mocked(toast.error)
      .mock.calls.some(([message]) => message === text)
    expect(isInAlert || isInToast).toBe(true)
  })
}
