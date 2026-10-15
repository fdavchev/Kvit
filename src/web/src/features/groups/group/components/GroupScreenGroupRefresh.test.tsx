import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import { greeceTripBalances, markoPaidFilipPending, settlementPathOf } from '@/test/balancesTestData'
import { rowWithText, sectionNamed } from '@/test/balancesTestHelpers'
import {
  asMemberOf,
  closingEndsAt,
  closingGroup,
  closingPath,
  closingConfirmPath,
  closingObjectPath,
  closingStartedAt,
  closingTexts,
  finishedBarText,
  finishedGroup,
  readyToCloseGroup,
  textOf,
  unlockPath,
} from '@/test/closingTestData'
import {
  anaView,
  buttonIn,
  findBarTitled,
  findClosingBar,
  liveGroupAnswer,
  ownerView,
  type LiveGroupState,
} from '@/test/closingTestHelpers'
import { freezeTime, groupPath, plainSpaces, unfreezeTime } from '@/test/expenseTestHelpers'
import { waitForSheetToClose } from '@/test/expenseFormTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { filipMember, markoMember } from '@/test/memberTestData'
import { problemAnswer, requestCount, type AnswerFactory } from '@/test/requestTestHelpers'
import {
  confirmedPaymentOf,
  liveAnswers,
  paymentIds,
  seedActivityAndExpenses,
  type LiveGroup,
} from '@/test/settlementActionsTestData'
import { buttonNamedIn, findCardsInLanguage } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { testMe } from '@/test/testMe'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const openGroup = testGroup
const memberOpenGroup = asMemberOf(openGroup)
const memberClosingGroup = asMemberOf(closingGroup)
const promptParams = { name: 'Filip', group: testGroup.name }
const markoSentence = 'Marko says they paid you 800 MKD'

const openOneBill = groupOf({ kind: 'OneBill', memberCount: 3 })
const finishedOneBill = groupOf({
  kind: 'OneBill',
  memberCount: 3,
  status: 'Finished',
  closingStartedAt,
  closingEndsAt,
  closingConfirmedBy: [],
  closingWaitingFor: [],
})

const markoPaidFilip = confirmedPaymentOf(markoMember, filipMember, 80000, {
  id: paymentIds.first,
  recordedByUserId: testMe.id,
  createdAt: '2026-10-06T08:00:00Z',
})

function liveOptions(base: GroupAppOptions, live: LiveGroupState, answers: Record<string, AnswerFactory>): GroupAppOptions {
  return {
    ...base,
    answers: {
      ...base.answers,
      [`GET ${groupPath}`]: liveGroupAnswer(live),
      ...answers,
    },
  }
}

function promptBarText(): string {
  return textOf('en', closingTexts.prompt, promptParams)
}

