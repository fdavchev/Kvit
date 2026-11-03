import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { expect } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { OutboxSender } from '@/core/outbox/outboxSender'
import { routes } from '@/core/router/routes'
import { GroupScreen } from '@/features/groups/group/components/GroupScreen'
import { stubGroupApp, type GroupAppOptions } from './activityTestHelpers'
import { plainSpaces } from './expenseTestHelpers'
import { outboxText } from './outboxTextsTestData'
import { readSavedOutbox } from './queuedSaveTestHelpers'
import { renderRoutesWithProviders } from './renderWithProviders'
import { seedMe, testMe } from './testMe'
import { translated } from './translated'

export function waitingRows(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('[data-waiting-row]'))
}

export function waitingRowText(row: HTMLElement): string {
  return plainSpaces(row.textContent ?? '')
}

export function waitingRowTexts(): string[] {
  return waitingRows().map(waitingRowText)
}

export function waitingRowWith(text: string): HTMLElement {
  const row = waitingRows().find((candidate) => waitingRowText(candidate).includes(text))
  if (row === undefined) {
    throw new Error(`Found no waiting row with "${text}" in: ${JSON.stringify(waitingRowTexts())}`)
  }
  return row
}

export async function findWaitingRowWith(text: string): Promise<HTMLElement> {
  await waitFor(() => {
    expect(
      waitingRowTexts().some((rowText) => rowText.includes(text)),
      `No waiting row with "${text}" is on the screen; the waiting rows are: ${JSON.stringify(waitingRowTexts())}`,
    ).toBe(true)
  })
  return waitingRowWith(text)
}

export function tapTargetOf(row: HTMLElement): HTMLElement {
  if (row instanceof HTMLButtonElement) {
    return row
  }
  const button = row.querySelector('button')
  if (button === null) {
    throw new Error(`The waiting row has no button to tap: ${row.outerHTML}`)
  }
  return button
}

export async function addSendingItem(outboxSender: OutboxSender, item: OutboxItem): Promise<void> {
  await act(async () => {
    await outboxSender.enqueue({ ...item, state: 'sending' })
  })
}

export function discardSheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: outboxText(language, 'outbox.discardTitle') })
}

export function closeActionOfDiscardSheet(language: Language = 'en'): HTMLElement {
  const names = [translated(language, 'settlements.cancel'), translated(language, 'common.close')]
  const found = names
    .map((name) => within(discardSheet(language)).queryByRole('button', { name }))
    .find((button) => button !== null)
  if (found === undefined || found === null) {
    throw new Error(`The discard sheet has no close action named ${names.map((name) => `"${name}"`).join(' or ')}`)
  }
  return found
}

export async function discardThroughTheSheet(row: HTMLElement, language: Language = 'en'): Promise<void> {
  fireEvent.click(tapTargetOf(row))
  const sheet = discardSheet(language)
  fireEvent.click(within(sheet).getByRole('button', { name: outboxText(language, 'outbox.discard') }))
  await waitFor(() => {
    expect(screen.queryByRole('dialog')).toBeNull()
  })
}

export async function expectNoWaitingRows(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 30))
  expect(waitingRowTexts()).toEqual([])
}

export function savedIdsOfTheSignedInPerson(): string[] {
  return readSavedOutbox(testMe.id).map((item) => item.id)
}

export function noteText(count: number, language: Language = 'en'): string {
  return outboxText(language, 'outbox.notCounted', count)
}

export async function renderGroupTab(path: string, options: GroupAppOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubGroupApp(options)
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.group(':groupId'), element: <GroupScreen /> },
      { path: routes.groupBalances(':groupId'), element: <GroupScreen /> },
      { path: routes.groupTotals(':groupId'), element: <GroupScreen /> },
      { path: routes.groupActivity(':groupId'), element: <GroupScreen /> },
      { path: routes.groups, element: <p>groups page</p> },
      { path: '*', element: <p>another page</p> },
    ],
    path,
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...testMe, language, timeZone: options.timeZone ?? testMe.timeZone })(queryClient)
        options.seedCache?.(queryClient)
      },
    },
  )
  return { fetchMock, ...rendered }
}
