import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { GroupBalances } from '@/core/services/balances/balancesService'
import type { GroupMember } from '@/core/services/groups/membersService'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import type { Currency } from '@/shared/utils/formatMoney'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  anaPaidFilipPending,
  balancesPath,
  everyoneIsKvitBalances,
  greeceTripBalances,
  greeceTripInTwoCurrencies,
  markoOwesGrandmaBalances,
  markoPaidFilipPending,
  markoPaidGrandmaPending,
  settlementsPath,
} from '@/test/balancesTestData'
import {
  findBalanceCards,
  findOnlyBalanceCard,
  queryTextIn,
  rowsOf,
  rowWithText,
  sectionNamed,
  textIn,
} from '@/test/balancesTestHelpers'
import {
  amountField,
  isDisabled,
  saveButton,
  typeAmount,
  waitForSheetToClose,
} from '@/test/expenseFormTestHelpers'
import { freezeTime, unfreezeTime, uuidFormat } from '@/test/expenseTestHelpers'
import { macedonianOf } from '@/test/englishTextKeys'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { testGroup } from '@/test/groupTestData'
import {
  anaMember,
  filipMember,
  grandmaMember,
  markoMember,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
} from '@/test/requestTestHelpers'
import {
  anaSignedIn,
  confirmedPaymentOf,
  liveAnswers,
  onePaymentBalances,
  pendingPaymentOf,
  seedActivityAndExpenses,
  type LiveGroup,
} from '@/test/settlementActionsTestData'
import {
  allButtonsNamed,
  betweenOthersRow,
  buttonNamedIn,
  buttonsNamedIn,
  expectGroupAskedAgain,
  expectGroupNotAskedAgain,
  expectRecorded,
  findCardsInLanguage,
  neverAnsweringFetch,
  openMarkPaid,
  recordedBody,
  sentBodies,
  sheetButton,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }
const countsRightAway = 'Counts right away.'
const kvitTitle = 'Everyone is kvit \u{1F389}'

function waitsUntil(name: string): string {
  return `Waits until ${name} confirms. You can cancel until then.`
}

interface MarkPaidScenario {
  name: string
  options: GroupAppOptions
  balances: GroupBalances
  rowText: string
  cardIndex: number | null
  title: string
  note: string
  from: GroupMember
  to: GroupMember
  amountMinor: number
  currency: Currency
}

