import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  anaOwesTwoMkd,
  anaPaidFilipPending,
  balancesOf,
  greeceTripBalances,
  markoOwesAnaBalances,
  markoOwesGrandmaBalances,
  markoPaidAnaPending,
  markoPaidFilipPending,
  markoPaidGrandmaPending,
  settlementIds,
  settlementOf,
  settlementPathOf,
  settlementsPath,
} from '@/test/balancesTestData'
import {
  findOnlyBalanceCard,
  queryAllSectionsNamed,
  queryTextIn,
  rowsOf,
  rowWithText,
  sectionNamed,
  textIn,
} from '@/test/balancesTestHelpers'
import { macedonianOf } from '@/test/englishTextKeys'
import { isDisabled } from '@/test/expenseFormTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { anaMember, filipMember, ownerViewMembers } from '@/test/memberTestData'
import {
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
} from '@/test/requestTestHelpers'
import {
  anaSignedIn,
  liveAnswers,
  onePaymentBalances,
  seedActivityAndExpenses,
  type LiveGroup,
} from '@/test/settlementActionsTestData'
import {
  allButtonsNamed,
  buttonNamedIn,
  buttonsNamedIn,
  expectGroupAskedAgain,
  expectGroupNotAskedAgain,
  findCardsInLanguage,
  neverAnsweringFetch,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const markoSentence = 'Marko says they paid you 800 MKD'
const anaSentence = 'Ana says they paid you 500 MKD'
const markoToGrandmaSentence = 'Marko says they paid Grandma 800 MKD'

function confirmPathOf(settlementId: string): string {
  return `${settlementPathOf(settlementId)}/confirm`
}

function rejectPathOf(settlementId: string): string {
  return `${settlementPathOf(settlementId)}/reject`
}

function cancelPathOf(settlementId: string): string {
  return `${settlementPathOf(settlementId)}/cancel`
}

function needsYouCard(sentence: string): HTMLElement {
  return rowWithText(sectionNamed('Needs you'), sentence)
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

const twoPendingToFilip = [markoPaidFilipPending, anaPaidFilipPending]

describe('answering and cancelling payments on the Balances tab', () => {
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

  describe('Confirm and Reject on a Needs you card', () => {
    it('are on each card, as real buttons called Confirm and Reject', async () => {
      await renderBalances({ settlements: twoPendingToFilip })

      for (const sentence of [markoSentence, anaSentence]) {
        const card = needsYouCard(sentence)
        const confirm = buttonNamedIn(card, 'Confirm')
        const reject = buttonNamedIn(card, 'Reject')
        expect(confirm.tagName).toBe('BUTTON')
        expect((confirm as HTMLButtonElement).type).toBe('button')
        expect(reject.tagName).toBe('BUTTON')
        expect((reject as HTMLButtonElement).type).toBe('button')
        expect(isDisabled(confirm)).toBe(false)
        expect(isDisabled(reject)).toBe(false)
      }
      expect(allButtonsNamed('Confirm')).toHaveLength(2)
      expect(allButtonsNamed('Reject')).toHaveLength(2)
    })

    it('can take the focus', async () => {
      await renderBalances({ settlements: [markoPaidFilipPending] })
      const card = needsYouCard(markoSentence)

      for (const name of ['Confirm', 'Reject']) {
        const button = buttonNamedIn(card, name)
        button.focus()
        expect(document.activeElement).toBe(button)
      }
    })

    it('are on the card of a payment to a name without an account, for the owner', async () => {
      await renderBalances({ balances: markoOwesGrandmaBalances, settlements: [markoPaidGrandmaPending] })

      const card = needsYouCard(markoToGrandmaSentence)

      expect(buttonsNamedIn(card, 'Confirm')).toHaveLength(1)
      expect(buttonsNamedIn(card, 'Reject')).toHaveLength(1)
    })

    it('are on the card of a payment to me when I am not the owner', async () => {
      await renderBalances({
        balances: markoOwesAnaBalances,
        settlements: [markoPaidAnaPending],
        ...anaSignedIn(),
      })

      const card = needsYouCard('Marko says they paid you 800 MKD')

      expect(buttonsNamedIn(card, 'Confirm')).toHaveLength(1)
      expect(buttonsNamedIn(card, 'Reject')).toHaveLength(1)
    })

    it('are nowhere when the payment to a name without an account is seen by a member who is not the owner', async () => {
      await renderBalances({
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...anaSignedIn(),
      })

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
      expect(allButtonsNamed('Confirm')).toEqual([])
      expect(allButtonsNamed('Reject')).toEqual([])
    })

    it('are nowhere when the pending payment is my own as the payer', async () => {
      await renderBalances({ settlements: [anaPaidFilipPending], ...anaSignedIn() })

      expect(allButtonsNamed('Confirm')).toEqual([])
      expect(allButtonsNamed('Reject')).toEqual([])
    })

    it('are nowhere when nothing waits for my answer', async () => {
      await renderBalances()

      expect(allButtonsNamed('Confirm')).toEqual([])
      expect(allButtonsNamed('Reject')).toEqual([])
    })

    it('are written in Macedonian for a person who uses Macedonian', async () => {
      await renderBalances({ language: 'mk', settlements: [markoPaidFilipPending] })
      const [card] = rowsOf(await screen.findByRole('region', { name: translated('mk', 'balances.needsYou') }))

      expect(buttonsNamedIn(card, macedonianOf('Confirm'))).toHaveLength(1)
      expect(buttonsNamedIn(card, macedonianOf('Reject'))).toHaveLength(1)
    })
  })

  describe('Confirm', () => {
    it('sends a confirm request for that payment, with no body', async () => {
      const { fetchMock } = await renderBalances({
        settlements: twoPendingToFilip,
        answers: { [`POST ${confirmPathOf(settlementIds.anaToFilip)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(anaSentence), 'Confirm'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', confirmPathOf(settlementIds.anaToFilip))).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', confirmPathOf(settlementIds.anaToFilip))[0].body).toBeUndefined()
      expect(requestCount(fetchMock, 'POST', confirmPathOf(settlementIds.markoToFilip))).toBe(0)
    })

    it('also works for the owner on the card of a payment to a name without an account', async () => {
      const { fetchMock } = await renderBalances({
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        answers: { [`POST ${confirmPathOf(settlementIds.markoToGrandma)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(markoToGrandmaSentence), 'Confirm'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', confirmPathOf(settlementIds.markoToGrandma))).toBe(1)
      })
    })

    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: { [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Confirm'))

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('removes the card and shows the payment in the list of payments, when the answers have changed', async () => {
      const confirmed = { ...markoPaidFilipPending, status: 'Confirmed' as const, resolvedAt: '2026-10-06T09:00:00Z' }
      const live: LiveGroup = { balances: greeceTripBalances, settlements: twoPendingToFilip }
      await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: () => {
            live.balances = onePaymentBalances(anaMember, filipMember, 70000)
            live.settlements = [confirmed, anaPaidFilipPending]
            return new Response(null, { status: 204 })
          },
        },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Confirm'))

      await waitFor(() => {
        expect(queryTextIn(sectionNamed('Needs you'), markoSentence)).toBeNull()
      })
      expect(textIn(sectionNamed('Needs you'), anaSentence)).toBeTruthy()
      expect(textIn(sectionNamed('Payments'), 'Marko paid Filip')).toBeTruthy()
    })

    it('disables both buttons of the card while the request waits, and leaves the other card alone', async () => {
      await renderBalances({ settlements: twoPendingToFilip })
      const neverAnswering = neverAnsweringFetch()

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Confirm'))

      await waitFor(() => {
        expect(isDisabled(buttonNamedIn(needsYouCard(markoSentence), 'Confirm'))).toBe(true)
      })
      expect(isDisabled(buttonNamedIn(needsYouCard(markoSentence), 'Reject'))).toBe(true)
      expect(isDisabled(buttonNamedIn(needsYouCard(anaSentence), 'Confirm'))).toBe(false)
      expect(isDisabled(buttonNamedIn(needsYouCard(anaSentence), 'Reject'))).toBe(false)
      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it('sends one request, not two, when Confirm is pressed again while it waits for the answer', async () => {
      await renderBalances({ settlements: [markoPaidFilipPending] })
      const neverAnswering = neverAnsweringFetch()
      const confirm = buttonNamedIn(needsYouCard(markoSentence), 'Confirm')

      fireEvent.click(confirm)
      await waitFor(() => {
        expect(isDisabled(confirm)).toBe(true)
      })
      fireEvent.click(confirm)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'SETTLEMENT_NOT_PENDING'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'SETTLEMENT_NOT_FOUND'],
    ])('shows the translated text for %i %s on that card, and the card stays with its buttons enabled', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock, queryClient } = await renderBalances({
        settlements: twoPendingToFilip,
        answers: { [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: problemAnswer(status, code) },
      })
      const card = needsYouCard(markoSentence)

      fireEvent.click(buttonNamedIn(card, 'Confirm'))

      const alert = await within(card).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(isDisabled(buttonNamedIn(card, 'Confirm'))).toBe(false)
      expect(isDisabled(buttonNamedIn(card, 'Reject'))).toBe(false)
      expect(within(needsYouCard(anaSentence)).queryByRole('alert')).toBeNull()
      expectGroupNotAskedAgain(fetchMock, queryClient)
    })

    it('shows the network message on that card when the server cannot be reached', async () => {
      await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: { [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: networkFailureAnswer() },
      })
      const card = needsYouCard(markoSentence)

      fireEvent.click(buttonNamedIn(card, 'Confirm'))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('takes the error away when the next try works', async () => {
      let tries = 0
      await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: {
          [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: () => {
            tries += 1
            return tries === 1 ? problemAnswer(400, 'SETTLEMENT_NOT_PENDING')() : new Response(null, { status: 204 })
          },
        },
      })
      const card = needsYouCard(markoSentence)
      fireEvent.click(buttonNamedIn(card, 'Confirm'))
      await within(card).findByRole('alert')

      fireEvent.click(buttonNamedIn(card, 'Confirm'))

      await waitFor(() => {
        expect(tries).toBe(2)
      })
      expect(within(card).queryByRole('alert')).toBeNull()
    })

    it('does not show an error for the confirm when only asking again fails', async () => {
      let isAskingAgainBroken = false
      const { fetchMock } = await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: {
          [`POST ${confirmPathOf(settlementIds.markoToFilip)}`]: () => {
            isAskingAgainBroken = true
            return new Response(null, { status: 204 })
          },
          [`GET ${settlementsPath}`]: () =>
            isAskingAgainBroken ? new Response(null, { status: 500 }) : Response.json({ settlements: [markoPaidFilipPending] }),
        },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Confirm'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', confirmPathOf(settlementIds.markoToFilip))).toBe(1)
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(2)
      })
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })

  describe('Reject', () => {
    it('sends a reject request for that payment, with no body', async () => {
      const { fetchMock } = await renderBalances({
        settlements: twoPendingToFilip,
        answers: { [`POST ${rejectPathOf(settlementIds.anaToFilip)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(anaSentence), 'Reject'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', rejectPathOf(settlementIds.anaToFilip))).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', rejectPathOf(settlementIds.anaToFilip))[0].body).toBeUndefined()
      expect(requestCount(fetchMock, 'POST', confirmPathOf(settlementIds.anaToFilip))).toBe(0)
    })

    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: { [`POST ${rejectPathOf(settlementIds.markoToFilip)}`]: noContentAnswer() },
      })

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Reject'))

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('removes the card and shows the suggested row of that pair again, with its Mark paid button', async () => {
      const rejected = { ...markoPaidFilipPending, status: 'Rejected' as const, resolvedAt: '2026-10-06T09:00:00Z' }
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilipPending] }
      await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`POST ${rejectPathOf(settlementIds.markoToFilip)}`]: () => {
            live.settlements = [rejected]
            return new Response(null, { status: 204 })
          },
        },
      })
      const card = await findOnlyBalanceCard()
      expect(queryTextIn(card, 'Marko pays you')).toBeNull()

      fireEvent.click(buttonNamedIn(needsYouCard(markoSentence), 'Reject'))

      await waitFor(() => {
        expect(buttonsNamedIn(rowWithText(card, 'Marko pays you'), 'Mark paid')).toHaveLength(1)
      })
      expect(queryAllSectionsNamed('Needs you')).toEqual([])
      expect(queryAllSectionsNamed('Payments')).toEqual([])
    })

    it('disables both buttons of the card while the request waits', async () => {
      await renderBalances({ settlements: twoPendingToFilip })
      neverAnsweringFetch()

      fireEvent.click(buttonNamedIn(needsYouCard(anaSentence), 'Reject'))

      await waitFor(() => {
        expect(isDisabled(buttonNamedIn(needsYouCard(anaSentence), 'Reject'))).toBe(true)
      })
      expect(isDisabled(buttonNamedIn(needsYouCard(anaSentence), 'Confirm'))).toBe(true)
      expect(isDisabled(buttonNamedIn(needsYouCard(markoSentence), 'Reject'))).toBe(false)
    })

    it.each([
      [400, 'SETTLEMENT_NOT_PENDING'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
    ])('shows the translated text for %i %s on that card, and the card stays', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [markoPaidFilipPending],
        answers: { [`POST ${rejectPathOf(settlementIds.markoToFilip)}`]: problemAnswer(status, code) },
      })
      const card = needsYouCard(markoSentence)

      fireEvent.click(buttonNamedIn(card, 'Reject'))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(isDisabled(buttonNamedIn(card, 'Reject'))).toBe(false)
      expectGroupNotAskedAgain(fetchMock, queryClient)
    })
  })

  describe('Cancel on a row that waits for the answer of the receiver', () => {
    const anaPaidFilipThreeHundred = { ...anaPaidFilipPending, amountMinor: 30000 }
    const anaPaidFilipSecond = settlementOf({
      id: settlementIds.anaToFilipSecond,
      fromMemberId: anaMember.id,
      fromName: 'Ana',
      toMemberId: filipMember.id,
      toName: 'Filip',
      amountMinor: 20000,
    })
    const waitingRowText = 'You paid Filip 500 MKD'

    function waitingRow(): HTMLElement {
      return rowWithText(screen.getByRole('region', { name: /^Your balance/ }), waitingRowText)
    }

    it('is on the row, as a real button called Cancel', async () => {
      await renderBalances({ settlements: [anaPaidFilipPending], ...anaSignedIn() })

      const cancel = buttonNamedIn(waitingRow(), 'Cancel')

      expect(cancel.tagName).toBe('BUTTON')
      expect((cancel as HTMLButtonElement).type).toBe('button')
      cancel.focus()
      expect(document.activeElement).toBe(cancel)
      expect(isDisabled(cancel)).toBe(false)
    })

    it('is on each of two waiting rows, and cancels the payment of the row that was pressed', async () => {
      const { fetchMock } = await renderBalances({
        settlements: [anaPaidFilipThreeHundred, anaPaidFilipSecond],
        ...anaSignedIn({
          answers: { [`POST ${cancelPathOf(settlementIds.anaToFilipSecond)}`]: noContentAnswer() },
        }),
      })
      const card = await findOnlyBalanceCard()
      const [first, second] = rowsOf(card)
      expect(buttonsNamedIn(first, 'Cancel')).toHaveLength(1)

      fireEvent.click(buttonNamedIn(second, 'Cancel'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', cancelPathOf(settlementIds.anaToFilipSecond))).toBe(1)
      })
      expect(requestCount(fetchMock, 'POST', cancelPathOf(settlementIds.anaToFilip))).toBe(0)
    })

    it('is not on a suggested row', async () => {
      await renderBalances({ balances: balancesOf([anaOwesTwoMkd]), settlements: [], ...anaSignedIn() })

      expect(allButtonsNamed('Cancel')).toEqual([])
    })

    it('is not on a Needs you card of the receiver', async () => {
      await renderBalances({ settlements: [markoPaidFilipPending] })

      expect(allButtonsNamed('Cancel')).toEqual([])
    })

    it('sends a cancel request with no body', async () => {
      const { fetchMock } = await renderBalances({
        settlements: [anaPaidFilipPending],
        ...anaSignedIn({
          answers: { [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: noContentAnswer() },
        }),
      })

      fireEvent.click(buttonNamedIn(waitingRow(), 'Cancel'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', cancelPathOf(settlementIds.anaToFilip))).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', cancelPathOf(settlementIds.anaToFilip))[0].body).toBeUndefined()
    })

    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [anaPaidFilipPending],
        ...anaSignedIn({
          answers: { [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: noContentAnswer() },
        }),
      })

      fireEvent.click(buttonNamedIn(waitingRow(), 'Cancel'))

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('brings back the suggested row and its Mark paid button when the answers have changed', async () => {
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [anaPaidFilipPending] }
      await renderBalances(
        anaSignedIn({
          answers: {
            ...liveAnswers(live),
            [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: () => {
              live.settlements = [{ ...anaPaidFilipPending, status: 'Cancelled' }]
              return new Response(null, { status: 204 })
            },
          },
        }),
      )
      expect(allButtonsNamed('Mark paid')).toEqual([])

      fireEvent.click(buttonNamedIn(waitingRow(), 'Cancel'))

      const card = await findOnlyBalanceCard()
      await waitFor(() => {
        expect(buttonsNamedIn(rowWithText(card, 'You pay Filip'), 'Mark paid')).toHaveLength(1)
      })
      expect(queryTextIn(card, waitingRowText)).toBeNull()
      expect(allButtonsNamed('Cancel')).toEqual([])
    })

    it('disables the button of the row while the request waits, and leaves the other row alone', async () => {
      await renderBalances({ settlements: [anaPaidFilipThreeHundred, anaPaidFilipSecond], ...anaSignedIn() })
      const [first, second] = rowsOf(await findOnlyBalanceCard())
      neverAnsweringFetch()

      fireEvent.click(buttonNamedIn(first, 'Cancel'))

      await waitFor(() => {
        expect(isDisabled(buttonNamedIn(first, 'Cancel'))).toBe(true)
      })
      expect(isDisabled(buttonNamedIn(second, 'Cancel'))).toBe(false)
    })

    it('sends one request, not two, when Cancel is pressed again while it waits for the answer', async () => {
      await renderBalances({ settlements: [anaPaidFilipPending], ...anaSignedIn() })
      const neverAnswering = neverAnsweringFetch()
      const cancel = buttonNamedIn(waitingRow(), 'Cancel')

      fireEvent.click(cancel)
      await waitFor(() => {
        expect(isDisabled(cancel)).toBe(true)
      })
      fireEvent.click(cancel)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'SETTLEMENT_NOT_PENDING'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'SETTLEMENT_NOT_FOUND'],
    ])('shows the translated text for %i %s on that row, and the row stays with its button enabled', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock, queryClient } = await renderBalances({
        settlements: [anaPaidFilipThreeHundred, anaPaidFilipSecond],
        ...anaSignedIn({
          answers: { [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: problemAnswer(status, code) },
        }),
      })
      const [first, second] = rowsOf(await findOnlyBalanceCard())

      fireEvent.click(buttonNamedIn(first, 'Cancel'))

      expect((await within(first).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(isDisabled(buttonNamedIn(first, 'Cancel'))).toBe(false)
      expect(within(second).queryByRole('alert')).toBeNull()
      expect(textIn(first, 'You paid Filip 300 MKD')).toBeTruthy()
      expectGroupNotAskedAgain(fetchMock, queryClient)
    })

    it('shows the network message on that row when the server cannot be reached', async () => {
      await renderBalances({
        settlements: [anaPaidFilipPending],
        ...anaSignedIn({
          answers: { [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: networkFailureAnswer() },
        }),
      })
      const row = waitingRow()

      fireEvent.click(buttonNamedIn(row, 'Cancel'))

      expect((await within(row).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('does not show an error for the cancel when only asking again fails', async () => {
      let isAskingAgainBroken = false
      const { fetchMock } = await renderBalances({
        settlements: [anaPaidFilipPending],
        ...anaSignedIn({
          answers: {
            [`POST ${cancelPathOf(settlementIds.anaToFilip)}`]: () => {
              isAskingAgainBroken = true
              return new Response(null, { status: 204 })
            },
            [`GET ${settlementsPath}`]: () =>
              isAskingAgainBroken ? new Response(null, { status: 500 }) : Response.json({ settlements: [anaPaidFilipPending] }),
          },
        }),
      })

      fireEvent.click(buttonNamedIn(waitingRow(), 'Cancel'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(2)
      })
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })
})
