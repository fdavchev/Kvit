import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { Settlement } from '@/core/services/settlements/settlementsService'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  anaPaidFilipPending,
  balancesPath,
  everyoneIsKvitBalances,
  greeceTripBalances,
  greeceTripInTwoCurrencies,
  markoPaidFilipPending,
  settlementPathOf,
  settlementsPath,
} from '@/test/balancesTestData'
import {
  findBalanceCards,
  queryAllSectionsNamed,
  rowsOf,
  rowTextOf,
  sectionNamed,
} from '@/test/balancesTestHelpers'
import { macedonianOf } from '@/test/englishTextKeys'
import { isDisabled } from '@/test/expenseFormTestHelpers'
import { findErrorText, freezeTime, isBefore, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import {
  anaMember,
  anaUserId,
  filipMember,
  grandmaMember,
  markoMember,
  ownerViewMembers,
  petarUserId,
} from '@/test/memberTestData'
import {
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
} from '@/test/requestTestHelpers'
import {
  anaSignedIn,
  confirmedPaymentOf,
  liveAnswers,
  paymentIds,
  seedActivityAndExpenses,
  type LiveGroup,
} from '@/test/settlementActionsTestData'
import {
  allButtonsNamed,
  buttonNamedIn,
  buttonsNamedIn,
  expectGroupAskedAgain,
  findCardsInLanguage,
  neverAnsweringFetch,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { testMe } from '@/test/testMe'
import {
  actionOf,
  pressToastAction,
  referenceToastStyling,
  shownToastTexts,
  stylingOf,
  toastShownWithText,
  toastsShownWithOptions,
} from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const paymentDeleted = 'Payment deleted'
const kvitTitle = 'Everyone is kvit \u{1F389}'

const markoPaidFilip = confirmedPaymentOf(markoMember, filipMember, 80000, {
  id: paymentIds.first,
  recordedByUserId: testMe.id,
  createdAt: '2026-10-06T08:00:00Z',
})
const anaPaidFilip = confirmedPaymentOf(anaMember, filipMember, 50000, {
  id: paymentIds.second,
  recordedByUserId: anaUserId,
  createdAt: '2026-10-05T09:00:00Z',
})
const filipPaidAnaInEuros = confirmedPaymentOf(filipMember, anaMember, 2000, {
  id: paymentIds.third,
  currency: 'EUR',
  recordedByUserId: testMe.id,
  createdAt: '2026-09-20T09:00:00Z',
})
const grandmaPaidMarkoLastYear = confirmedPaymentOf(grandmaMember, markoMember, 10000, {
  id: paymentIds.fourth,
  recordedByUserId: petarUserId,
  createdAt: '2025-12-31T21:30:00Z',
})

const withoutConfirmedPayments: [string, Settlement[]][] = [
  ['there are no payments at all', []],
  ['the only payment is pending', [markoPaidFilipPending]],
  [
    'the only payments are pending, rejected and cancelled',
    [
      markoPaidFilipPending,
      { ...anaPaidFilip, status: 'Rejected' },
      { ...anaPaidFilip, id: paymentIds.third, status: 'Cancelled' },
    ],
  ],
]

function paymentRows(): HTMLElement[] {
  return rowsOf(sectionNamed('Payments'))
}

function paymentRowWith(text: string): HTMLElement {
  const row = paymentRows().find((candidate) => rowTextOf(candidate).includes(text))
  if (row === undefined) {
    throw new Error(`Found no row of the Payments section with "${text}" in: ${sectionNamed('Payments').textContent}`)
  }
  return row
}

async function renderBalances(options: GroupAppOptions = {}) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    balances: greeceTripBalances,
    ...filipView,
    seedCache: seedActivityAndExpenses,
    ...options,
  })
  await findCardsInLanguage(options.language ?? 'en')
  return rendered
}