const scenarios: MarkPaidScenario[] = [
  {
    name: 'I am owed by a person',
    options: filipView,
    balances: onePaymentBalances(markoMember, filipMember, 80000),
    rowText: 'Marko pays you',
    cardIndex: 0,
    title: 'Marko paid me',
    note: countsRightAway,
    from: markoMember,
    to: filipMember,
    amountMinor: 80000,
    currency: 'MKD',
  },
  {
    name: 'I am owed, and I am not the owner',
    options: anaSignedIn(),
    balances: onePaymentBalances(markoMember, anaMember, 25000),
    rowText: 'Marko pays you',
    cardIndex: 0,
    title: 'Marko paid me',
    note: countsRightAway,
    from: markoMember,
    to: anaMember,
    amountMinor: 25000,
    currency: 'MKD',
  },
  {
    name: 'I owe a person with an account',
    options: anaSignedIn(),
    balances: onePaymentBalances(anaMember, filipMember, 70000),
    rowText: 'You pay Filip',
    cardIndex: 0,
    title: 'I paid Filip',
    note: waitsUntil('Filip'),
    from: anaMember,
    to: filipMember,
    amountMinor: 70000,
    currency: 'MKD',
  },
  {
    name: 'I owe a name without an account, and I am not the owner',
    options: anaSignedIn(),
    balances: onePaymentBalances(anaMember, markoMember, 25000),
    rowText: 'You pay Marko',
    cardIndex: 0,
    title: 'I paid Marko',
    note: waitsUntil('Marko'),
    from: anaMember,
    to: markoMember,
    amountMinor: 25000,
    currency: 'MKD',
  },
  {
    name: 'I owe a name without an account, and I am the owner',
    options: filipView,
    balances: onePaymentBalances(filipMember, grandmaMember, 30000),
    rowText: 'You pay Grandma',
    cardIndex: 0,
    title: 'I paid Grandma',
    note: countsRightAway,
    from: filipMember,
    to: grandmaMember,
    amountMinor: 30000,
    currency: 'MKD',
  },
  {
    name: 'I owe a person with an account, and I am the owner',
    options: filipView,
    balances: onePaymentBalances(filipMember, anaMember, 45000),
    rowText: 'You pay Ana',
    cardIndex: 0,
    title: 'I paid Ana',
    note: waitsUntil('Ana'),
    from: filipMember,
    to: anaMember,
    amountMinor: 45000,
    currency: 'MKD',
  },
  {
    name: 'the owner acts for a name that pays another name',
    options: filipView,
    balances: markoOwesGrandmaBalances,
    rowText: 'Marko pays Grandma',
    cardIndex: null,
    title: 'Marko paid Grandma',
    note: countsRightAway,
    from: markoMember,
    to: grandmaMember,
    amountMinor: 80000,
    currency: 'MKD',
  },
  {
    name: 'the owner acts for a name that pays a person with an account',
    options: filipView,
    balances: onePaymentBalances(markoMember, anaMember, 35000),
    rowText: 'Marko pays Ana',
    cardIndex: null,
    title: 'Marko paid Ana',
    note: waitsUntil('Ana'),
    from: markoMember,
    to: anaMember,
    amountMinor: 35000,
    currency: 'MKD',
  },
  {
    name: 'the owner acts for a person with an account who pays a name',
    options: filipView,
    balances: onePaymentBalances(anaMember, grandmaMember, 15000),
    rowText: 'Ana pays Grandma',
    cardIndex: null,
    title: 'Ana paid Grandma',
    note: countsRightAway,
    from: anaMember,
    to: grandmaMember,
    amountMinor: 15000,
    currency: 'MKD',
  },
  {
    name: 'I owe euros to a person with an account, in the card of the euros',
    options: filipView,
    balances: greeceTripInTwoCurrencies,
    rowText: 'You pay Ana',
    cardIndex: 1,
    title: 'I paid Ana',
    note: waitsUntil('Ana'),
    from: filipMember,
    to: anaMember,
    amountMinor: 2000,
    currency: 'EUR',
  },
  {
    name: 'I am owed euros with cents',
    options: filipView,
    balances: onePaymentBalances(markoMember, filipMember, 1250, 'EUR'),
    rowText: 'Marko pays you',
    cardIndex: 0,
    title: 'Marko paid me',
    note: countsRightAway,
    from: markoMember,
    to: filipMember,
    amountMinor: 1250,
    currency: 'EUR',
  },
]

const betweenOthersCases: [string, GroupBalances, string][] = [
  ['a name that pays another name', markoOwesGrandmaBalances, 'Marko pays Grandma'],
  ['a name that pays a person with an account', onePaymentBalances(markoMember, petarMember, 35000), 'Marko pays Petar'],
  ['a person with an account who pays a name', onePaymentBalances(petarMember, grandmaMember, 15000), 'Petar pays Grandma'],
]

function createdFor(scenario: MarkPaidScenario) {
  return pendingPaymentOf(scenario.from, scenario.to, scenario.amountMinor, { currency: scenario.currency })
}

async function renderScenario(scenario: MarkPaidScenario, answers: GroupAppOptions['answers'] = {}) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    ...scenario.options,
    balances: scenario.balances,
    answers: {
      ...scenario.options.answers,
      [`POST ${settlementsPath}`]: jsonAnswer(createdFor(scenario)),
      ...answers,
    },
    seedCache: seedActivityAndExpenses,
  })
  await findBalanceCards()
  return rendered
}

async function rowOf(scenario: MarkPaidScenario): Promise<HTMLElement> {
  if (scenario.cardIndex === null) {
    return betweenOthersRow(scenario.rowText)
  }
  const cards = await findBalanceCards()
  return rowWithText(cards[scenario.cardIndex], scenario.rowText)
}

async function openSheetOf(scenario: MarkPaidScenario): Promise<HTMLElement> {
  return openMarkPaid(await rowOf(scenario), scenario.title)
}

function scenarioNamed(name: string): MarkPaidScenario {
  const found = scenarios.find((scenario) => scenario.name === name)
  if (found === undefined) {
    throw new Error(`Found no Mark paid scenario named "${name}"`)
  }
  return found
}

const owedByMarko = scenarioNamed('I am owed by a person')
const owesFilip = scenarioNamed('I owe a person with an account')
const owesAnaInEuros = scenarioNamed('I owe euros to a person with an account, in the card of the euros')
const owedEurosWithCents = scenarioNamed('I am owed euros with cents')

