import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { expenseRows, testCategories } from '@/test/expenseTestData'
import { categoriesPath, expensesPath } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { queuedExpense, withoutDisplayFields } from '@/test/outboxScreenTestData'
import {
  consoleErrorCallsAbout,
  discardSheet,
  discardThroughTheSheet,
  expectMalformedRowShown,
  findWaitingRowWith,
  rowWithoutText,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRows,
} from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { GroupExpenses } from './GroupExpenses'

async function renderWithOneBrokenExpense() {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
  stubFetchByRequest({
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: expenseRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
  })
  const waiting: OutboxItem[] = [queuedExpense('good'), withoutDisplayFields(queuedExpense('broken'))]
  seedOutbox(testMe.id, waiting)
  await renderElementWithProviders(<GroupExpenses groupId={testGroupId} actionsWhenEmpty={null} />, '/expenses', {
    seedCache: seedMe(testMe),
  })
  await findWaitingRowWith('Museum tickets')
  return consoleError
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('GroupExpenses with a stored waiting expense that has none of its display fields', () => {
  it('still shows the list of real expenses and the other waiting expense', async () => {
    await renderWithOneBrokenExpense()

    expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
    expect(waitingRows().some((row) => (row.textContent ?? '').includes('Museum tickets'))).toBe(true)
  })

  it('shows the broken expense as a visible waiting row titled with the dash of the real rows and "Not synced yet"', async () => {
    await renderWithOneBrokenExpense()

    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expectMalformedRowShown(rowWithoutText('Museum tickets'))
  })

  it('opens the discard sheet when the broken row is tapped', async () => {
    await renderWithOneBrokenExpense()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    fireEvent.click(tapTargetOf(expectMalformedRowShown(rowWithoutText('Museum tickets'))))

    expect(discardSheet()).toBeTruthy()
  })

  it('lets the person discard the broken expense', async () => {
    await renderWithOneBrokenExpense()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    await discardThroughTheSheet(expectMalformedRowShown(rowWithoutText('Museum tickets')))

    expect(savedIdsOfTheSignedInPerson()).toEqual(['good'])
  })

  it('logs the broken expense once with console.error, naming its id', async () => {
    const consoleError = await renderWithOneBrokenExpense()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'broken')).toHaveLength(1)
  })

  it('does not log the good expense', async () => {
    const consoleError = await renderWithOneBrokenExpense()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'good')).toEqual([])
  })
})