describe('the Payments list on the Balances tab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('which payments it shows', () => {
    it('has a heading of level 2 called Payments, and is a section with that name', async () => {
      await renderBalances({ settlements: [markoPaidFilip] })

      expect(screen.getByRole('heading', { level: 2, name: 'Payments' })).toBeTruthy()
      expect(sectionNamed('Payments')).toBeTruthy()
    })

    it('shows one row for each confirmed payment', async () => {
      await renderBalances({ settlements: [markoPaidFilip, anaPaidFilip, filipPaidAnaInEuros] })

      expect(paymentRows()).toHaveLength(3)
    })

    it('shows only the confirmed payments, not the pending, rejected or cancelled ones', async () => {
      await renderBalances({
        settlements: [
          markoPaidFilip,
          { ...anaPaidFilipPending, id: paymentIds.second, status: 'Pending' },
          { ...anaPaidFilip, id: paymentIds.third, status: 'Rejected' },
          { ...anaPaidFilip, id: paymentIds.fourth, status: 'Cancelled' },
        ],
      })

      expect(paymentRows()).toHaveLength(1)
      expect(rowTextOf(paymentRows()[0])).toContain('Marko paid Filip')
    })

    it.each(withoutConfirmedPayments)('is hidden, with neither the heading nor the section, when %s', async (_name, settlements) => {
      await renderBalances({ settlements })

      expect(queryAllSectionsNamed('Payments')).toEqual([])
      expect(screen.queryByText('Payments')).toBeNull()
      expect(screen.queryByRole('heading', { name: 'Payments' })).toBeNull()
    })

    it('is shown when everyone is kvit, because that is when payments are deleted by mistake', async () => {
      await renderBalances({ balances: everyoneIsKvitBalances, settlements: [markoPaidFilip] })

      expect(await screen.findByText(kvitTitle)).toBeTruthy()
      expect(paymentRows()).toHaveLength(1)
    })

    it('shows the same list to a member who is not the owner', async () => {
      await renderBalances({ settlements: [markoPaidFilip, anaPaidFilip], ...anaSignedIn() })

      expect(paymentRows()).toHaveLength(2)
    })

    it('sits below the balance cards and the Everyone section', async () => {
      await renderBalances({ settlements: [markoPaidFilip] })
      const [card] = await findBalanceCards()

      expect(isBefore(card, sectionNamed('Payments'))).toBe(true)
      expect(isBefore(sectionNamed('Everyone'), sectionNamed('Payments'))).toBe(true)
    })

    it('does not ask for anything that the Balances tab did not ask for already', async () => {
      const { fetchMock } = await renderBalances({ settlements: [markoPaidFilip] })

      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(1)
      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(1)
    })
  })

  describe('what a row says', () => {
    it('says who paid whom and the amount, with the amount in the currency of the payment', async () => {
      await renderBalances({ balances: greeceTripInTwoCurrencies, settlements: [markoPaidFilip, filipPaidAnaInEuros] })

      expect(rowTextOf(paymentRowWith('Marko paid Filip'))).toContain('800 MKD')
      expect(rowTextOf(paymentRowWith('Filip paid Ana'))).toContain('€20.00')
    })

    it('shows the newest payment first, whatever the order of the answer', async () => {
      await renderBalances({ settlements: [filipPaidAnaInEuros, markoPaidFilip, anaPaidFilip] })

      const texts = paymentRows().map((row) => rowTextOf(row))

      expect(texts[0]).toContain('Marko paid Filip')
      expect(texts[1]).toContain('Ana paid Filip')
      expect(texts[2]).toContain('Filip paid Ana')
    })

    it.each([
      ['today', markoPaidFilip, '6 Oct'],
      ['yesterday', anaPaidFilip, '5 Oct'],
      ['an older day of this year', filipPaidAnaInEuros, '20 Sep'],
      ['a day of another year', grandmaPaidMarkoLastYear, '31 Dec 2025'],
    ])('shows the date of a payment made %s the way the Expenses list shows dates', async (_name, settlement, date) => {
      await renderBalances({ settlements: [settlement] })

      expect(rowTextOf(paymentRows()[0])).toContain(date)
    })

    it('shows the date in the time zone of the person: a payment at 22:30 UTC on 5 October is 6 October in Skopje', async () => {
      await renderBalances({
        settlements: [{ ...markoPaidFilip, createdAt: '2026-10-05T22:30:00Z' }],
      })

      expect(rowTextOf(paymentRows()[0])).toContain('6 Oct')
      expect(rowTextOf(paymentRows()[0])).not.toContain('5 Oct')
    })

    it('writes the date and the amount in Macedonian for a person who uses Macedonian', async () => {
      await renderBalances({ language: 'mk', settlements: [markoPaidFilip] })
      const [row] = rowsOf(await screen.findByRole('region', { name: macedonianOf('Payments') }))

      expect(rowTextOf(row)).toContain('6 окт')
      expect(rowTextOf(row)).toContain('800 ден.')
    })
  })

  describe('Delete', () => {
    it('is on every row for the owner of the group, also on a payment that somebody else recorded', async () => {
      await renderBalances({ settlements: [markoPaidFilip, anaPaidFilip] })

      for (const row of paymentRows()) {
        expect(buttonsNamedIn(row, 'Delete')).toHaveLength(1)
      }
    })

    it('is on a payment that I recorded, and only on that one, for a member who is not the owner', async () => {
      await renderBalances({
        settlements: [markoPaidFilip, anaPaidFilip, grandmaPaidMarkoLastYear],
        ...anaSignedIn(),
      })

      expect(buttonsNamedIn(paymentRowWith('Ana paid Filip'), 'Delete')).toHaveLength(1)
      expect(buttonsNamedIn(paymentRowWith('Marko paid Filip'), 'Delete')).toEqual([])
      expect(buttonsNamedIn(paymentRowWith('Grandma paid Marko'), 'Delete')).toEqual([])
      expect(allButtonsNamed('Delete')).toHaveLength(1)
    })

    it('is nowhere for a member who is not the owner when somebody else recorded every payment', async () => {
      await renderBalances({ settlements: [markoPaidFilip], ...anaSignedIn() })

      expect(allButtonsNamed('Delete')).toEqual([])
    })

    it('is a real button that can take the focus', async () => {
      await renderBalances({ settlements: [markoPaidFilip] })

      const button = buttonNamedIn(paymentRows()[0], 'Delete')
      button.focus()

      expect(button.tagName).toBe('BUTTON')
      expect((button as HTMLButtonElement).type).toBe('button')
      expect(document.activeElement).toBe(button)
      expect(isDisabled(button)).toBe(false)
    })

    it('sends a delete request for the payment of that row, with no body, and asks no question first', async () => {
      const { fetchMock } = await renderBalances({
        settlements: [markoPaidFilip, anaPaidFilip],
        answers: { [`DELETE ${settlementPathOf(paymentIds.second)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(paymentRowWith('Ana paid Filip'), 'Delete'))

      expect(screen.queryByRole('dialog')).toBeNull()
      expect(screen.queryByRole('alertdialog')).toBeNull()
      await waitFor(() => {
        expect(requestCount(fetchMock, 'DELETE', settlementPathOf(paymentIds.second))).toBe(1)
      })
      expect(requestsOf(fetchMock, 'DELETE', settlementPathOf(paymentIds.second))[0].body).toBeUndefined()
      expect(requestCount(fetchMock, 'DELETE', settlementPathOf(paymentIds.first))).toBe(0)
    })

    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [markoPaidFilip],
        answers: { [`DELETE ${settlementPathOf(paymentIds.first)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('removes the row when the answers have changed, and hides the section when it was the last payment', async () => {
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilip, anaPaidFilip] }
      await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`DELETE ${settlementPathOf(paymentIds.first)}`]: () => {
            live.settlements = [anaPaidFilip]
            return new Response(null, { status: 204 })
          },
          [`DELETE ${settlementPathOf(paymentIds.second)}`]: () => {
            live.settlements = []
            return new Response(null, { status: 204 })
          },
        },
      })

      fireEvent.click(buttonNamedIn(paymentRowWith('Marko paid Filip'), 'Delete'))
      await waitFor(() => {
        expect(paymentRows()).toHaveLength(1)
      })
      fireEvent.click(buttonNamedIn(paymentRowWith('Ana paid Filip'), 'Delete'))

      await waitFor(() => {
        expect(queryAllSectionsNamed('Payments')).toEqual([])
      })
    })

    it('disables the button of the row while the request waits, and sends one request when it is pressed again', async () => {
      await renderBalances({ settlements: [markoPaidFilip, anaPaidFilip] })
      const neverAnswering = neverAnsweringFetch()
      const button = buttonNamedIn(paymentRowWith('Marko paid Filip'), 'Delete')

      fireEvent.click(button)
      await waitFor(() => {
        expect(isDisabled(button)).toBe(true)
      })
      fireEvent.click(button)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
      expect(isDisabled(buttonNamedIn(paymentRowWith('Ana paid Filip'), 'Delete'))).toBe(false)
    })

    it.each([
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'SETTLEMENT_NOT_FOUND'],
      [400, 'SETTLEMENT_NOT_CONFIRMED'],
    ])('shows the translated text for %i %s, keeps the row, enables Delete again and shows no Payment deleted toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock } = await renderBalances({
        settlements: [markoPaidFilip],
        answers: { [`DELETE ${settlementPathOf(paymentIds.first)}`]: problemAnswer(status, code) },
      })

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      await findErrorText(translated('en', `errors.${code}`))
      expect(paymentRows()).toHaveLength(1)
      expect(isDisabled(buttonNamedIn(paymentRows()[0], 'Delete'))).toBe(false)
      expect(shownToastTexts()).not.toContain(paymentDeleted)
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(1)
    })

    it('shows the network message when the server cannot be reached', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderBalances({
        settlements: [markoPaidFilip],
        answers: { [`DELETE ${settlementPathOf(paymentIds.first)}`]: networkFailureAnswer() },
      })

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      await findErrorText(translated('en', 'errors.network'))
      expect(shownToastTexts()).not.toContain(paymentDeleted)
    })
  })

  describe('the Undo toast', () => {
    async function renderWithLivePayment() {
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilip] }
      const rendered = await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`DELETE ${settlementPathOf(paymentIds.first)}`]: () => {
            live.settlements = []
            return new Response(null, { status: 204 })
          },
          [`POST ${settlementPathOf(paymentIds.first)}/restore`]: () => {
            live.settlements = [markoPaidFilip]
            return new Response(null, { status: 204 })
          },
        },
      })
      return { ...rendered, live }
    }

    it('shows Payment deleted with an Undo button for 4 seconds', async () => {
      await renderWithLivePayment()

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      expect(shown.options.duration).toBe(4000)
      expect(actionOf(shown).label).toBe('Undo')
    })

    it('has the wine-red look of the other delete toasts', async () => {
      const dangerStyling = referenceToastStyling(true)
      await renderWithLivePayment()

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      expect(stylingOf(shown)).toEqual(dangerStyling)
    })

    it('is not shown before the server has answered', async () => {
      await renderBalances({ settlements: [markoPaidFilip] })
      neverAnsweringFetch()

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      await waitFor(() => {
        expect(isDisabled(buttonNamedIn(paymentRows()[0], 'Delete'))).toBe(true)
      })
      expect(toastsShownWithOptions()).toEqual([])
    })

    it('is written in Macedonian for a person who uses Macedonian', async () => {
      await renderBalances({
        language: 'mk',
        settlements: [markoPaidFilip],
        answers: { [`DELETE ${settlementPathOf(paymentIds.first)}`]: noContentAnswer() },
      })
      const [row] = rowsOf(await screen.findByRole('region', { name: macedonianOf('Payments') }))

      fireEvent.click(buttonNamedIn(row, macedonianOf('Delete')))

      const shown = await waitFor(() => toastShownWithText(macedonianOf(paymentDeleted)))
      expect(actionOf(shown).label).toBe(translated('mk', 'common.undo'))
    })

    it('restores the payment when Undo is pressed, with no body, and not before', async () => {
      const { fetchMock } = await renderWithLivePayment()
      const restorePath = `${settlementPathOf(paymentIds.first)}/restore`
      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))
      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      expect(requestCount(fetchMock, 'POST', restorePath)).toBe(0)

      pressToastAction(shown)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', restorePath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', restorePath)[0].body).toBeUndefined()
    })

    it('brings the row back, after asking for the balances and the payments again', async () => {
      const { fetchMock } = await renderWithLivePayment()
      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))
      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      await waitFor(() => {
        expect(queryAllSectionsNamed('Payments')).toEqual([])
      })
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(2)

      pressToastAction(shown)

      await waitFor(() => {
        expect(paymentRows()).toHaveLength(1)
      })
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(3)
      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(3)
    })

    it('marks the activity and the expenses as out of date again after Undo', async () => {
      const { queryClient, fetchMock } = await renderWithLivePayment()
      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))
      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      await expectGroupAskedAgain(fetchMock, queryClient)
      seedActivityAndExpenses(queryClient)

      pressToastAction(shown)

      await waitFor(() => {
        expect(queryClient.getQueryState(['groups', groupId, 'activity'])?.isInvalidated).toBe(true)
        expect(queryClient.getQueryState(['groups', groupId, 'expenses'])?.isInvalidated).toBe(true)
      })
    })

    it('shows the translated error when the restore after Undo fails', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderBalances({
        settlements: [markoPaidFilip],
        answers: {
          [`DELETE ${settlementPathOf(paymentIds.first)}`]: noContentAnswer(),
          [`POST ${settlementPathOf(paymentIds.first)}/restore`]: problemAnswer(400, 'SETTLEMENT_NOT_DELETED'),
        },
      })
      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))
      const shown = await waitFor(() => toastShownWithText(paymentDeleted))

      pressToastAction(shown)

      await findErrorText(translated('en', 'errors.SETTLEMENT_NOT_DELETED'))
    })

    it('does not show an error for the delete when only asking again fails', async () => {
      let isAskingAgainBroken = false
      await renderBalances({
        settlements: [markoPaidFilip],
        answers: {
          [`DELETE ${settlementPathOf(paymentIds.first)}`]: () => {
            isAskingAgainBroken = true
            return new Response(null, { status: 204 })
          },
          [`GET ${settlementsPath}`]: () =>
            isAskingAgainBroken ? new Response(null, { status: 500 }) : Response.json({ settlements: [markoPaidFilip] }),
        },
      })

      fireEvent.click(buttonNamedIn(paymentRows()[0], 'Delete'))

      const shown = await waitFor(() => toastShownWithText(paymentDeleted))
      expect(actionOf(shown).label).toBe('Undo')
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })
})