describe('the group screen after a change that decides the status of the group', () => {
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

  describe('on the Balances tab', () => {
    it('shows the ready bar without a reload when the owner confirms the last payment that was keeping the group from being kvit', async () => {
      const liveGroup: LiveGroupState = { group: openGroup }
      const liveMoney: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilipPending] }
      const confirmPath = `${settlementPathOf(markoPaidFilipPending.id)}/confirm`
      const { fetchMock } = await renderGroupApp(
        routes.groupBalances(groupId),
        liveOptions(
          { ...ownerView(openGroup), balances: greeceTripBalances, seedCache: seedActivityAndExpenses, answers: liveAnswers(liveMoney) },
          liveGroup,
          {
            [`POST ${confirmPath}`]: () => {
              liveMoney.settlements = [{ ...markoPaidFilipPending, status: 'Confirmed', resolvedAt: '2026-10-06T09:00:00Z' }]
              liveGroup.group = readyToCloseGroup
              return new Response(null, { status: 204 })
            },
          },
        ),
      )
      await findCardsInLanguage('en')
      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()

      fireEvent.click(buttonNamedIn(rowWithText(sectionNamed('Needs you'), markoSentence), 'Confirm'))

      expect(await findBarTitled(closingTexts.readyTitle)).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
    })

    it('takes the Closing bar and the Closing badge away when the owner deletes a payment and the server cancelled the closing', async () => {
      const liveGroup: LiveGroupState = { group: closingGroup }
      const liveMoney: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilip] }
      const { fetchMock } = await renderGroupApp(
        routes.groupBalances(groupId),
        liveOptions(
          { ...ownerView(closingGroup), balances: greeceTripBalances, seedCache: seedActivityAndExpenses, answers: liveAnswers(liveMoney) },
          liveGroup,
          {
            [`DELETE ${settlementPathOf(paymentIds.first)}`]: () => {
              liveMoney.settlements = []
              liveGroup.group = openGroup
              return new Response(null, { status: 204 })
            },
          },
        ),
      )
      await findCardsInLanguage('en')
      expect(await findBarTitled(closingTexts.waitingTitle)).toBeTruthy()

      fireEvent.click(buttonNamedIn(rowWithText(sectionNamed('Payments'), 'Marko paid Filip'), 'Delete'))

      await waitFor(() => {
        expect(screen.queryByText(closingTexts.waitingTitle.en)).toBeNull()
      })
      expect(screen.queryByText(closingTexts.badgeClosing.en)).toBeNull()
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
    })

    it('shows Finished on a One bill when the owner confirms the payment that made everybody kvit', async () => {
      const liveGroup: LiveGroupState = { group: openOneBill }
      const liveMoney: LiveGroup = { balances: greeceTripBalances, settlements: [markoPaidFilipPending] }
      const confirmPath = `${settlementPathOf(markoPaidFilipPending.id)}/confirm`
      await renderGroupApp(
        routes.groupBalances(groupId),
        liveOptions(
          { ...ownerView(openOneBill), balances: greeceTripBalances, seedCache: seedActivityAndExpenses, answers: liveAnswers(liveMoney) },
          liveGroup,
          {
            [`POST ${confirmPath}`]: () => {
              liveMoney.settlements = [{ ...markoPaidFilipPending, status: 'Confirmed', resolvedAt: '2026-10-06T09:00:00Z' }]
              liveGroup.group = finishedOneBill
              return new Response(null, { status: 204 })
            },
          },
        ),
      )
      await findCardsInLanguage('en')
      expect(screen.queryByText(closingTexts.badgeFinished.en)).toBeNull()

      fireEvent.click(buttonNamedIn(rowWithText(sectionNamed('Needs you'), markoSentence), 'Confirm'))

      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
      expect(screen.getByText(closingTexts.badgeFinished.en)).toBeTruthy()
    })
  })

  describe('after a refused answer to a closing that was cancelled or changed in the meantime', () => {
    it('takes the prompt bar away and asks the server again for the group when Confirm answers 400 GROUP_NOT_CLOSING', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: memberClosingGroup }
      const { fetchMock } = await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(memberClosingGroup), live, {
          [`POST ${closingConfirmPath}`]: () => {
            live.group = memberOpenGroup
            return problemAnswer(400, 'GROUP_NOT_CLOSING')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      await waitFor(() => {
        expect(screen.queryByText(promptBarText(), { normalizer: plainSpaces })).toBeNull()
      })
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
    })

    it('takes the prompt bar away when Object answers 400 GROUP_NOT_CLOSING', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: memberClosingGroup }
      const { fetchMock } = await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(memberClosingGroup), live, {
          [`POST ${closingObjectPath}`]: () => {
            live.group = memberOpenGroup
            return problemAnswer(400, 'GROUP_NOT_CLOSING')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)
      fireEvent.click(buttonIn(bar, closingTexts.object.en))
      const dialog = await screen.findByRole('dialog', { name: closingTexts.objectTitle.en })

      fireEvent.click(within(dialog).getByRole('button', { name: closingTexts.send.en }))

      await within(dialog).findByRole('alert')
      fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))
      await waitForSheetToClose()
      await waitFor(() => {
        expect(screen.queryByText(promptBarText(), { normalizer: plainSpaces })).toBeNull()
      })
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
    })

    it('swaps the ready bar for the waiting bar when Close the group answers 400 GROUP_NOT_OPEN because the closing already started', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: readyToCloseGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: () => {
            live.group = closingGroup
            return problemAnswer(400, 'GROUP_NOT_OPEN')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect(await findBarTitled(closingTexts.waitingTitle)).toBeTruthy()
      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
    })

    it('takes the finished bar away when Unlock answers 400 GROUP_NOT_FINISHED because the group was unlocked already', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: finishedGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(finishedGroup), live, {
          [`POST ${unlockPath}`]: () => {
            live.group = openGroup
            return problemAnswer(400, 'GROUP_NOT_FINISHED')()
          },
        }),
      )
      const bar = await findClosingBar(finishedBarText.en)

      fireEvent.click(buttonIn(bar, closingTexts.unlock.en))

      expect(await screen.findByRole('link', { name: 'Add expense' })).toBeTruthy()
      expect(screen.queryByText(finishedBarText.en)).toBeNull()
    })

    it('leaves the prompt bar alone when Confirm answers a code that says nothing about the state of the group', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { fetchMock } = await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(memberClosingGroup), { group: memberClosingGroup }, {
          [`POST ${closingConfirmPath}`]: problemAnswer(400, 'CLOSING_ALREADY_ANSWERED'),
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      await within(bar).findByRole('alert')
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(1)
      expect(screen.queryByText(promptBarText(), { normalizer: plainSpaces })).toBeTruthy()
    })
  })

  describe('the message of a refused closing action whose bar goes away', () => {
    it('shows the refusal once as a toast when Confirm answers 400 GROUP_NOT_CLOSING and the prompt bar goes away', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(memberClosingGroup), live, {
          [`POST ${closingConfirmPath}`]: () => {
            live.group = memberOpenGroup
            return problemAnswer(400, 'GROUP_NOT_CLOSING')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      await waitFor(() => {
        expect(screen.queryByText(promptBarText(), { normalizer: plainSpaces })).toBeNull()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledTimes(1)
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith('This group is no longer being closed.')
    })

    it('keeps the refusal inside the prompt bar and shows no toast when Confirm answers 400 CLOSING_ALREADY_ANSWERED', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(memberClosingGroup), { group: memberClosingGroup }, {
          [`POST ${closingConfirmPath}`]: problemAnswer(400, 'CLOSING_ALREADY_ANSWERED'),
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      expect((await within(bar).findByRole('alert')).textContent).toContain('You have already confirmed.')
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('shows the refusal once as a toast when Unlock answers 400 GROUP_NOT_FINISHED and the finished bar goes away', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: finishedGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(finishedGroup), live, {
          [`POST ${unlockPath}`]: () => {
            live.group = openGroup
            return problemAnswer(400, 'GROUP_NOT_FINISHED')()
          },
        }),
      )
      const bar = await findClosingBar(finishedBarText.en)

      fireEvent.click(buttonIn(bar, closingTexts.unlock.en))

      await waitFor(() => {
        expect(screen.queryByText(finishedBarText.en)).toBeNull()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledTimes(1)
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith('Only a finished group can be unlocked.')
    })

    it('shows the refusal once as a toast when Close the group answers 400 GROUP_NOT_OPEN and the ready bar goes away', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: readyToCloseGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: () => {
            live.group = closingGroup
            return problemAnswer(400, 'GROUP_NOT_OPEN')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      await waitFor(() => {
        expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      })
      expect(vi.mocked(toast.error)).toHaveBeenCalledTimes(1)
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith('This group is already being closed or is finished.')
    })
  })
})
