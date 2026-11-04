import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { routes } from '@/core/router/routes'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { emptyGroupList, flatGroupRow, greeceGroupRow, groupListOf } from '@/test/groupTestData'
import {
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
import {
  addSendingItem,
  closeActionOfDiscardSheet,
  discardSheet,
  discardThroughTheSheet,
  expectNoWaitingRows,
  findWaitingRowWith,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRows,
  waitingRowText,
  waitingRowTexts,
} from '@/test/outboxScreenTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { GroupsScreen } from './GroupsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const receiptEmoji = '\u{1F9FE}'
const openGroupsList = groupListOf({ groups: [greeceGroupRow, flatGroupRow] })

async function renderGroups(options: { language?: Language; list?: unknown; waiting?: OutboxItem[] } = {}) {
  const language = options.language ?? 'en'
  stubFetchByRequest({ 'GET /api/groups': jsonAnswer(options.list ?? openGroupsList) })
  seedOutbox(testMe.id, options.waiting ?? [])
  return renderRoutesWithProviders(
    [
      { path: routes.groups, element: <GroupsScreen /> },
      { path: routes.newGroup, element: <p>new group page</p> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.recentlyDeletedGroups, element: <p>recently deleted page</p> },
    ],
    routes.groups,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('GroupsScreen with One bills waiting in the outbox', () => {
  describe('a waiting One bill', () => {
    it('is shown as a greyed row with the receipt look, its name, its amount and "Not synced yet"', async () => {
      await renderGroups({ waiting: [queuedOneBill('waiting')] })

      const row = await findWaitingRowWith('Dinner at Rex')

      expect(waitingRowText(row)).toContain(receiptEmoji)
      expect(waitingRowText(row)).toContain(money(360000, 'MKD'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.notSynced'))
    })

    it.each(languages)('writes "Not synced yet" in the language of the app (%s)', async (language) => {
      await renderGroups({ language, waiting: [queuedOneBill('waiting')] })

      const row = await findWaitingRowWith('Dinner at Rex')

      expect(waitingRowText(row)).toContain(outboxText(language, 'outbox.notSynced'))
    })

    it('is called by the name of the group it will become, when it has one', async () => {
      await renderGroups({
        waiting: [queuedOneBill('named', { display: { groupName: 'Bill · 6 Oct', title: 'Taxi home' } })],
      })

      const row = await findWaitingRowWith('Bill · 6 Oct')

      expect(waitingRowText(row)).not.toContain('Taxi home')
    })

    it('is called by its title when it has no group name', async () => {
      await renderGroups({
        waiting: [queuedOneBill('titled', { display: { groupName: undefined, title: 'Taxi home' } })],
      })

      expect(await findWaitingRowWith('Taxi home')).toBeTruthy()
    })

    it('is shown while it is being sent', async () => {
      const { outboxSender } = await renderGroups()
      await screen.findByRole('link', { name: /Greece trip/ })

      await addSendingItem(outboxSender, queuedOneBill('on-its-way'))

      expect(await findWaitingRowWith('Dinner at Rex')).toBeTruthy()
    })

    it('is not a link, because there is no group to open yet, and has a button to tap', async () => {
      await renderGroups({ waiting: [queuedOneBill('waiting')] })

      const row = await findWaitingRowWith('Dinner at Rex')

      expect(row.closest('a')).toBeNull()
      expect(tapTargetOf(row).tagName).toBe('BUTTON')
    })

    it('says that the euro price is set when sent, when the bill is in euros', async () => {
      await renderGroups({
        waiting: [queuedOneBill('euros', { display: { groupName: 'Ferry', title: 'Ferry', amountMinor: 4550, currency: 'EUR' } })],
      })

      const row = await findWaitingRowWith('Ferry')

      expect(waitingRowText(row)).toContain(money(4550, 'EUR'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.rateWhenSent'))
    })

    it('is shown also when the person has no group yet', async () => {
      await renderGroups({ list: emptyGroupList, waiting: [queuedOneBill('first-ever')] })

      expect(await findWaitingRowWith('Dinner at Rex')).toBeTruthy()
    })
  })

  describe('what is not shown', () => {
    it('shows no waiting row when nothing waits, and the list is as before', async () => {
      await renderGroups()
      await screen.findByRole('link', { name: /Greece trip/ })

      expect(waitingRows()).toHaveLength(0)
    })

    it('shows no row for a One bill that failed', async () => {
      await renderGroups({ waiting: [queuedOneBill('refused', { state: 'failed' })] })
      await screen.findByRole('link', { name: /Greece trip/ })

      await expectNoWaitingRows()
      expect(screen.queryByText('Dinner at Rex')).toBeNull()
    })

    it('shows no row for an expense, a payment or a personal entry', async () => {
      await renderGroups({ waiting: [queuedExpense('expense'), queuedSettlement('payment'), queuedPersonalEntry('entry')] })
      await screen.findByRole('link', { name: /Greece trip/ })

      await expectNoWaitingRows()
    })

    it('does not write the waiting One bill into the saved data of the screen', async () => {
      const { queryClient } = await renderGroups({ waiting: [queuedOneBill('waiting')] })
      await findWaitingRowWith('Dinner at Rex')

      const cachedData = queryClient
        .getQueryCache()
        .getAll()
        .map((query) => JSON.stringify(query.state.data ?? null))
        .join('\n')

      expect(cachedData).not.toContain('Dinner at Rex')
    })
  })

  describe('where the waiting rows sit', () => {
    it('keeps every real group as a link', async () => {
      await renderGroups({ waiting: [queuedOneBill('waiting')] })
      await findWaitingRowWith('Dinner at Rex')

      expect(screen.getByRole('link', { name: /Greece trip/ }).getAttribute('href')).toBe(routes.group(greeceGroupRow.id))
      expect(screen.getByRole('link', { name: /Flat 4B/ }).getAttribute('href')).toBe(routes.group(flatGroupRow.id))
    })

    it('puts a waiting row above the first real group', async () => {
      await renderGroups({ waiting: [queuedOneBill('waiting')] })
      const row = await findWaitingRowWith('Dinner at Rex')
      const firstGroup = await screen.findByRole('link', { name: /Greece trip/ })

      expect(isBefore(row, firstGroup)).toBe(true)
    })

    it('puts the newest waiting row first', async () => {
      await renderGroups({
        waiting: [
          queuedOneBill('older', { createdAt: '2026-10-06T10:00:00.000Z', display: { groupName: 'First queued' } }),
          queuedOneBill('newer', { createdAt: '2026-10-06T10:05:00.000Z', display: { groupName: 'Second queued' } }),
        ],
      })
      await findWaitingRowWith('Second queued')

      expect(waitingRowTexts()[0]).toContain('Second queued')
      expect(waitingRowTexts()[1]).toContain('First queued')
    })
  })

  describe('tapping a waiting row', () => {
    it('opens the sheet that asks whether to discard the change and leaves the page', async () => {
      const { router } = await renderGroups({ waiting: [queuedOneBill('waiting')] })
      const row = await findWaitingRowWith('Dinner at Rex')

      fireEvent.click(tapTargetOf(row))

      expect(discardSheet()).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.groups)
    })

    it('takes the One bill out of the outbox, and the row off the screen, when Discard is pressed', async () => {
      await renderGroups({
        waiting: [queuedOneBill('waiting'), queuedOneBill('other', { display: { groupName: 'Taxi home' } })],
      })
      const row = await findWaitingRowWith('Dinner at Rex')

      await discardThroughTheSheet(row)

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes('Dinner at Rex'))).toBe(false)
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['other'])
    })

    it('keeps the One bill when the sheet is closed without discarding', async () => {
      await renderGroups({ waiting: [queuedOneBill('waiting')] })
      const row = await findWaitingRowWith('Dinner at Rex')
      fireEvent.click(tapTargetOf(row))

      fireEvent.click(closeActionOfDiscardSheet())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['waiting'])
      expect(within(document.body).getAllByText(/Dinner at Rex/).length).toBeGreaterThan(0)
    })

    it('does not let a One bill that is being sent be discarded', async () => {
      const { outboxSender } = await renderGroups()
      await screen.findByRole('link', { name: /Greece trip/ })
      await addSendingItem(outboxSender, queuedOneBill('on-its-way'))
      const row = await findWaitingRowWith('Dinner at Rex')

      fireEvent.click(tapTargetOf(row))

      expect(
        within(discardSheet()).getByRole('button', { name: outboxText('en', 'outbox.sending') }).matches(':disabled'),
      ).toBe(true)
    })
  })
})