describe('Mark paid on the Balances tab', () => {
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

  describe('where the button is', () => {
    it('is on each row of the card where somebody pays me, and nowhere else on the tab', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const card = await findOnlyBalanceCard()

      const rows = rowsOf(card)
      expect(rows).toHaveLength(2)
      for (const row of rows) {
        expect(buttonsNamedIn(row, 'Mark paid')).toHaveLength(1)
      }
      expect(allButtonsNamed('Mark paid')).toHaveLength(2)
    })

    it('is on the row where I pay, and not on the Between others row of two other people, for a member who is not the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...anaSignedIn() })
      const card = await findOnlyBalanceCard()

      expect(buttonsNamedIn(rowWithText(card, 'You pay Filip'), 'Mark paid')).toHaveLength(1)
      expect(queryTextIn(betweenOthersRow('Marko pays Filip'), 'Marko pays Filip')).toBeTruthy()
      expect(allButtonsNamed('Mark paid')).toHaveLength(1)
    })

    it.each(betweenOthersCases)('is on the Between others row of %s, for the owner', async (_name, balances, rowText) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances, ...filipView })
      await findBalanceCards()

      expect(buttonsNamedIn(betweenOthersRow(rowText), 'Mark paid')).toHaveLength(1)
      expect(allButtonsNamed('Mark paid')).toHaveLength(1)
    })

    it.each(betweenOthersCases)('is not on the Between others row of %s, for a member who is not the owner', async (_name, balances, rowText) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances, ...anaSignedIn() })
      await findBalanceCards()

      expect(queryTextIn(betweenOthersRow(rowText), rowText)).toBeTruthy()
      expect(allButtonsNamed('Mark paid')).toEqual([])
    })

    it('is not on the Between others row of two people with accounts, not even for the owner', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: onePaymentBalances(anaMember, petarMember, 20000),
        ...filipView,
      })
      await findBalanceCards()

      expect(queryTextIn(betweenOthersRow('Ana pays Petar'), 'Ana pays Petar')).toBeTruthy()
      expect(allButtonsNamed('Mark paid')).toEqual([])
    })

    it('is not on the row of my own pending payment, which has no suggested row any more', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [anaPaidFilipPending],
        ...anaSignedIn(),
      })
      const card = await findOnlyBalanceCard()

      expect(textIn(rowWithText(card, 'You paid Filip 500 MKD'), 'Waiting for Filip to confirm')).toBeTruthy()
      expect(allButtonsNamed('Mark paid')).toEqual([])
    })

    it('is not on the Between others row that a pending payment hides, and not on the Needs you card of that payment', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: markoOwesGrandmaBalances,
        settlements: [markoPaidGrandmaPending],
        ...filipView,
      })
      await findBalanceCards()

      expect(textIn(betweenOthersRow('Marko pays Grandma'), 'waiting')).toBeTruthy()
      expect(rowsOf(sectionNamed('Needs you'))).toHaveLength(1)
      expect(allButtonsNamed('Mark paid')).toEqual([])
    })

    it('is not on a Needs you card, and the other row stays', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [markoPaidFilipPending],
        ...filipView,
      })
      const card = await findOnlyBalanceCard()

      expect(buttonsNamedIn(sectionNamed('Needs you'), 'Mark paid')).toEqual([])
      expect(buttonsNamedIn(rowWithText(card, 'Ana pays you'), 'Mark paid')).toHaveLength(1)
      expect(allButtonsNamed('Mark paid')).toHaveLength(1)
    })

    it.each(['Confirmed', 'Rejected', 'Cancelled'] as const)('comes back on the row when the payment of that pair is %s, because it hides nothing', async (status) => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripBalances,
        settlements: [{ ...anaPaidFilipPending, status }],
        ...anaSignedIn(),
      })
      const card = await findOnlyBalanceCard()

      expect(buttonsNamedIn(rowWithText(card, 'You pay Filip'), 'Mark paid')).toHaveLength(1)
    })

    it('is on the rows of each currency card, with the card of the euros holding its own button', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripInTwoCurrencies, ...filipView })
      const [denarCard, euroCard] = await findBalanceCards()

      expect(rowsOf(denarCard)).toHaveLength(2)
      expect(buttonsNamedIn(denarCard, 'Mark paid')).toHaveLength(2)
      expect(rowsOf(euroCard)).toHaveLength(1)
      expect(buttonsNamedIn(euroCard, 'Mark paid')).toHaveLength(1)
    })

    it('is missing only from the card of the currency where a pending payment of mine hides the row', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        balances: greeceTripInTwoCurrencies,
        settlements: [anaPaidFilipPending],
        ...anaSignedIn(),
      })
      const [denarCard, euroCard] = await findBalanceCards()

      expect(buttonsNamedIn(denarCard, 'Mark paid')).toEqual([])
      expect(buttonsNamedIn(rowWithText(euroCard, 'Filip pays you'), 'Mark paid')).toHaveLength(1)
    })

    it('is not on the screen when everyone is kvit', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: everyoneIsKvitBalances, ...filipView })
      await screen.findByText(kvitTitle)

      expect(allButtonsNamed('Mark paid')).toEqual([])
    })
  })

  describe('as a control', () => {
    it('is a real button that does not submit a form, can take the focus, and is named Mark paid', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const card = await findOnlyBalanceCard()

      const button = buttonNamedIn(rowWithText(card, 'Marko pays you'), 'Mark paid')
      button.focus()

      expect(button.tagName).toBe('BUTTON')
      expect((button as HTMLButtonElement).type).toBe('button')
      expect(button.tabIndex).toBeGreaterThanOrEqual(0)
      expect(document.activeElement).toBe(button)
      expect(isDisabled(button)).toBe(false)
      expect(button.textContent).toBe('Mark paid')
    })

    it.each(languages)('is written in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, language, ...filipView })
      const [card] = await findCardsInLanguage(language)

      const label = language === 'en' ? 'Mark paid' : macedonianOf('Mark paid')
      const buttons = buttonsNamedIn(card, label)

      expect(buttons).toHaveLength(2)
      expect(buttons[0].textContent).toBe(label)
    })
  })

  describe('the sheet it opens', () => {
    it.each(scenarios)('is titled with who paid whom, for: $name', async (scenario) => {
      await renderScenario(scenario)

      const sheet = await openSheetOf(scenario)

      expect(within(sheet).getByRole('heading', { name: scenario.title })).toBeTruthy()
    })

    it.each(scenarios)('starts with the suggested amount in the amount field, for: $name', async (scenario) => {
      await renderScenario(scenario)

      await openSheetOf(scenario)

      expect(parseMoneyInput(amountField().value, scenario.currency)).toBe(scenario.amountMinor)
    })

    it.each(scenarios)('says when the payment will count, for: $name', async (scenario) => {
      await renderScenario(scenario)

      const sheet = await openSheetOf(scenario)

      expect(textIn(sheet, scenario.note)).toBeTruthy()
      const otherNote = scenario.note === countsRightAway ? waitsUntil(scenario.to.displayName) : countsRightAway
      expect(queryTextIn(sheet, otherNote)).toBeNull()
    })

    it('opens with the amount of the row that was pressed, not of another row', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { balances: greeceTripBalances, ...filipView })
      const card = await findOnlyBalanceCard()

      await openMarkPaid(rowWithText(card, 'Ana pays you'), 'Ana paid me')

      expect(parseMoneyInput(amountField().value, 'MKD')).toBe(70000)
    })

    it('has the buttons Save and Cancel, with Save enabled for the suggested amount', async () => {
      await renderScenario(owedByMarko)

      const sheet = await openSheetOf(owedByMarko)

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(false)
      expect(isDisabled(sheetButton(sheet, 'Cancel'))).toBe(false)
    })

    it('does not offer another currency, because the suggested payment has its own', async () => {
      await renderScenario(owedByMarko)

      const sheet = await openSheetOf(owedByMarko)

      expect(within(sheet).queryByRole('button', { name: /^Currency/ })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: 'EUR' })).toBeNull()
    })

    it('closes with Cancel, sends nothing and asks for nothing', async () => {
      const { fetchMock } = await renderScenario(owedByMarko)
      const sheet = await openSheetOf(owedByMarko)
      const requestsBefore = fetchMock.mock.calls.length

      fireEvent.click(sheetButton(sheet, 'Cancel'))

      await waitForSheetToClose()
      expect(fetchMock.mock.calls).toHaveLength(requestsBefore)
    })

    it('closes with Escape and sends nothing', async () => {
      const { fetchMock } = await renderScenario(owedByMarko)
      const sheet = await openSheetOf(owedByMarko)

      fireEvent.keyDown(sheet, { key: 'Escape' })

      await waitForSheetToClose()
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(0)
    })

    it('starts again with the suggested amount when it is opened again after a changed amount was cancelled', async () => {
      await renderScenario(owedByMarko)
      const sheet = await openSheetOf(owedByMarko)
      typeAmount('123')
      fireEvent.click(sheetButton(sheet, 'Cancel'))
      await waitForSheetToClose()

      await openSheetOf(owedByMarko)

      expect(parseMoneyInput(amountField().value, 'MKD')).toBe(80000)
    })

    it('keeps the Mark paid button on the row after the sheet was cancelled', async () => {
      await renderScenario(owedByMarko)
      const sheet = await openSheetOf(owedByMarko)
      fireEvent.click(sheetButton(sheet, 'Cancel'))
      await waitForSheetToClose()

      expect(buttonsNamedIn(await rowOf(owedByMarko), 'Mark paid')).toHaveLength(1)
    })
  })

  describe('the amount in the sheet', () => {
    it.each([
      ['a smaller amount', '500', 50000],
      ['a larger amount than the suggestion', '1000', 100000],
      ['an amount of one denar', '1', 100],
    ])('lets Save send %s in denars', async (_name, text, amountMinor) => {
      const { fetchMock } = await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)
      typeAmount(text)

      fireEvent.click(saveButton())

      await expectRecorded(fetchMock, {
        fromMemberId: markoMember.id,
        toMemberId: filipMember.id,
        amountMinor,
        currency: 'MKD',
      })
    })

    it.each([
      ['whole euros', '7', 700],
      ['euros with one decimal', '7.5', 750],
      ['euros with a decimal comma', '7,25', 725],
      ['a larger amount than the suggestion', '99.99', 9999],
    ])('lets Save send %s in euros, to the cent', async (_name, text, amountMinor) => {
      const { fetchMock } = await renderScenario(owedEurosWithCents)
      await openSheetOf(owedEurosWithCents)
      typeAmount(text)

      fireEvent.click(saveButton())

      await expectRecorded(fetchMock, {
        fromMemberId: markoMember.id,
        toMemberId: filipMember.id,
        amountMinor,
        currency: 'EUR',
      })
    })

    it.each(['', '0', '00', 'abc', '12.5', '1,200'])('keeps Save disabled for the denar amount "%s"', async (text) => {
      await renderScenario(owedByMarko)
      const sheet = await openSheetOf(owedByMarko)

      typeAmount(text)

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it.each(['', '0', '0.00', '12.555', 'abc'])('keeps Save disabled for the euro amount "%s"', async (text) => {
      await renderScenario(owedEurosWithCents)
      const sheet = await openSheetOf(owedEurosWithCents)

      typeAmount(text)

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it('marks the field as not valid for a decimal in denars, and clears the mark when a whole number is typed', async () => {
      await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)

      typeAmount('12.5')
      expect(amountField().getAttribute('aria-invalid')).toBe('true')

      typeAmount('12')
      expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
    })

    it('does not send anything when Save is pressed while it is disabled', async () => {
      const { fetchMock } = await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)
      typeAmount('0')

      fireEvent.click(saveButton())

      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(0)
    })
  })

  describe('saving', () => {
    it.each(scenarios)('sends the payment from the payer to the receiver, in the currency of the row, for: $name', async (scenario) => {
      const { fetchMock } = await renderScenario(scenario)
      await openSheetOf(scenario)

      fireEvent.click(saveButton())

      await expectRecorded(fetchMock, {
        fromMemberId: scenario.from.id,
        toMemberId: scenario.to.id,
        amountMinor: scenario.amountMinor,
        currency: scenario.currency,
      })
    })

    it('sends it as JSON to the payments of the group', async () => {
      const { fetchMock } = await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', settlementsPath)[0].contentType).toBe('application/json')
    })

    it('sends a clientRequestId that is a new id', async () => {
      const { fetchMock } = await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
      })
      expect(String(recordedBody(fetchMock).clientRequestId)).toMatch(uuidFormat)
    })

    it('closes the sheet after the answer and shows no error', async () => {
      await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('keeps the sheet open until the server has answered', async () => {
      await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)
      neverAnsweringFetch()

      fireEvent.click(saveButton())

      await expectDisabledWhilePending(saveButton())
      expect(screen.getByRole('dialog', { name: owedByMarko.title })).toBeTruthy()
    })

    it('sends one request, not two, when Save is pressed again while it waits for the answer', async () => {
      await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)
      const neverAnswering = neverAnsweringFetch()

      fireEvent.click(saveButton())
      await expectDisabledWhilePending(saveButton())
      fireEvent.click(saveButton())

      expect(sentBodies(neverAnswering)).toHaveLength(1)
    })

    it('uses another clientRequestId every time the sheet is opened again', async () => {
      const { fetchMock } = await renderScenario(owedByMarko, {
        [`POST ${settlementsPath}`]: problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE'),
      })
      const sheet = await openSheetOf(owedByMarko)
      fireEvent.click(saveButton())
      await within(sheet).findByRole('alert')
      fireEvent.click(sheetButton(sheet, 'Cancel'))
      await waitForSheetToClose()
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(2)
      })
      expect(recordedBody(fetchMock, 1).clientRequestId).not.toBe(recordedBody(fetchMock, 0).clientRequestId)
    })

    it.each([
      ['an error of the server', problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE')],
      ['a network failure', networkFailureAnswer()],
    ])('keeps the same clientRequestId and the same request when Save is pressed again after %s', async (_name, failure) => {
      let tries = 0
      const { fetchMock } = await renderScenario(owedByMarko, {
        [`POST ${settlementsPath}`]: () => {
          tries += 1
          return tries === 1 ? failure() : Response.json(createdFor(owedByMarko))
        },
      })
      const sheet = await openSheetOf(owedByMarko)
      fireEvent.click(saveButton())
      await within(sheet).findByRole('alert')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(2)
      })
      expect(recordedBody(fetchMock, 1).clientRequestId).toBe(recordedBody(fetchMock, 0).clientRequestId)
      expect(recordedBody(fetchMock, 1)).toEqual(recordedBody(fetchMock, 0))
      await waitForSheetToClose()
    })
  })

  describe('when the server refuses', () => {
    it.each([
      [400, 'SETTLEMENT_AMOUNT_NOT_POSITIVE'],
      [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
      [400, 'SETTLEMENT_MEMBER_NOT_CURRENT'],
      [400, 'SETTLEMENT_CLIENT_REQUEST_ID_USED'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'GROUP_NOT_FOUND'],
    ])('shows the translated text for %i %s inside the sheet and leaves the sheet open', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderScenario(owedByMarko, { [`POST ${settlementsPath}`]: problemAnswer(status, code) })
      const sheet = await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(screen.getByRole('dialog', { name: owedByMarko.title })).toBe(sheet)
    })

    it('shows the network message inside the sheet when the server cannot be reached', async () => {
      await renderScenario(owedByMarko, { [`POST ${settlementsPath}`]: networkFailureAnswer() })
      const sheet = await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('writes the error in Macedonian for a person who uses Macedonian', async () => {
      await renderGroupApp(routes.groupBalances(groupId), {
        language: 'mk',
        balances: owedByMarko.balances,
        ...filipView,
        answers: { [`POST ${settlementsPath}`]: problemAnswer(403, 'SETTLEMENT_NOT_ALLOWED') },
      })
      const [card] = await findCardsInLanguage('mk')
      fireEvent.click(within(rowsOf(card)[0]).getByRole('button'))
      const sheet = await screen.findByRole('dialog')

      fireEvent.click(saveButton('mk'))

      expect((await within(sheet).findByRole('alert')).textContent).toContain(
        translated('mk', 'errors.SETTLEMENT_NOT_ALLOWED'),
      )
    })

    it('keeps the amount that was typed, and enables Save again', async () => {
      await renderScenario(owedByMarko, { [`POST ${settlementsPath}`]: problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE') })
      const sheet = await openSheetOf(owedByMarko)
      typeAmount('500')

      fireEvent.click(saveButton())
      await within(sheet).findByRole('alert')

      expect(amountField().value).toBe('500')
      expect(isDisabled(saveButton())).toBe(false)
    })

    it('does not ask for the balances or the payments again, and marks nothing as out of date', async () => {
      const { fetchMock, queryClient } = await renderScenario(owedByMarko, {
        [`POST ${settlementsPath}`]: problemAnswer(403, 'SETTLEMENT_NOT_ALLOWED'),
      })
      const sheet = await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())
      await within(sheet).findByRole('alert')

      expectGroupNotAskedAgain(fetchMock, queryClient)
    })

    it('removes the error when the next try works, and closes the sheet', async () => {
      let tries = 0
      await renderScenario(owedByMarko, {
        [`POST ${settlementsPath}`]: () => {
          tries += 1
          return tries === 1 ? problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE')() : Response.json(createdFor(owedByMarko))
        },
      })
      const sheet = await openSheetOf(owedByMarko)
      fireEvent.click(saveButton())
      await within(sheet).findByRole('alert')

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('after it was saved', () => {
    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderScenario(owedByMarko)
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('shows the payment as counted at once when I am the receiver: the kvit message, and the payment in the list of payments', async () => {
      const confirmed = confirmedPaymentOf(markoMember, filipMember, 80000)
      const live: LiveGroup = { balances: owedByMarko.balances, settlements: [] }
      await renderScenario(owedByMarko, {
        ...liveAnswers(live),
        [`POST ${settlementsPath}`]: () => {
          live.balances = everyoneIsKvitBalances
          live.settlements = [confirmed]
          return Response.json(confirmed)
        },
      })
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      expect(await screen.findByText(kvitTitle)).toBeTruthy()
      expect(allButtonsNamed('Mark paid')).toEqual([])
      expect(textIn(sectionNamed('Payments'), 'Marko paid Filip')).toBeTruthy()
    })

    it('shows the payment as waiting when I am the payer: a row that waits for the answer with Cancel, and no Mark paid', async () => {
      const pending = pendingPaymentOf(anaMember, filipMember, 70000)
      const live: LiveGroup = { balances: owesFilip.balances, settlements: [] }
      await renderScenario(owesFilip, {
        ...liveAnswers(live),
        [`POST ${settlementsPath}`]: () => {
          live.settlements = [pending]
          return Response.json(pending)
        },
      })
      await openSheetOf(owesFilip)

      fireEvent.click(saveButton())

      const card = await findOnlyBalanceCard()
      const row = await waitFor(() => rowWithText(card, 'You paid Filip 700 MKD'))
      expect(textIn(row, 'Waiting for Filip to confirm')).toBeTruthy()
      expect(buttonsNamedIn(row, 'Cancel')).toHaveLength(1)
      expect(allButtonsNamed('Mark paid')).toEqual([])
    })

    it('shows the amount that was sent, not the suggested one, in the row that waits', async () => {
      const pending = pendingPaymentOf(anaMember, filipMember, 50000)
      const live: LiveGroup = { balances: owesFilip.balances, settlements: [] }
      await renderScenario(owesFilip, {
        ...liveAnswers(live),
        [`POST ${settlementsPath}`]: () => {
          live.settlements = [pending]
          return Response.json(pending)
        },
      })
      await openSheetOf(owesFilip)
      typeAmount('500')

      fireEvent.click(saveButton())

      const card = await findOnlyBalanceCard()
      expect(await waitFor(() => rowWithText(card, 'You paid Filip 500 MKD'))).toBeTruthy()
    })

    it('shows the payment of the euros in the card of the euros, with the denar card unchanged', async () => {
      const pending = pendingPaymentOf(filipMember, anaMember, 2000, { currency: 'EUR' })
      const live: LiveGroup = { balances: owesAnaInEuros.balances, settlements: [] }
      await renderScenario(owesAnaInEuros, {
        ...liveAnswers(live),
        [`POST ${settlementsPath}`]: () => {
          live.settlements = [pending]
          return Response.json(pending)
        },
      })
      await openSheetOf(owesAnaInEuros)

      fireEvent.click(saveButton())

      await waitFor(async () => {
        const [denarCard, euroCard] = await findBalanceCards()
        expect(textIn(rowWithText(euroCard, 'You paid Ana €20.00'), 'Waiting for Ana to confirm')).toBeTruthy()
        expect(buttonsNamedIn(denarCard, 'Mark paid')).toHaveLength(2)
      })
    })

    it('does not show an error for the save when only the asking again fails', async () => {
      let isBalancesBroken = false
      const { fetchMock } = await renderScenario(owedByMarko, {
        [`GET ${balancesPath}`]: () =>
          isBalancesBroken ? new Response(null, { status: 500 }) : Response.json(owedByMarko.balances),
        [`POST ${settlementsPath}`]: () => {
          isBalancesBroken = true
          return Response.json(createdFor(owedByMarko))
        },
      })
      await openSheetOf(owedByMarko)

      fireEvent.click(saveButton())

      await waitForSheetToClose()
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })
})
