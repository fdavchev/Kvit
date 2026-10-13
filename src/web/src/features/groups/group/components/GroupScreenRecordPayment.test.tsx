import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import type { GroupMember } from '@/core/services/groups/membersService'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  balancesPath,
  everyoneIsKvitBalances,
  greeceTripBalances,
  settlementsPath,
} from '@/test/balancesTestData'
import { findBalanceCards, queryTextIn, sectionNamed, textIn } from '@/test/balancesTestHelpers'
import {
  amountField,
  isDisabled,
  saveButton,
  typeAmount,
  waitForSheetToClose,
} from '@/test/expenseFormTestHelpers'
import { freezeTime, isBefore, unfreezeTime, uuidFormat } from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import {
  anaMember,
  bojanRemoved,
  filipMember,
  grandmaMember,
  markoMember,
  membersOf,
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
  pendingPaymentOf,
  seedActivityAndExpenses,
  soloKvitBalances,
  type LiveGroup,
} from '@/test/settlementActionsTestData'
import {
  expectGroupAskedAgain,
  expectGroupNotAskedAgain,
  expectRecorded,
  isPressed,
  neverAnsweringFetch,
  recordedBody,
  sentBodies,
  sheetButton,
} from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const recordAnotherPayment = 'Record another payment'
const iPaid = 'I paid'
const someonePaidMe = 'Someone paid me'
const countsRightAway = 'Counts right away.'
const kvitTitle = 'Everyone is kvit \u{1F389}'
const filipView: GroupAppOptions = { members: ownerViewMembers, group: testGroup }

function waitsUntil(name: string): string {
  return `Waits until ${name} confirms. You can cancel until then.`
}

async function renderBalances(options: GroupAppOptions = {}) {
  const rendered = await renderGroupApp(routes.groupBalances(groupId), {
    balances: greeceTripBalances,
    ...filipView,
    seedCache: seedActivityAndExpenses,
    ...options,
    answers: {
      [`POST ${settlementsPath}`]: jsonAnswer(pendingPaymentOf(filipMember, anaMember, 50000)),
      ...options.answers,
    },
  })
  await findBalanceCards()
  return rendered
}

function recordButton(): HTMLElement {
  return screen.getByRole('button', { name: recordAnotherPayment })
}

async function openSheet(): Promise<HTMLElement> {
  fireEvent.click(recordButton())
  return screen.findByRole('dialog', { name: recordAnotherPayment })
}

async function fillAndSave(sheet: HTMLElement, person: string, amount: string): Promise<void> {
  fireEvent.click(sheetButton(sheet, person))
  typeAmount(amount)
  fireEvent.click(saveButton())
}

interface RecordCase {
  name: string
  options: GroupAppOptions
  direction: string
  person: string
  amount: string
  note: string
  from: GroupMember
  to: GroupMember
  amountMinor: number
}

const cases: RecordCase[] = [
  {
    name: 'the owner paid a person with an account',
    options: filipView,
    direction: iPaid,
    person: 'Ana',
    amount: '500',
    note: waitsUntil('Ana'),
    from: filipMember,
    to: anaMember,
    amountMinor: 50000,
  },
  {
    name: 'the owner paid a name without an account',
    options: filipView,
    direction: iPaid,
    person: 'Marko',
    amount: '300',
    note: countsRightAway,
    from: filipMember,
    to: markoMember,
    amountMinor: 30000,
  },
  {
    name: 'a person with an account paid the owner',
    options: filipView,
    direction: someonePaidMe,
    person: 'Ana',
    amount: '200',
    note: countsRightAway,
    from: anaMember,
    to: filipMember,
    amountMinor: 20000,
  },
  {
    name: 'a name without an account paid the owner',
    options: filipView,
    direction: someonePaidMe,
    person: 'Grandma',
    amount: '100',
    note: countsRightAway,
    from: grandmaMember,
    to: filipMember,
    amountMinor: 10000,
  },
  {
    name: 'a member who is not the owner paid the owner',
    options: anaSignedIn(),
    direction: iPaid,
    person: 'Filip',
    amount: '700',
    note: waitsUntil('Filip'),
    from: anaMember,
    to: filipMember,
    amountMinor: 70000,
  },
  {
    name: 'a member who is not the owner paid a name without an account',
    options: anaSignedIn(),
    direction: iPaid,
    person: 'Marko',
    amount: '250',
    note: waitsUntil('Marko'),
    from: anaMember,
    to: markoMember,
    amountMinor: 25000,
  },
  {
    name: 'a name without an account paid a member who is not the owner',
    options: anaSignedIn(),
    direction: someonePaidMe,
    person: 'Marko',
    amount: '250',
    note: countsRightAway,
    from: markoMember,
    to: anaMember,
    amountMinor: 25000,
  },
]

