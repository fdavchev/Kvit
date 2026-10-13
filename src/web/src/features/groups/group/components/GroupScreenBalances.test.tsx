import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routeObjects } from '@/core/router/router'
import { routes } from '@/core/router/routes'
import { problemResponse } from '@/test/apiTestHelpers'
import {
  activityTab,
  balancesTab,
  expensesTab,
  renderGroupApp,
  stubGroupApp,
  type GroupAppOptions,
} from '@/test/activityTestHelpers'
import {
  anaOwesThreeMkd,
  anaOwesTwoMkd,
  anaPaidFilipPending,
  balancesOf,
  balancesPath,
  bojanLeftAtZeroMkd,
  bojanOwesFilipMkd,
  everyoneEvenWithPendingPaymentBalances,
  everyoneIsKvitBalances,
  filipIsOwedByFourMkd,
  filipIsOwedByThreeMkd,
  fourPeopleBalances,
  greeceTripBalances,
  greeceTripEur,
  greeceTripInTwoCurrencies,
  greeceTripMkd,
  markoOwesAnaBalances,
  markoOwesGrandmaBalances,
  markoPaidAnaPending,
  markoPaidFilipPending,
  markoPaidGrandmaPending,
  noExpensesYetBalances,
  settlementIds,
  settlementOf,
  settlementsPath,
} from '@/test/balancesTestData'
import {
  expectAmountLook,
  expectRowWithAmountBeneath,
  findBalanceCards,
  findOnlyBalanceCard,
  queryAllSectionsNamed,
  queryTextIn,
  rowsOf,
  rowTextOf,
  rowWithText,
  sectionNamed,
  textIn,
} from '@/test/balancesTestHelpers'
import {
  freezeTime,
  groupPath,
  isBefore,
  membersPath,
  plainSpaces,
  restoreRealRetryPolicy,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { anaMember, anaViewMembers, filipMember, ownerViewMembers } from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { networkFailureAnswer, problemAnswer, requestCount } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const crashMessage = 'A screen crashed while rendering'
const simplifiedLine = 'Simplified: fewer payments, same totals.'
const kvitTitle = 'Everyone is kvit \u{1F389}'
const kvitHint = 'Nobody owes anything.'
const recordAnotherPayment = 'Record another payment'
const minus = '−'

const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const anaView: GroupAppOptions = { members: anaViewMembers, group: groupOf({ isOwner: false }) }

const anaPaidFilipPartOfTwo = settlementOf({
  id: settlementIds.anaToFilip,
  fromMemberId: anaMember.id,
  fromName: 'Ana',
  toMemberId: filipMember.id,
  toName: 'Filip',
  amountMinor: 40000,
})

async function showsGroupScreen(): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: testGroup.name })
}

function crashErrors(consoleError: MockInstance<typeof console.error>): Error[] {
  return consoleError.mock.calls
    .filter(([message]) => message === crashMessage)
    .map(([, error]) => error)
    .filter((error): error is Error => error instanceof Error)
}

function stubGroupAppThatNeverAnswers(path: string): void {
  const fetchMock = stubGroupApp()
  const answerRest = fetchMock.getMockImplementation()
  if (answerRest === undefined) {
    throw new Error('stubGroupApp installed no fetch')
  }
  fetchMock.mockImplementation((input, init) =>
    String(input) === path ? new Promise<Response>(() => {}) : answerRest(input, init),
  )
}

function everyoneRowOf(everyone: HTMLElement, name: string): HTMLElement {
  const row = rowsOf(everyone).find((candidate) => queryTextIn(candidate, name) !== null)
  if (row === undefined) {
    throw new Error(`Found no row of ${name} in the Everyone section: ${everyone.textContent}`)
  }
  return row
}

