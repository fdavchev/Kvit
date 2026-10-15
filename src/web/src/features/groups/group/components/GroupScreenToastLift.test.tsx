import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import {
  asMemberOf,
  closingConfirmPath,
  closingGroup,
  closingGroupAnaConfirmed,
  closingPath,
  closingTexts,
  finishedBarText,
  finishedGroup,
  readyToCloseGroup,
  textOf,
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
import { freezeTime, groupPath, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { problemAnswer, type AnswerFactory } from '@/test/requestTestHelpers'
import { installFakeResizeObservers, type FakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { clearToastLiftOnPage, rectOfHeight, toastLiftOnPage } from '@/test/toastLiftTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const barHeight = 120
const gapAboveTheBar = 12
const liftOfTheBar = `${barHeight + gapAboveTheBar}px`
const memberClosingGroup = asMemberOf(closingGroup)
const memberOpenGroup = asMemberOf(testGroup)
const promptParams = { name: 'Filip', group: testGroup.name }

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

describe('the lift of the toasts on the group screen', () => {
  let observers: FakeResizeObservers
  let heightOfEveryElement: ReturnType<typeof vi.spyOn>

  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
    observers = installFakeResizeObservers()
    heightOfEveryElement = vi
      .spyOn(Element.prototype, 'getBoundingClientRect')
      .mockReturnValue(rectOfHeight(barHeight))
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    clearToastLiftOnPage()
  })

  describe('while a closing bar shows', () => {
    it.each([
      ['the question to close, for the owner', ownerView(readyToCloseGroup), () => findBarTitled(closingTexts.readyTitle)],
      ['the waiting bar, for the owner', ownerView(closingGroup), () => findBarTitled(closingTexts.waitingTitle)],
      ['the prompt, for a member who has not answered', anaView(closingGroup), () => findBarTitled(closingTexts.prompt, 'en', promptParams)],
      ['the confirmed bar, for a member who confirmed', anaView(closingGroupAnaConfirmed), () => findBarTitled(closingTexts.youConfirmed)],
      ['the finished bar, for a member', anaView(finishedGroup), () => findClosingBar(finishedBarText.en)],
    ])('lifts the toasts above %s by the height of the bar plus 12px', async (_name, options, findBar) => {
      await renderGroupApp(routes.group(groupId), options)

      await findBar()

      expect(toastLiftOnPage()).toBe(liftOfTheBar)
    })

    it('lifts the toasts also on the other tabs', async () => {
      await renderGroupApp(routes.groupBalances(groupId), ownerView(closingGroup))

      await findBarTitled(closingTexts.waitingTitle)

      expect(toastLiftOnPage()).toBe(liftOfTheBar)
    })

    it('follows the bar when its height changes', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      const bar = await findBarTitled(closingTexts.waitingTitle)

      heightOfEveryElement.mockReturnValue(rectOfHeight(200))
      observers.fireFor(bar)

      expect(toastLiftOnPage()).toBe('212px')
    })
  })

  describe('while no closing bar shows', () => {
    it('does not lift the toasts on an open group for the owner', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(testGroup))

      await screen.findByRole('link', { name: /Dinner/ })

      expect(toastLiftOnPage()).toBe('')
    })

    it('does not lift the toasts on an open group for a member', async () => {
      await renderGroupApp(routes.group(groupId), anaView(testGroup))

      await screen.findByRole('link', { name: /Dinner/ })

      expect(toastLiftOnPage()).toBe('')
    })
  })

  describe('when the bar goes away or changes', () => {
    it('takes the lift away when the bar disappears because the closing was cancelled', async () => {
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
      expect(toastLiftOnPage()).toBe(liftOfTheBar)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      await waitFor(() => {
        expect(screen.queryByText(textOf('en', closingTexts.prompt, promptParams))).toBeNull()
      })
      expect(toastLiftOnPage()).toBe('')
    })

    it('keeps the lift when one bar is swapped for another', async () => {
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
      const readyBar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(readyBar, closingTexts.start.en))

      await findBarTitled(closingTexts.waitingTitle)
      expect(toastLiftOnPage()).toBe(liftOfTheBar)
    })

    it('takes the lift away when the person leaves the group screen', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      await findBarTitled(closingTexts.waitingTitle)
      expect(toastLiftOnPage()).toBe(liftOfTheBar)

      cleanup()

      expect(toastLiftOnPage()).toBe('')
    })

    it('stops watching the bar when the person leaves the group screen', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      const bar = await findBarTitled(closingTexts.waitingTitle)
      expect(observers.observedElements()).toEqual([bar])

      cleanup()

      expect(observers.observedElements()).toEqual([])
    })
  })
})