describe('Record another payment on the Balances tab', () => {
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

  describe('the button', () => {
    it('is a real button called Record another payment, below the Everyone section, shown once', async () => {
      await renderBalances()

      const button = recordButton()

      expect(button.tagName).toBe('BUTTON')
      expect((button as HTMLButtonElement).type).toBe('button')
      expect(isDisabled(button)).toBe(false)
      expect(screen.getAllByText(recordAnotherPayment)).toHaveLength(1)
      expect(isBefore(sectionNamed('Everyone'), button)).toBe(true)
    })

    it('can take the focus', async () => {
      await renderBalances()

      const button = recordButton()
      button.focus()

      expect(document.activeElement).toBe(button)
    })

    it('is shown when everyone is kvit', async () => {
      await renderBalances({ balances: everyoneIsKvitBalances })
      await screen.findByText(kvitTitle)

      expect(isDisabled(recordButton())).toBe(false)
    })

    it('is hidden when nobody else is in the group', async () => {
      await renderBalances({
        balances: soloKvitBalances,
        members: membersOf({ members: [filipMember], removed: [], canClaimNames: false }),
      })
      await screen.findByText(kvitTitle)

      expect(screen.queryByText(recordAnotherPayment)).toBeNull()
      expect(screen.queryByRole('button', { name: recordAnotherPayment })).toBeNull()
    })

    it('is hidden when everyone else has left the group', async () => {
      await renderBalances({
        balances: soloKvitBalances,
        members: membersOf({ members: [filipMember], removed: [bojanRemoved], canClaimNames: false }),
      })
      await screen.findByText(kvitTitle)

      expect(screen.queryByText(recordAnotherPayment)).toBeNull()
    })
  })

  describe('the sheet', () => {
    it('opens as a dialog with the title Record another payment', async () => {
      await renderBalances()

      const sheet = await openSheet()

      expect(within(sheet).getByRole('heading', { name: recordAnotherPayment })).toBeTruthy()
    })

    it('starts with I paid chosen and Someone paid me not chosen', async () => {
      await renderBalances()

      const sheet = await openSheet()

      expect(isPressed(sheetButton(sheet, iPaid))).toBe(true)
      expect(isPressed(sheetButton(sheet, someonePaidMe))).toBe(false)
    })

    it('offers every other current member as a chip, plain names too, and neither me nor a person who left', async () => {
      await renderBalances()

      const sheet = await openSheet()

      for (const name of [anaMember, markoMember, grandmaMember, petarMember].map((member) => member.displayName)) {
        expect(isPressed(sheetButton(sheet, name))).toBe(false)
      }
      expect(within(sheet).queryByRole('button', { name: 'Filip' })).toBeNull()
      expect(within(sheet).queryByRole('button', { name: bojanRemoved.displayName })).toBeNull()
    })

    it('does not offer me to the member who is not the owner either, but offers the owner', async () => {
      await renderBalances(anaSignedIn())

      const sheet = await openSheet()

      expect(within(sheet).queryByRole('button', { name: 'Ana' })).toBeNull()
      expect(within(sheet).getByRole('button', { name: 'Filip' })).toBeTruthy()
    })

    it('starts with the amount field empty', async () => {
      await renderBalances()

      await openSheet()

      expect(amountField().value).toBe('')
    })

    it.each([
      ['denars', 'MKD', 'EUR'],
      ['euros', 'EUR', 'MKD'],
    ] as const)('starts with %s chosen when the group counts in %s', async (_name, chosen, other) => {
      await renderBalances({ group: groupOf({ defaultCurrency: chosen }) })

      const sheet = await openSheet()

      expect(isPressed(sheetButton(sheet, chosen))).toBe(true)
      expect(isPressed(sheetButton(sheet, other))).toBe(false)
    })

    it('has Save and Cancel buttons, with Cancel enabled', async () => {
      await renderBalances()

      const sheet = await openSheet()

      expect(isDisabled(sheetButton(sheet, 'Cancel'))).toBe(false)
      expect(sheetButton(sheet, 'Save')).toBeTruthy()
    })

    it('chooses one person at a time', async () => {
      await renderBalances()
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, 'Ana'))
      fireEvent.click(sheetButton(sheet, 'Marko'))

      expect(isPressed(sheetButton(sheet, 'Ana'))).toBe(false)
      expect(isPressed(sheetButton(sheet, 'Marko'))).toBe(true)
    })

    it('chooses one direction at a time', async () => {
      await renderBalances()
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, someonePaidMe))

      expect(isPressed(sheetButton(sheet, someonePaidMe))).toBe(true)
      expect(isPressed(sheetButton(sheet, iPaid))).toBe(false)
    })

    it('chooses one currency at a time', async () => {
      await renderBalances()
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, 'EUR'))

      expect(isPressed(sheetButton(sheet, 'EUR'))).toBe(true)
      expect(isPressed(sheetButton(sheet, 'MKD'))).toBe(false)
    })

    it('closes with Cancel and sends nothing', async () => {
      const { fetchMock } = await renderBalances()
      const sheet = await openSheet()
      const requestsBefore = fetchMock.mock.calls.length

      fireEvent.click(sheetButton(sheet, 'Cancel'))

      await waitForSheetToClose()
      expect(fetchMock.mock.calls).toHaveLength(requestsBefore)
    })

    it('closes with Escape and sends nothing', async () => {
      const { fetchMock } = await renderBalances()
      const sheet = await openSheet()

      fireEvent.keyDown(sheet, { key: 'Escape' })

      await waitForSheetToClose()
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(0)
    })

    it('starts again empty when it is opened again after a person and an amount were chosen', async () => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))
      fireEvent.click(sheetButton(sheet, someonePaidMe))
      typeAmount('500')
      fireEvent.click(sheetButton(sheet, 'Cancel'))
      await waitForSheetToClose()

      const again = await openSheet()

      expect(amountField().value).toBe('')
      expect(isPressed(sheetButton(again, 'Ana'))).toBe(false)
      expect(isPressed(sheetButton(again, iPaid))).toBe(true)
    })
  })

  describe('when Save is possible', () => {
    it('keeps Save disabled when nothing was chosen', async () => {
      await renderBalances()
      const sheet = await openSheet()

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it('keeps Save disabled when only a person was chosen', async () => {
      await renderBalances()
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, 'Ana'))

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it('keeps Save disabled when only an amount was typed', async () => {
      await renderBalances()
      const sheet = await openSheet()

      typeAmount('500')

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it('enables Save when a person and an amount are chosen', async () => {
      await renderBalances()
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, 'Ana'))
      typeAmount('500')

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(false)
    })

    it.each(['0', '00', 'abc', '12.5', '1,200', ''])('keeps Save disabled for the denar amount "%s" although a person is chosen', async (text) => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))

      typeAmount(text)

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it.each(['0', '0.00', '12.555', 'abc'])('keeps Save disabled for the euro amount "%s" although a person is chosen', async (text) => {
      await renderBalances({ group: groupOf({ defaultCurrency: 'EUR' }) })
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))

      typeAmount(text)

      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)
    })

    it('keeps the typed text 12.50 when denars are chosen, marks the field and disables Save, and enables Save again when euros are chosen', async () => {
      await renderBalances({ group: groupOf({ defaultCurrency: 'EUR' }) })
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))
      typeAmount('12.50')
      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(false)

      fireEvent.click(sheetButton(sheet, 'MKD'))

      expect(amountField().value).toBe('12.50')
      expect(amountField().getAttribute('aria-invalid')).toBe('true')
      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(true)

      fireEvent.click(sheetButton(sheet, 'EUR'))

      expect(amountField().getAttribute('aria-invalid')).not.toBe('true')
      expect(isDisabled(sheetButton(sheet, 'Save'))).toBe(false)
    })

    it('does not send anything when Save is pressed while it is disabled', async () => {
      const { fetchMock } = await renderBalances()
      await openSheet()

      fireEvent.click(saveButton())

      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(0)
    })
  })

  describe('the note under the amount', () => {
    it.each(cases)('says when the payment counts, for: $name', async (recordCase) => {
      await renderBalances(recordCase.options)
      const sheet = await openSheet()

      fireEvent.click(sheetButton(sheet, recordCase.direction))
      fireEvent.click(sheetButton(sheet, recordCase.person))

      expect(textIn(sheet, recordCase.note)).toBeTruthy()
      const otherNote = recordCase.note === countsRightAway ? waitsUntil(recordCase.person) : countsRightAway
      expect(queryTextIn(sheet, otherNote)).toBeNull()
    })

    it('changes when the direction is changed from I paid to Someone paid me', async () => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))
      expect(textIn(sheet, waitsUntil('Ana'))).toBeTruthy()

      fireEvent.click(sheetButton(sheet, someonePaidMe))

      expect(textIn(sheet, countsRightAway)).toBeTruthy()
      expect(queryTextIn(sheet, waitsUntil('Ana'))).toBeNull()
    })

    it('changes when another person is chosen', async () => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))

      fireEvent.click(sheetButton(sheet, 'Marko'))

      expect(textIn(sheet, countsRightAway)).toBeTruthy()
      expect(queryTextIn(sheet, waitsUntil('Ana'))).toBeNull()
    })
  })

  describe('saving', () => {
    it.each(cases)('sends the payment from the payer to the receiver, for: $name', async (recordCase) => {
      const { fetchMock } = await renderBalances(recordCase.options)
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, recordCase.direction))

      await fillAndSave(sheet, recordCase.person, recordCase.amount)

      await expectRecorded(fetchMock, {
        fromMemberId: recordCase.from.id,
        toMemberId: recordCase.to.id,
        amountMinor: recordCase.amountMinor,
        currency: 'MKD',
      })
    })

    it('sends euros to the cent after the euro chip was chosen in a group that counts in denars', async () => {
      const { fetchMock } = await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'EUR'))

      await fillAndSave(sheet, 'Ana', '12.50')

      await expectRecorded(fetchMock, {
        fromMemberId: filipMember.id,
        toMemberId: anaMember.id,
        amountMinor: 1250,
        currency: 'EUR',
      })
    })

    it('sends euros when the group counts in euros and the chip was not touched', async () => {
      const { fetchMock } = await renderBalances({ group: groupOf({ defaultCurrency: 'EUR' }) })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '7')

      await expectRecorded(fetchMock, {
        fromMemberId: filipMember.id,
        toMemberId: anaMember.id,
        amountMinor: 700,
        currency: 'EUR',
      })
    })

    it('sends it as JSON with a clientRequestId that is a new id', async () => {
      const { fetchMock } = await renderBalances()
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', settlementsPath)[0].contentType).toBe('application/json')
      expect(String(recordedBody(fetchMock).clientRequestId)).toMatch(uuidFormat)
    })

    it('closes the sheet after the answer and shows no error', async () => {
      await renderBalances()
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      await waitForSheetToClose()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('disables Save while the request waits for an answer, and keeps the sheet open', async () => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))
      typeAmount('500')
      neverAnsweringFetch()

      fireEvent.click(saveButton())

      await expectDisabledWhilePending(saveButton())
      expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
    })

    it('sends one request, not two, when Save is pressed again while it waits for the answer', async () => {
      await renderBalances()
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, 'Ana'))
      typeAmount('500')
      const neverAnswering = neverAnsweringFetch()

      fireEvent.click(saveButton())
      await expectDisabledWhilePending(saveButton())
      fireEvent.click(saveButton())

      expect(sentBodies(neverAnswering)).toHaveLength(1)
    })

    it('uses another clientRequestId every time the sheet is opened again', async () => {
      const { fetchMock } = await renderBalances({
        answers: { [`POST ${settlementsPath}`]: problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE') },
      })
      const sheet = await openSheet()
      await fillAndSave(sheet, 'Ana', '500')
      await within(sheet).findByRole('alert')
      fireEvent.click(sheetButton(sheet, 'Cancel'))
      await waitForSheetToClose()
      const again = await openSheet()

      await fillAndSave(again, 'Ana', '500')

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
      const { fetchMock } = await renderBalances({
        answers: {
          [`POST ${settlementsPath}`]: () => {
            tries += 1
            return tries === 1 ? failure() : Response.json(pendingPaymentOf(filipMember, anaMember, 50000))
          },
        },
      })
      const sheet = await openSheet()
      await fillAndSave(sheet, 'Ana', '500')
      await within(sheet).findByRole('alert')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(2)
      })
      expect(recordedBody(fetchMock, 1)).toEqual(recordedBody(fetchMock, 0))
      await waitForSheetToClose()
    })
  })

  describe('when the server refuses', () => {
    it.each([
      [400, 'SETTLEMENT_AMOUNT_NOT_POSITIVE'],
      [400, 'SETTLEMENT_SAME_MEMBER'],
      [400, 'SETTLEMENT_MEMBER_NOT_CURRENT'],
      [400, 'SETTLEMENT_CURRENCY_INVALID'],
      [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
    ])('shows the translated text for %i %s inside the sheet and leaves the sheet open', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderBalances({ answers: { [`POST ${settlementsPath}`]: problemAnswer(status, code) } })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(screen.getByRole('dialog', { name: recordAnotherPayment })).toBe(sheet)
    })

    it('shows the network message inside the sheet when the server cannot be reached', async () => {
      await renderBalances({ answers: { [`POST ${settlementsPath}`]: networkFailureAnswer() } })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('keeps the direction, the person, the currency and the amount that were chosen', async () => {
      await renderBalances({ answers: { [`POST ${settlementsPath}`]: problemAnswer(400, 'SETTLEMENT_AMOUNT_TOO_LARGE') } })
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, someonePaidMe))
      fireEvent.click(sheetButton(sheet, 'EUR'))

      await fillAndSave(sheet, 'Marko', '12.50')
      await within(sheet).findByRole('alert')

      expect(isPressed(sheetButton(sheet, someonePaidMe))).toBe(true)
      expect(isPressed(sheetButton(sheet, 'Marko'))).toBe(true)
      expect(isPressed(sheetButton(sheet, 'EUR'))).toBe(true)
      expect(amountField().value).toBe('12.50')
      expect(isDisabled(saveButton())).toBe(false)
    })

    it('does not ask for the balances or the payments again, and marks nothing as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances({
        answers: { [`POST ${settlementsPath}`]: problemAnswer(403, 'SETTLEMENT_NOT_ALLOWED') },
      })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')
      await within(sheet).findByRole('alert')

      expectGroupNotAskedAgain(fetchMock, queryClient)
    })
  })

  describe('after it was saved', () => {
    it('asks for the balances and the payments again and marks the activity and the expenses as out of date', async () => {
      const { fetchMock, queryClient } = await renderBalances()
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      await expectGroupAskedAgain(fetchMock, queryClient)
    })

    it('shows a payment that counts at once in the list of payments', async () => {
      const confirmed = confirmedPaymentOf(anaMember, filipMember, 20000)
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [] }
      await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`POST ${settlementsPath}`]: () => {
            live.settlements = [confirmed]
            return Response.json(confirmed)
          },
        },
      })
      const sheet = await openSheet()
      fireEvent.click(sheetButton(sheet, someonePaidMe))

      await fillAndSave(sheet, 'Ana', '200')

      await waitFor(() => {
        expect(textIn(sectionNamed('Payments'), 'Ana paid Filip')).toBeTruthy()
      })
    })

    it('shows a payment that waits for the answer of the receiver as a row of mine that waits, in the card of its currency', async () => {
      const pending = pendingPaymentOf(filipMember, anaMember, 50000)
      const live: LiveGroup = { balances: greeceTripBalances, settlements: [] }
      await renderBalances({
        answers: {
          ...liveAnswers(live),
          [`POST ${settlementsPath}`]: () => {
            live.settlements = [pending]
            return Response.json(pending)
          },
        },
      })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      expect(await screen.findByText('Waiting for Ana to confirm')).toBeTruthy()
      expect(screen.queryByRole('region', { name: 'Payments' })).toBeNull()
    })

    it('does not show an error for the save when only asking again fails', async () => {
      let isAskingAgainBroken = false
      const { fetchMock } = await renderBalances({
        answers: {
          [`GET ${balancesPath}`]: () =>
            isAskingAgainBroken ? new Response(null, { status: 500 }) : Response.json(greeceTripBalances),
          [`POST ${settlementsPath}`]: () => {
            isAskingAgainBroken = true
            return Response.json(pendingPaymentOf(filipMember, anaMember, 50000))
          },
        },
      })
      const sheet = await openSheet()

      await fillAndSave(sheet, 'Ana', '500')

      await waitForSheetToClose()
      expect(requestCount(fetchMock, 'POST', settlementsPath)).toBe(1)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })
})
