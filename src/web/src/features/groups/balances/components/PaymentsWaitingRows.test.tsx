import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import { routes } from '@/core/router/routes'
import { greeceTripBalances } from '@/test/balancesTestData'
import { sectionNamed } from '@/test/balancesTestHelpers'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { testGroup, testGroupId } from '@/test/groupTestData'
import { filipMember, markoMember, ownerViewMembers } from '@/test/memberTestData'
import {
  otherGroupId,
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
  renderGroupTab,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRows,
  waitingRowText,
  waitingRowTexts,
} from '@/test/outboxScreenTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { confirmedPaymentOf, paymentIds } from '@/test/settlementActionsTestData'
import { findCardsInLanguage } from '@/test/settlementActionsTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const markoPaidFilip = confirmedPaymentOf(markoMember, filipMember, 80000, {
  id: paymentIds.first,
  recordedByUserId: testMe.id,
  createdAt: '2026-10-06T08:00:00Z',
})

function anaPaysFilip(id: string, overrides: Parameters<typeof queuedSettlement>[1] = {}): OutboxItem {
  return queuedSettlement(id, {
    ...overrides,
    display: { payerName: 'Ana', receiverName: 'Filip', amountMinor: 50000, currency: 'MKD', ...overrides.display },
  })
}

async function renderPayments(
  options: { language?: Language; settlements?: Settlement[]; waiting?: OutboxItem[] } = {},
) {
  const language = options.language ?? 'en'
  seedOutbox(testMe.id, options.waiting ?? [])
  const rendered = await renderGroupTab(routes.groupBalances(testGroupId), {
    language,
    balances: greeceTripBalances,
    members: ownerViewMembers,
    group: testGroup,
    settlements: options.settlements ?? [markoPaidFilip],
  })
  await findCardsInLanguage(language)
  return rendered
}

function fromPaidTo(from: string, to: string, language: Language = 'en'): string {
  return translated(language, 'settlements.fromPaidTo', { from, to })
}

function realPaymentRows(): HTMLElement[] {
  return within(sectionNamed('Payments'))
    .getAllByRole('listitem')
    .filter((row) => !row.matches('[data-waiting-row]') && row.querySelector('[data-waiting-row]') === null)
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('the Payments list with changes waiting in the outbox', () => {
  describe('a waiting payment of this group', () => {
    it('is shown as a greyed row in the Payments list with who paid whom, the amount and "Not synced yet"', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })

      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      expect(sectionNamed('Payments').contains(row)).toBe(true)
      expect(waitingRowText(row)).toContain(money(50000, 'MKD'))
      expect(waitingRowText(row)).toContain(outboxText('en', 'outbox.notSynced'))
    })

    it.each(languages)('writes the row in the language of the app (%s)', async (language) => {
      await renderPayments({ language, waiting: [anaPaysFilip('waiting')] })

      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip', language))

      expect(waitingRowText(row)).toContain(outboxText(language, 'outbox.notSynced'))
    })

    it('is shown while it is being sent', async () => {
      const { outboxSender } = await renderPayments()

      await addSendingItem(outboxSender, anaPaysFilip('on-its-way'))

      expect(await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))).toBeTruthy()
    })

    it('is not a link and has a button to tap', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })

      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      expect(row.closest('a')).toBeNull()
      expect(tapTargetOf(row).tagName).toBe('BUTTON')
    })

    it('is shown also when the group has no confirmed payment yet', async () => {
      await renderPayments({ settlements: [], waiting: [anaPaysFilip('first-ever')] })

      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      expect(sectionNamed('Payments').contains(row)).toBe(true)
    })
  })

  describe('what is not shown', () => {
    it('shows no waiting row when nothing waits, and the Payments list is as before', async () => {
      await renderPayments()

      expect(waitingRows()).toHaveLength(0)
      expect(realPaymentRows()).toHaveLength(1)
    })

    it('shows no row for a payment that failed', async () => {
      await renderPayments({ waiting: [anaPaysFilip('refused', { state: 'failed' })] })

      await expectNoWaitingRows()
    })

    it('shows no row for a payment of another group', async () => {
      await renderPayments({ waiting: [anaPaysFilip('theirs', { groupId: otherGroupId })] })

      await expectNoWaitingRows()
    })

    it('shows no row for an expense, a One bill or a personal entry', async () => {
      await renderPayments({ waiting: [queuedExpense('expense'), queuedOneBill('bill'), queuedPersonalEntry('entry')] })

      await expectNoWaitingRows()
    })

    it('does not write the waiting payment into the saved data of the screen', async () => {
      const { queryClient } = await renderPayments({
        waiting: [anaPaysFilip('waiting', { display: { payerName: 'Zoran' } })],
      })
      await findWaitingRowWith(fromPaidTo('Zoran', 'Filip'))

      const cachedData = queryClient
        .getQueryCache()
        .getAll()
        .map((query) => JSON.stringify(query.state.data ?? null))
        .join('\n')

      expect(cachedData).not.toContain('Zoran')
    })
  })

  describe('where the waiting rows sit', () => {
    it('keeps the confirmed payments of the group', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })
      await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      const real = realPaymentRows()

      expect(real).toHaveLength(1)
      expect(real[0].textContent).toContain(fromPaidTo('Marko', 'Filip'))
    })

    it('puts a waiting row above the first confirmed payment', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })
      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      expect(isBefore(row, realPaymentRows()[0])).toBe(true)
    })

    it('puts the newest waiting row first', async () => {
      await renderPayments({
        waiting: [
          anaPaysFilip('older', { createdAt: '2026-10-06T10:00:00.000Z', display: { amountMinor: 10000 } }),
          anaPaysFilip('newer', { createdAt: '2026-10-06T10:05:00.000Z', display: { amountMinor: 20000 } }),
        ],
      })
      await waitFor(() => {
        expect(waitingRows()).toHaveLength(2)
      })

      expect(waitingRowTexts()[0]).toContain(money(20000, 'MKD'))
      expect(waitingRowTexts()[1]).toContain(money(10000, 'MKD'))
    })
  })

  describe('tapping a waiting row', () => {
    it('opens the sheet that asks whether to discard the change', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })
      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      fireEvent.click(tapTargetOf(row))

      expect(discardSheet()).toBeTruthy()
    })

    it('takes the payment out of the outbox, and the row off the screen, when Discard is pressed', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting'), anaPaysFilip('other', { display: { payerName: 'Marko' } })] })
      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))

      await discardThroughTheSheet(row)

      await waitFor(() => {
        expect(waitingRowTexts().some((text) => text.includes(fromPaidTo('Ana', 'Filip')))).toBe(false)
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['other'])
    })

    it('keeps the payment when the sheet is closed without discarding', async () => {
      await renderPayments({ waiting: [anaPaysFilip('waiting')] })
      const row = await findWaitingRowWith(fromPaidTo('Ana', 'Filip'))
      fireEvent.click(tapTargetOf(row))

      fireEvent.click(closeActionOfDiscardSheet())

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(savedIdsOfTheSignedInPerson()).toEqual(['waiting'])
    })
  })
})
