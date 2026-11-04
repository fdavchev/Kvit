import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import { greeceTripBalances } from '@/test/balancesTestData'
import { testGroup, testGroupId } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import { queuedSettlement, withoutDisplayFields } from '@/test/outboxScreenTestData'
import {
  consoleErrorCallsAbout,
  discardSheet,
  discardThroughTheSheet,
  expectMalformedRowShown,
  findWaitingRowWith,
  renderGroupTab,
  rowWithoutText,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRows,
} from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { findCardsInLanguage } from '@/test/settlementActionsTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

async function renderWithOneBrokenPayment() {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
  const waiting: OutboxItem[] = [queuedSettlement('good'), withoutDisplayFields(queuedSettlement('broken'))]
  seedOutbox(testMe.id, waiting)
  await renderGroupTab(routes.groupBalances(testGroupId), {
    balances: greeceTripBalances,
    members: ownerViewMembers,
    group: testGroup,
    settlements: [],
  })
  await findCardsInLanguage('en')
  await findWaitingRowWith('Marko')
  return consoleError
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('the Payments list with a stored waiting payment that has none of its display fields', () => {
  it('still shows the Payments heading and the other waiting payment', async () => {
    await renderWithOneBrokenPayment()

    expect(await screen.findByRole('heading', { name: translated('en', 'settlements.payments') })).toBeTruthy()
    expect(waitingRows().some((row) => (row.textContent ?? '').includes('Marko'))).toBe(true)
  })

  it('shows the broken payment as a visible waiting row titled with the dash and "Not synced yet"', async () => {
    await renderWithOneBrokenPayment()

    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expectMalformedRowShown(rowWithoutText('Marko'))
  })

  it('opens the discard sheet when the broken row is tapped', async () => {
    await renderWithOneBrokenPayment()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    fireEvent.click(tapTargetOf(expectMalformedRowShown(rowWithoutText('Marko'))))

    expect(discardSheet()).toBeTruthy()
  })

  it('lets the person discard the broken payment', async () => {
    await renderWithOneBrokenPayment()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    await discardThroughTheSheet(expectMalformedRowShown(rowWithoutText('Marko')))

    expect(savedIdsOfTheSignedInPerson()).toEqual(['good'])
  })

  it('logs the broken payment once with console.error, naming its id', async () => {
    const consoleError = await renderWithOneBrokenPayment()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'broken')).toHaveLength(1)
  })

  it('does not log the good payment', async () => {
    const consoleError = await renderWithOneBrokenPayment()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'good')).toEqual([])
  })
})