describe('GroupScreen with the Balances tab', () => {
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

  describe('the pill switch', () => {
    it('links Balances to its address under the group', async () => {
      await renderGroupApp(routes.group(groupId))
      await showsGroupScreen()

      expect(balancesTab().getAttribute('href')).toBe(routes.groupBalances(groupId))
      expect(routes.groupBalances(groupId)).toBe(`/groups/${groupId}/balances`)
    })

    it('marks Balances as the current page on the Balances tab and neither Expenses nor Activity', async () => {
      await renderGroupApp(routes.groupBalances(groupId))
      await showsGroupScreen()

      expect(balancesTab().getAttribute('aria-current')).toBe('page')
      expect(expensesTab().getAttribute('aria-current')).not.toBe('page')
      expect(activityTab().getAttribute('aria-current')).not.toBe('page')
    })

    it.each([
      ['the Expenses tab', routes.group(groupId)],
      ['the Activity tab', routes.groupActivity(groupId)],
    ])('does not mark Balances as the current page on %s', async (_name, path) => {
      await renderGroupApp(path)
      await showsGroupScreen()

      expect(balancesTab().getAttribute('aria-current')).not.toBe('page')
    })

    it.each(languages)('writes the name of the tab with the text balances.title (%s)', async (language) => {
      await renderGroupApp(routes.groupBalances(groupId), { language })
      await showsGroupScreen()

      expect(balancesTab(language).textContent).toBe(translated(language, 'balances.title'))
    })

    it('writes the name of the tab in English as Balances', async () => {
      await renderGroupApp(routes.groupBalances(groupId))
      await showsGroupScreen()

      expect(balancesTab().textContent).toBe('Balances')
    })

    it('opens the Balances tab when its link is pressed, and the Expenses tab again when that link is pressed', async () => {
      const { router } = await renderGroupApp(routes.group(groupId), { balances: greeceTripBalances, ...filipView })
      await showsGroupScreen()

      fireEvent.click(balancesTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupBalances(groupId))
      })
      expect(await findBalanceCards()).toHaveLength(1)
      expect(balancesTab().getAttribute('aria-current')).toBe('page')

      fireEvent.click(expensesTab())
      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(groupId))
      })
      expect(await screen.findByRole('link', { name: /Dinner/ })).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('keeps the group header with its name, size and the Members and Settings links on the Balances tab', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { group: groupOf({ memberCount: 3 }) })

      await showsGroupScreen()

      expect(screen.getByText('3 people · MKD')).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('en', 'members.title') }).getAttribute('href')).toBe(
        routes.groupMembers(groupId),
      )
      expect(screen.getByRole('link', { name: translated('en', 'group.settings') }).getAttribute('href')).toBe(
        routes.groupSettings(groupId),
      )
    })

    it('shows no + button, no expense rows and no activity on the Balances tab', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      expect(screen.queryByRole('link', { name: translated('en', 'expense.addTitle') })).toBeNull()
      expect(screen.queryByRole('link', { name: /Dinner/ })).toBeNull()
      expect(screen.queryByText(translated('en', 'activity.empty'))).toBeNull()
    })
  })

  describe('what the Balances tab asks for', () => {
    it('asks for the signed-in person, the group, its members, its balances and its payments, and for nothing else', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        ...filipView,
      })
      await findBalanceCards()

      const urls = fetchMock.mock.calls.map(([url]) => String(url)).sort()
      expect(urls).toEqual(['/api/me', groupPath, membersPath, balancesPath, settlementsPath].sort())
    })

    it('does not ask for the balances or the payments while the Expenses tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.group(groupId))
      await screen.findByRole('link', { name: /Dinner/ })

      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(0)
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(0)
    })

    it('does not ask for the balances or the payments while the Activity tab is open', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupActivity(groupId))
      await screen.findByText(translated('en', 'activity.empty'))

      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(0)
      expect(requestCount(fetchMock, 'GET', settlementsPath)).toBe(0)
    })

    it('keeps the balances in the cache under the key groups, the group id, balances', async () => {
      const { queryClient } = await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        ...filipView,
      })
      await findBalanceCards()

      expect(queryClient.getQueryData(['groups', groupId, 'balances'])).toEqual(greeceTripBalances)
    })

    it('keeps the payments in the cache under the key groups, the group id, settlements', async () => {
      const settlements = [markoPaidFilipPending]
      const { queryClient } = await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements,
        ...filipView,
      })
      await findBalanceCards()

      expect(queryClient.getQueryData(['groups', groupId, 'settlements'])).toEqual(settlements)
    })
  })

  describe('loading and failing', () => {
    it.each([
      ['the balances', balancesPath],
      ['the payments', settlementsPath],
    ])('shows a loading spinner and no card while %s are being asked', async (_name, path) => {
      stubGroupAppThatNeverAnswers(path)
      await renderRoutesWithProviders(routeObjects, routes.groupBalances(groupId))
      await showsGroupScreen()

      expect(screen.getByRole('status')).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
      expect(screen.queryByText(translated('en', 'balances.empty'))).toBeNull()
    })

    it.each([
      ['the balances', `GET ${balancesPath}`],
      ['the payments', `GET ${settlementsPath}`],
      ['the members', `GET ${membersPath}`],
    ])('shows the translated error with a retry button, not the cards, when %s cannot be loaded, and keeps the group header', async (_name, request) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        answers: { [request]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('shows the network message when the balances cannot be reached', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        answers: { [`GET ${balancesPath}`]: networkFailureAnswer() },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('shows the generic error with a retry button when the balances answer is malformed', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        answers: {
          [`GET ${balancesPath}`]: () =>
            Response.json({ ...greeceTripBalances, isEveryoneKvit: 'maybe' }),
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('asks for the balances again and shows them when the retry button is pressed', async () => {
      let tries = 0
      await renderGroupApp(routes.groupBalances(groupId), {
        ...filipView,
        answers: {
          [`GET ${balancesPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(greeceTripBalances)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findBalanceCards()).toHaveLength(1)
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it.each([
      [404, 'GROUP_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the balances only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderGroupApp(routes.groupBalances(groupId), {
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${balancesPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', balancesPath)).toBe(1)
    })

    it('shows a Go to Groups link and no retry button when the balances answer that the group is not found', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        answers: { [`GET ${balancesPath}`]: () => problemResponse(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })
  })

  describe('the balance card', () => {
    it('is called "Your balance" and shows no currency code when the group has one currency', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })

      const card = await findOnlyBalanceCard()

      expect(textIn(card, 'Your balance')).toBeTruthy()
      expect(sectionNamed('Your balance')).toBe(card)
    })

    it('shows what I am owed as a big plus amount in the owed colour, who owes me, and one "pays you" row for each person', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, '+1,500 MKD'), 'positive')
      expect(textIn(card, 'Marko and Ana owe you')).toBeTruthy()
      const rows = rowsOf(card)
      expect(rows).toHaveLength(2)
      expectRowWithAmountBeneath(rows[0], 'Marko pays you', '800 MKD')
      expectRowWithAmountBeneath(rows[1], 'Ana pays you', '700 MKD')
    })

    it('shows what I owe as a big minus amount with the real minus sign in the owing colour, who I owe, and one "You pay" row', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...anaView })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, `${minus}700 MKD`), 'negative')
      expect(textIn(card, 'You owe Filip')).toBeTruthy()
      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expectRowWithAmountBeneath(rows[0], 'You pay Filip', '700 MKD')
    })

    it('shows 0 with no sign and no colour, the line "All even" and no row when I owe nothing and am owed nothing', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: markoOwesGrandmaBalances, ...filipView })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, '0 MKD'), 'plain')
      expect(textIn(card, 'All even')).toBeTruthy()
      expect(rowsOf(card)).toEqual([])
    })

    it('shows "Marko owes you" for one person who owes me', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: markoOwesAnaBalances, ...anaView })
      const card = await findOnlyBalanceCard()

      expect(textIn(card, 'Marko owes you')).toBeTruthy()
    })

    it.each([
      ['two people', [anaOwesTwoMkd], 'You owe Filip and Grandma'],
      ['three people', [anaOwesThreeMkd], 'You owe Filip, Grandma and 1 more'],
    ])('writes the line for %s I owe', async (_name, currencies, line) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: balancesOf(currencies), ...anaView })
      const card = await findOnlyBalanceCard()

      expect(textIn(card, line)).toBeTruthy()
    })

    it.each([
      ['three people', [filipIsOwedByThreeMkd], 'Ana, Marko and 1 more owe you'],
      ['four people', [filipIsOwedByFourMkd], 'Ana, Marko and 2 more owe you'],
    ])('writes the line for %s who owe me', async (_name, currencies, line) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: balancesOf(currencies), ...filipView })
      const card = await findOnlyBalanceCard()

      expect(textIn(card, line)).toBeTruthy()
    })

    it('shows one row for each person who pays me when three or more people owe me', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([filipIsOwedByThreeMkd]),
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(rowsOf(card)).toHaveLength(3)
    })

    it('shows my pending payment as "You paid Filip 500 MKD" with "Waiting for Filip to confirm", with the amount of the payment and not the suggested amount, instead of the "You pay" row', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expect(textIn(rows[0], 'You paid Filip 500 MKD')).toBeTruthy()
      expect(textIn(rows[0], 'Waiting for Filip to confirm')).toBeTruthy()
      expect(queryTextIn(card, 'You pay Filip')).toBeNull()
    })

    it('keeps the big amount and the line under it while my payment is pending, because a pending payment is not counted yet', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, `${minus}700 MKD`), 'negative')
      expect(textIn(card, 'You owe Filip')).toBeTruthy()
    })

    it('replaces only the row of the person I paid and keeps the "You pay" row of the other person', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([anaOwesTwoMkd]),
        settlements: [anaPaidFilipPartOfTwo],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expect(rowsOf(card)).toHaveLength(2)
      expect(textIn(rowWithText(card, 'You paid Filip 400 MKD'), 'Waiting for Filip to confirm')).toBeTruthy()
      expectRowWithAmountBeneath(rowWithText(card, 'You pay Grandma'), 'You pay Grandma', '300 MKD')
      expect(queryTextIn(card, 'You pay Filip')).toBeNull()
    })

    it('shows no row for a payment that waits for my answer, because it lives in Needs you', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [markoPaidFilipPending],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expectRowWithAmountBeneath(rows[0], 'Ana pays you', '700 MKD')
      expect(queryTextIn(card, 'Marko pays you')).toBeNull()
    })

    it('shows no row at all when every payment to me waits for my answer, and still says who owes me', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [markoPaidFilipPending, anaPaidFilipPending],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(rowsOf(card)).toEqual([])
      expect(textIn(card, 'Marko and Ana owe you')).toBeTruthy()
      expectAmountLook(textIn(card, '+1,500 MKD'), 'positive')
    })

    it('does not let a pending payment in euros hide the suggested row of a payment in denars between the same two people', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...anaPaidFilipPending, currency: 'EUR', amountMinor: 5000 }],
        ...anaView,
      })
      const [mkdCard] = await findBalanceCards()

      expectRowWithAmountBeneath(rowsOf(mkdCard)[0], 'You pay Filip', '700 MKD')
      expect(rowsOf(mkdCard)).toHaveLength(1)
      expect(queryTextIn(mkdCard, 'Waiting for Filip to confirm')).toBeNull()
    })

    it('does not let a pending payment between two other people change my rows', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        settlements: [markoPaidFilipPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expectRowWithAmountBeneath(rows[0], 'You pay Filip', '200 MKD')
    })

    it.each(['Confirmed', 'Rejected', 'Cancelled'] as const)('does not replace the "You pay" row because of a %s payment', async (status) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...anaPaidFilipPending, status }],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expectRowWithAmountBeneath(rowsOf(card)[0], 'You pay Filip', '700 MKD')
      expect(queryTextIn(card, 'You paid Filip 500 MKD')).toBeNull()
    })

    it('shows the card for each currency, denars first, each with its own currency code in the title line', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripInTwoCurrencies,
        ...filipView,
      })

      const cards = await findBalanceCards()

      expect(cards).toHaveLength(2)
      expect(screen.getByRole('region', { name: /^Your balance.*MKD/ })).toBe(cards[0])
      expect(screen.getByRole('region', { name: /^Your balance.*EUR/ })).toBe(cards[1])
      expect(screen.queryByRole('region', { name: /^Your balance.*MKD.*EUR|^Your balance.*EUR.*MKD/ })).toBeNull()
    })

    it('shows the amounts, the line and the rows of each card in the currency of that card', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripInTwoCurrencies,
        ...filipView,
      })
      const [mkdCard, eurCard] = await findBalanceCards()

      expectAmountLook(textIn(mkdCard, '+1,500 MKD'), 'positive')
      expect(textIn(mkdCard, 'Marko and Ana owe you')).toBeTruthy()
      expect(rowsOf(mkdCard)).toHaveLength(2)
      expectAmountLook(textIn(eurCard, `${minus}€20.00`), 'negative')
      expect(textIn(eurCard, 'You owe Ana')).toBeTruthy()
      const eurRows = rowsOf(eurCard)
      expect(eurRows).toHaveLength(1)
      expectRowWithAmountBeneath(eurRows[0], 'You pay Ana', '€20.00')
    })

    it('writes the amounts in Macedonian with a dot for thousands, «ден.» and the real minus sign', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        language: 'mk',
        balances: greeceTripBalances,
        ...anaView,
      })

      expect(
        (await screen.findAllByText(`${minus}700 ден.`, { normalizer: plainSpaces })).length,
      ).toBeGreaterThan(0)
    })

    it('writes the positive amount in Macedonian with a plus sign and a dot for thousands', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        language: 'mk',
        balances: greeceTripBalances,
        ...filipView,
      })

      expect(
        (await screen.findAllByText('+1.500 ден.', { normalizer: plainSpaces })).length,
      ).toBeGreaterThan(0)
    })
  })

  describe('every pending payment I made', () => {
    const anaPaidFilipSecondPending = settlementOf({
      id: settlementIds.anaToFilipSecond,
      fromMemberId: anaMember.id,
      fromName: 'Ana',
      toMemberId: filipMember.id,
      toName: 'Filip',
      amountMinor: 20000,
    })
    const anaPaidFilipThreeHundred = { ...anaPaidFilipPending, amountMinor: 30000 }
    const filipPaidAnaPending = settlementOf({
      id: settlementIds.markoToAna,
      fromMemberId: filipMember.id,
      fromName: 'Filip',
      toMemberId: anaMember.id,
      toName: 'Ana',
      amountMinor: 30000,
    })

    it('gets a row of its own when every balance is 0, in a card that says "All even", with no kvit message', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: everyoneEvenWithPendingPaymentBalances,
        settlements: [{ ...anaPaidFilipPending, amountMinor: 70000 }],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, '0 MKD'), 'plain')
      expect(textIn(card, 'All even')).toBeTruthy()
      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expect(textIn(rows[0], 'You paid Filip 700 MKD')).toBeTruthy()
      expect(textIn(rows[0], 'Waiting for Filip to confirm')).toBeTruthy()
      expect(screen.queryByText(kvitTitle)).toBeNull()
      expect(screen.queryByText(kvitHint)).toBeNull()
    })

    it('gets a row of its own when the payment goes the other way than every suggested payment, and the suggested rows stay', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [filipPaidAnaPending],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(rowsOf(card)).toHaveLength(3)
      const paid = rowWithText(card, 'You paid Ana 300 MKD')
      expect(textIn(paid, 'Waiting for Ana to confirm')).toBeTruthy()
      expectRowWithAmountBeneath(rowWithText(card, 'Marko pays you'), 'Marko pays you', '800 MKD')
      expectRowWithAmountBeneath(rowWithText(card, 'Ana pays you'), 'Ana pays you', '700 MKD')
      expect(textIn(card, 'Marko and Ana owe you')).toBeTruthy()
    })

    it.each([
      ['smaller than', 50000, '500 MKD'],
      ['the same as', 70000, '700 MKD'],
      ['larger than', 90000, '900 MKD'],
    ])('shows the amount of the payment, and hides the suggested row, when the amount is %s the suggested 700 MKD', async (_name, amountMinor, amount) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...anaPaidFilipPending, amountMinor }],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expect(textIn(rows[0], `You paid Filip ${amount}`)).toBeTruthy()
      expect(queryTextIn(card, 'You pay Filip')).toBeNull()
    })

    it('gives each of two pending payments to the same person in the same currency its own row with its own amount, and hides the suggested row', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipThreeHundred, anaPaidFilipSecondPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(2)
      expect(textIn(rows[0], 'You paid Filip 300 MKD')).toBeTruthy()
      expect(textIn(rows[0], 'Waiting for Filip to confirm')).toBeTruthy()
      expect(textIn(rows[1], 'You paid Filip 200 MKD')).toBeTruthy()
      expect(textIn(rows[1], 'Waiting for Filip to confirm')).toBeTruthy()
      expect(queryTextIn(card, 'You pay Filip')).toBeNull()
    })

    it('keeps the big amount and the line under it while two payments are pending', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipThreeHundred, anaPaidFilipSecondPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, `${minus}700 MKD`), 'negative')
      expect(textIn(card, 'You owe Filip')).toBeTruthy()
    })

    it('gets a row in the card of its own currency, with the suggested row of that pair hidden, when the group has two currencies', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripInTwoCurrencies,
        settlements: [{ ...filipPaidAnaPending, currency: 'EUR', amountMinor: 2000 }],
        ...filipView,
      })
      const [mkdCard, eurCard] = await findBalanceCards()

      const eurRows = rowsOf(eurCard)
      expect(eurRows).toHaveLength(1)
      expect(textIn(eurRows[0], 'You paid Ana €20.00')).toBeTruthy()
      expect(textIn(eurRows[0], 'Waiting for Ana to confirm')).toBeTruthy()
      expect(rowsOf(mkdCard)).toHaveLength(2)
      expect(queryTextIn(mkdCard, 'You paid Ana €20.00')).toBeNull()
    })

    it('gets a card of its own, with a balance of 0 and "All even", when its currency is not in the answer at all', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([], false),
        settlements: [anaPaidFilipPending],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expectAmountLook(textIn(card, '0 MKD'), 'plain')
      expect(textIn(card, 'All even')).toBeTruthy()
      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expect(textIn(rows[0], 'You paid Filip 500 MKD')).toBeTruthy()
      expect(textIn(rows[0], 'Waiting for Filip to confirm')).toBeTruthy()
    })

    it('shows neither the empty message nor the kvit message when the only thing in the group is my pending payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([], false),
        settlements: [anaPaidFilipPending],
        ...anaView,
      })
      await findBalanceCards()

      expect(screen.queryByText(translated('en', 'balances.empty'))).toBeNull()
      expect(screen.queryByText(kvitTitle)).toBeNull()
      expect(screen.queryByText(kvitHint)).toBeNull()
    })

    it('gets a card of its own currency after the cards of the answer, when only another currency is in the answer', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...anaPaidFilipPending, currency: 'EUR', amountMinor: 5000 }],
        ...anaView,
      })

      const cards = await findBalanceCards()

      expect(cards).toHaveLength(2)
      expect(screen.getByRole('region', { name: /^Your balance.*MKD/ })).toBe(cards[0])
      expect(screen.getByRole('region', { name: /^Your balance.*EUR/ })).toBe(cards[1])
      expectAmountLook(textIn(cards[1], '€0.00'), 'plain')
      expect(textIn(cards[1], 'All even')).toBeTruthy()
      expect(textIn(rowsOf(cards[1])[0], 'You paid Filip €50.00')).toBeTruthy()
      expectRowWithAmountBeneath(rowsOf(cards[0])[0], 'You pay Filip', '700 MKD')
    })

    it('puts the card of a currency that is not in the answer after the denar card, not before it, when the answer has only euros', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([greeceTripEur]),
        settlements: [anaPaidFilipPending],
        ...anaView,
      })

      const cards = await findBalanceCards()

      expect(cards).toHaveLength(2)
      expect(screen.getByRole('region', { name: /^Your balance.*MKD/ })).toBe(cards[0])
      expect(screen.getByRole('region', { name: /^Your balance.*EUR/ })).toBe(cards[1])
    })

    it('shows no row for a pending payment that waits for my answer, also when its currency is not in the answer, and makes no card for it', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([], false),
        settlements: [markoPaidFilipPending],
        ...filipView,
      })

      const cards = rowsOf(await screen.findByRole('region', { name: 'Needs you' }))
      expect(textIn(cards[0], 'Marko says they paid you 800 MKD')).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('shows no row and no card for a pending payment between two other people when the answer has no currencies, and still says there is nothing to settle', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: noExpensesYetBalances,
        settlements: [markoPaidGrandmaPending],
        ...anaView,
      })

      expect(await screen.findByText(translated('en', 'balances.empty'))).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it.each(['Confirmed', 'Rejected', 'Cancelled'] as const)('shows no row and no card for my %s payment when its currency is not in the answer', async (status) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: noExpensesYetBalances,
        settlements: [{ ...anaPaidFilipPending, status }],
        ...anaView,
      })

      expect(await screen.findByText(translated('en', 'balances.empty'))).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('shows no row for a pending payment of someone else to a name, also for the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(rowsOf(card)).toEqual([])
      expect(textIn(card, 'All even')).toBeTruthy()
    })
  })

  describe('Needs you', () => {
    it('shows one card for each payment that waits for my answer, above the balance cards, each with the sentence and how long ago it was recorded', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [markoPaidFilipPending, anaPaidFilipPending],
        ...filipView,
      })
      const [balanceCard] = await findBalanceCards()

      const needsYou = sectionNamed('Needs you')
      const cards = rowsOf(needsYou)
      expect(cards).toHaveLength(2)
      expect(textIn(cards[0], 'Marko says they paid you 800 MKD')).toBeTruthy()
      expect(textIn(cards[0], '2 hours ago')).toBeTruthy()
      expect(textIn(cards[1], 'Ana says they paid you 500 MKD')).toBeTruthy()
      expect(textIn(cards[1], '1 day ago')).toBeTruthy()
      expect(isBefore(needsYou, balanceCard)).toBe(true)
    })

    it('writes the amount of the payment itself, not the suggested amount, when the payment is larger than the suggestion', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...markoPaidFilipPending, amountMinor: 100000 }],
        ...filipView,
      })
      await findBalanceCards()

      const [card] = rowsOf(sectionNamed('Needs you'))
      expect(textIn(card, 'Marko says they paid you 1,000 MKD')).toBeTruthy()
      expect(rowTextOf(card)).not.toContain('800 MKD')
    })

    it('writes the amount of the payment itself, not the suggested amount, when the payment is smaller than the suggestion', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...markoPaidFilipPending, amountMinor: 30000 }],
        ...filipView,
      })
      await findBalanceCards()

      const [card] = rowsOf(sectionNamed('Needs you'))
      expect(textIn(card, 'Marko says they paid you 300 MKD')).toBeTruthy()
    })

    it('writes the amount of the payment itself for a payment to a name without an account, when the owner answers for it', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [{ ...markoPaidGrandmaPending, amountMinor: 50000 }],
        ...filipView,
      })
      await findBalanceCards()

      const [card] = rowsOf(sectionNamed('Needs you'))
      expect(textIn(card, 'Marko says they paid Grandma 500 MKD')).toBeTruthy()
    })

    it('has a heading of level 2 called Needs you', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [markoPaidFilipPending],
        ...filipView,
      })
      await findBalanceCards()

      expect(screen.getByRole('heading', { level: 2, name: 'Needs you' })).toBeTruthy()
    })

    it('shows the owner a payment to a name without an account as "Marko says they paid Grandma 800 MKD"', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...filipView,
      })
      await findBalanceCards()

      const cards = rowsOf(sectionNamed('Needs you'))
      expect(cards).toHaveLength(1)
      expect(textIn(cards[0], 'Marko says they paid Grandma 800 MKD')).toBeTruthy()
      expect(textIn(cards[0], '2 hours ago')).toBeTruthy()
    })

    it('shows a payment to a name without an account to the owner and not to a member who is not the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...anaView,
      })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
      expect(screen.queryByText(/says they paid/)).toBeNull()
    })

    it('does not show the owner a payment to a person with an account who is not the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesAnaBalances,
        settlements: [markoPaidAnaPending],
        ...filipView,
      })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
    })

    it('shows the person with an account who receives the payment the card, even when she is not the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesAnaBalances,
        settlements: [markoPaidAnaPending],
        ...anaView,
      })
      await findBalanceCards()

      const cards = rowsOf(sectionNamed('Needs you'))
      expect(cards).toHaveLength(1)
      expect(textIn(cards[0], 'Marko says they paid you 800 MKD')).toBeTruthy()
    })

    it('shows no card for my own pending payment as the payer', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipPending],
        ...anaView,
      })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
      expect(screen.queryByText(/says they paid/)).toBeNull()
    })

    it('shows no Needs you section when nothing is pending', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
      expect(screen.queryByText('Needs you')).toBeNull()
    })

    it.each(['Confirmed', 'Rejected', 'Cancelled'] as const)('shows no card for a %s payment to me', async (status) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...markoPaidFilipPending, status }],
        ...filipView,
      })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Needs you')).toEqual([])
    })

    it('shows a payment in euros with its amount in euros', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripInTwoCurrencies,
        settlements: [{ ...markoPaidFilipPending, currency: 'EUR', amountMinor: 2000 }],
        ...filipView,
      })
      await findBalanceCards()

      const cards = rowsOf(sectionNamed('Needs you'))
      expect(textIn(cards[0], 'Marko says they paid you €20.00')).toBeTruthy()
    })

    it('shows the card of a payment that is older than a week with a date and not a relative time', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...markoPaidFilipPending, createdAt: '2026-09-20T09:00:00Z' }],
        ...filipView,
      })
      await findBalanceCards()

      const [card] = rowsOf(sectionNamed('Needs you'))
      expect(textIn(card, '20 Sep')).toBeTruthy()
    })
  })

  describe('Between others', () => {
    it('shows the payments that do not involve me, with the amount, in the order of the answer', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        ...anaView,
      })
      await findBalanceCards()

      const rows = rowsOf(sectionNamed('Between others'))
      expect(rows).toHaveLength(2)
      expect(textIn(rows[0], 'Marko pays Filip')).toBeTruthy()
      expect(rowTextOf(rows[0])).toContain('600 MKD')
      expect(textIn(rows[1], 'Grandma pays Filip')).toBeTruthy()
      expect(rowTextOf(rows[1])).toContain('400 MKD')
    })

    it('has a heading of level 2 called Between others', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: markoOwesGrandmaBalances, ...filipView })
      await findBalanceCards()

      expect(screen.getByRole('heading', { level: 2, name: 'Between others' })).toBeTruthy()
    })

    it('does not repeat in Between others the payments that involve me', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        ...anaView,
      })
      await findBalanceCards()

      const between = sectionNamed('Between others')
      expect(queryTextIn(between, 'Ana pays Filip')).toBeNull()
      expect(queryTextIn(between, 'You pay Filip')).toBeNull()
    })

    it('shows a "waiting" tag on the payment that has a pending payment between the same two people, and none on the others', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        settlements: [markoPaidFilipPending],
        ...anaView,
      })
      await findBalanceCards()

      const rows = rowsOf(sectionNamed('Between others'))
      expect(textIn(rows[0], 'waiting')).toBeTruthy()
      expect(rowTextOf(rows[0])).toContain('600 MKD')
      expect(queryTextIn(rows[1], 'waiting')).toBeNull()
    })

    it('shows one "waiting" tag, not two, on a payment that has two pending payments between the same two people', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        settlements: [
          markoPaidFilipPending,
          { ...markoPaidFilipPending, id: settlementIds.markoToFilipSecond, amountMinor: 10000 },
        ],
        ...anaView,
      })
      await findBalanceCards()

      const rows = rowsOf(sectionNamed('Between others'))
      expect(rows).toHaveLength(2)
      expect(within(rows[0]).getAllByText('waiting')).toHaveLength(1)
      expect(within(rows[1]).queryByText('waiting')).toBeNull()
    })

    it('shows the "waiting" tag on a payment between two other people that has a pending payment, also for the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...filipView,
      })
      await findBalanceCards()

      const [row] = rowsOf(sectionNamed('Between others'))
      expect(textIn(row, 'Marko pays Grandma')).toBeTruthy()
      expect(textIn(row, 'waiting')).toBeTruthy()
    })

    it('shows no waiting tag for a payment that is not pending any more', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [{ ...markoPaidGrandmaPending, status: 'Rejected' }],
        ...filipView,
      })
      await findBalanceCards()

      const [row] = rowsOf(sectionNamed('Between others'))
      expect(queryTextIn(row, 'waiting')).toBeNull()
    })

    it('shows no Between others section when every payment involves me', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      expect(queryAllSectionsNamed('Between others')).toEqual([])
      expect(screen.queryByText('Between others')).toBeNull()
    })

    it('sits below the balance card and above the Everyone section', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: fourPeopleBalances, ...anaView })
      const [card] = await findBalanceCards()

      expect(isBefore(card, sectionNamed('Between others'))).toBe(true)
      expect(isBefore(sectionNamed('Between others'), sectionNamed('Everyone'))).toBe(true)
    })
  })

  describe('Simplified: fewer payments, same totals.', () => {
    it('is shown once when there is at least one payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      expect(screen.getAllByText(simplifiedLine)).toHaveLength(1)
    })

    it('is shown once, not once for each currency, when two currencies have payments', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripInTwoCurrencies, ...filipView })
      await findBalanceCards()

      expect(screen.getAllByText(simplifiedLine)).toHaveLength(1)
    })

    it('is shown when the only payments are mine', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: markoOwesAnaBalances, ...anaView })
      await findBalanceCards()

      expect(screen.getAllByText(simplifiedLine)).toHaveLength(1)
    })

    it.each([
      ['everyone is kvit', everyoneIsKvitBalances],
      ['every balance is 0 but a payment is pending', everyoneEvenWithPendingPaymentBalances],
    ])('is not shown when there is no payment to suggest because %s', async (_name, balances) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances, ...filipView })
      await findBalanceCards()

      expect(screen.queryByText(simplifiedLine)).toBeNull()
    })
  })

  describe('Everyone', () => {
    it('lists every current member with the balance signed and coloured, zero too, in the order of the answer', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      const everyone = sectionNamed('Everyone')
      const rows = rowsOf(everyone)
      expect(rows).toHaveLength(4)
      expect(textIn(rows[0], 'Filip')).toBeTruthy()
      expectAmountLook(textIn(rows[0], '+1,500 MKD'), 'positive')
      expect(textIn(rows[1], 'Ana')).toBeTruthy()
      expectAmountLook(textIn(rows[1], `${minus}700 MKD`), 'negative')
      expect(textIn(rows[2], 'Marko')).toBeTruthy()
      expectAmountLook(textIn(rows[2], `${minus}800 MKD`), 'negative')
      expect(textIn(rows[3], 'Grandma')).toBeTruthy()
      expectAmountLook(textIn(rows[3], '0 MKD'), 'plain')
    })

    it('has a heading of level 2 called Everyone', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      await findBalanceCards()

      expect(screen.getByRole('heading', { level: 2, name: 'Everyone' })).toBeTruthy()
    })

    it('shows the same list to a member who is not the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...anaView })
      await findBalanceCards()

      expect(rowsOf(sectionNamed('Everyone'))).toHaveLength(4)
    })

    it('shows a person who left with a balance that is not 0, signed and coloured', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([bojanOwesFilipMkd]),
        ...filipView,
      })
      await findBalanceCards()

      const bojan = everyoneRowOf(sectionNamed('Everyone'), 'Bojan')
      expectAmountLook(textIn(bojan, `${minus}300 MKD`), 'negative')
    })

    it('hides a person who left and whose balance is 0, and still shows the current members who are at 0', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([bojanLeftAtZeroMkd]),
        ...filipView,
      })
      await findBalanceCards()

      const everyone = sectionNamed('Everyone')
      expect(queryTextIn(everyone, 'Bojan')).toBeNull()
      expect(rowsOf(everyone)).toHaveLength(4)
      expect(textIn(everyone, 'Marko')).toBeTruthy()
      expect(textIn(everyone, 'Grandma')).toBeTruthy()
    })

    it('sits below the balance card and ends before the link to record another payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const [card] = await findBalanceCards()

      expect(isBefore(card, sectionNamed('Everyone'))).toBe(true)
      expect(isBefore(sectionNamed('Everyone'), screen.getByText(recordAnotherPayment))).toBe(true)
    })

    it('lists the people of both currencies when the group has two currencies', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripInTwoCurrencies, ...filipView })
      await findBalanceCards()

      const text = queryAllSectionsNamed(/^Everyone/)
        .map((section) => rowTextOf(section))
        .join(' ')
      expect(text).toContain('+1,500 MKD')
      expect(text).toContain('+€20.00')
      expect(text).toContain(`${minus}€20.00`)
    })
  })

  describe('everyone is kvit, every balance is 0 but a payment is pending, and no expenses yet', () => {
    it('says "Everyone is kvit" and "Nobody owes anything." when everyone is kvit, and shows no list of payments and no Everyone section', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: everyoneIsKvitBalances, ...filipView })

      expect(await screen.findByText(kvitTitle)).toBeTruthy()
      expect(screen.getByText(kvitHint)).toBeTruthy()
      expect(queryAllSectionsNamed('Between others')).toEqual([])
      expect(queryAllSectionsNamed('Needs you')).toEqual([])
    })

    it('shows the link to record another payment when everyone is kvit', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: everyoneIsKvitBalances, ...filipView })
      await screen.findByText(kvitTitle)

      expect(screen.getByText(recordAnotherPayment)).toBeTruthy()
    })

    it('does not say that everyone is kvit when every balance is 0 but a payment is pending, and shows "All even" in the card', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: everyoneEvenWithPendingPaymentBalances,
        settlements: [{ ...anaPaidFilipPending, amountMinor: 70000 }],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(screen.queryByText(kvitTitle)).toBeNull()
      expect(screen.queryByText(kvitHint)).toBeNull()
      expect(textIn(card, 'All even')).toBeTruthy()
      expectAmountLook(textIn(card, '0 MKD'), 'plain')
    })

    it('shows the receiver of that pending payment a Needs you card, while the card says "All even"', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: everyoneEvenWithPendingPaymentBalances,
        settlements: [{ ...anaPaidFilipPending, amountMinor: 70000 }],
        ...filipView,
      })
      await findBalanceCards()

      const cards = rowsOf(sectionNamed('Needs you'))
      expect(cards).toHaveLength(1)
      expect(textIn(cards[0], 'Ana says they paid you 700 MKD')).toBeTruthy()
    })

    it('does not say that everyone is kvit to the payer of that pending payment either, and gives the payer the row of the payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: everyoneEvenWithPendingPaymentBalances,
        settlements: [{ ...anaPaidFilipPending, amountMinor: 70000 }],
        ...anaView,
      })
      const card = await findOnlyBalanceCard()

      expect(screen.queryByText(kvitTitle)).toBeNull()
      expect(textIn(card, 'All even')).toBeTruthy()
      const rows = rowsOf(card)
      expect(rows).toHaveLength(1)
      expect(textIn(rows[0], 'You paid Filip 700 MKD')).toBeTruthy()
      expect(textIn(rows[0], 'Waiting for Filip to confirm')).toBeTruthy()
    })

    it.each(languages)('says there is nothing to settle and shows no card when the group has no expenses yet (%s)', async (language) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        language,
        balances: noExpensesYetBalances,
        ...filipView,
      })

      expect(await screen.findByText(translated(language, 'balances.empty'))).toBeTruthy()
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('writes the message for a group with no expenses yet as "No expenses yet, so nothing to settle."', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: noExpensesYetBalances, ...filipView })

      expect(await screen.findByText('No expenses yet, so nothing to settle.')).toBeTruthy()
    })

    it('does not say that everyone is kvit for a group with no expenses yet, although the answer says so', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: noExpensesYetBalances, ...filipView })
      await screen.findByText(translated('en', 'balances.empty'))

      expect(screen.queryByText(kvitTitle)).toBeNull()
      expect(screen.queryByText(kvitHint)).toBeNull()
      expect(screen.queryByText(simplifiedLine)).toBeNull()
    })
  })

  describe('Record another payment', () => {
    it('is shown below everything else when there are balances', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const [card] = await findBalanceCards()

      const link = screen.getByText(recordAnotherPayment)
      expect(isBefore(card, link)).toBe(true)
      expect(isBefore(sectionNamed('Everyone'), link)).toBe(true)
    })

    it('is shown once', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripInTwoCurrencies, ...filipView })
      await findBalanceCards()

      expect(screen.getAllByText(recordAnotherPayment)).toHaveLength(1)
    })
  })

  describe('a balance that cannot be shown', () => {
    it('stops with an error instead of guessing when no member of the group is me', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        members: { ...ownerViewMembers, members: ownerViewMembers.members.map((member) => ({ ...member, isYou: false })) },
        group: testGroup,
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      await waitFor(() => {
        expect(crashErrors(consoleError)).not.toEqual([])
      })
      expect(screen.queryAllByRole('region', { name: /^Your balance/ })).toEqual([])
    })

    it('stops with an error instead of guessing when my row is missing in the balances of a currency', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      const withoutMe = {
        ...greeceTripMkd,
        balances: greeceTripMkd.balances.filter((row) => row.memberId !== filipMember.id),
      }
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: balancesOf([withoutMe]),
        ...filipView,
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      await waitFor(() => {
        expect(crashErrors(consoleError)).not.toEqual([])
      })
    })
  })

  describe('the order of the screen', () => {
    it('shows Needs you, then the balance cards, then Between others, then Simplified, then Everyone, then Record another payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: fourPeopleBalances,
        settlements: [{ ...markoPaidFilipPending, toMemberId: anaMember.id, toName: 'Ana' }],
        ...anaView,
      })
      const [card] = await findBalanceCards()

      const order: [string, HTMLElement][] = [
        ['Needs you', sectionNamed('Needs you')],
        ['the balance card', card],
        ['Between others', sectionNamed('Between others')],
        ['the simplified line', screen.getByText(simplifiedLine)],
        ['Everyone', sectionNamed('Everyone')],
        ['Record another payment', screen.getByText(recordAnotherPayment)],
      ]
      order.slice(1).forEach(([name, element], index) => {
        const [previousName, previous] = order[index]
        expect(isBefore(previous, element), `${name} should come after ${previousName}`).toBe(true)
      })
    })

    it('shows the pill switch above all of it', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const [card] = await findBalanceCards()

      expect(isBefore(balancesTab(), card)).toBe(true)
    })
  })

  describe('inside a card', () => {
    it('keeps every row of a card inside that card', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripInTwoCurrencies, ...filipView })
      const [mkdCard, eurCard] = await findBalanceCards()

      expect(within(mkdCard).queryByText('You pay Ana')).toBeNull()
      expect(within(eurCard).queryByText('Marko pays you')).toBeNull()
    })
  })
})
