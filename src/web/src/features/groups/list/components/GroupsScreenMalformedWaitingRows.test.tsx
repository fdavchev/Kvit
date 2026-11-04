import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import { flatGroupRow, greeceGroupRow, groupListOf } from '@/test/groupTestData'
import { queuedOneBill, withoutDisplayFields } from '@/test/outboxScreenTestData'
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
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { GroupsScreen } from './GroupsScreen'

async function renderWithOneBrokenOneBill() {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
  stubFetchByRequest({ 'GET /api/groups': jsonAnswer(groupListOf({ groups: [greeceGroupRow, flatGroupRow] })) })
  const waiting: OutboxItem[] = [queuedOneBill('good'), withoutDisplayFields(queuedOneBill('broken'))]
  seedOutbox(testMe.id, waiting)
  await renderRoutesWithProviders(
    [
      { path: routes.groups, element: <GroupsScreen /> },
      { path: '*', element: <p>another page</p> },
    ],
    routes.groups,
    { seedCache: seedMe(testMe) },
  )
  await findWaitingRowWith('Dinner at Rex')
  return consoleError
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('GroupsScreen with a stored waiting One bill that has none of its display fields', () => {
  it('still shows the groups and the other waiting One bill', async () => {
    await renderWithOneBrokenOneBill()

    expect(await screen.findByText(greeceGroupRow.name)).toBeTruthy()
    expect(waitingRows().some((row) => (row.textContent ?? '').includes('Dinner at Rex'))).toBe(true)
  })

  it('shows the broken One bill as a visible waiting row titled with the dash and "Not synced yet"', async () => {
    await renderWithOneBrokenOneBill()

    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expectMalformedRowShown(rowWithoutText('Dinner at Rex'))
  })

  it('opens the discard sheet when the broken row is tapped', async () => {
    await renderWithOneBrokenOneBill()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    fireEvent.click(tapTargetOf(expectMalformedRowShown(rowWithoutText('Dinner at Rex'))))

    expect(discardSheet()).toBeTruthy()
  })

  it('lets the person discard the broken One bill', async () => {
    await renderWithOneBrokenOneBill()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    await discardThroughTheSheet(expectMalformedRowShown(rowWithoutText('Dinner at Rex')))

    expect(savedIdsOfTheSignedInPerson()).toEqual(['good'])
  })

  it('logs the broken One bill once with console.error, naming its id', async () => {
    const consoleError = await renderWithOneBrokenOneBill()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'broken')).toHaveLength(1)
  })

  it('does not log the good One bill', async () => {
    const consoleError = await renderWithOneBrokenOneBill()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'good')).toEqual([])
  })
})
