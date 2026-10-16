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
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const areaHeight = 120
const gapAboveTheArea = 12
const liftOfTheArea = `${areaHeight + gapAboveTheArea}px`
const memberClosingGroup = asMemberOf(closingGroup)
const memberOpenGroup = asMemberOf(testGroup)
const promptParams = { name: 'Filip', group: testGroup.name }

function floatingAreaOf(element: HTMLElement): HTMLElement {
  const area = element.closest<HTMLElement>('.sticky')
  if (area === null) {
    throw new Error('The element is not inside the floating area at the bottom of the group screen')
  }
  return area
}

function findAddExpenseButton(): Promise<HTMLElement> {
  return screen.findByRole('link', { name: translated('en', 'expense.addTitle') })
}

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
      .mockReturnValue(rectOfHeight(areaHeight))
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
    ])('lifts the toasts above %s by the height of the area that holds it plus 12px', async (_name, options, findBar) => {
      await renderGroupApp(routes.group(groupId), options)

      await findBar()

      expect(toastLiftOnPage()).toBe(liftOfTheArea)
    })

    it('lifts the toasts also on the other tabs', async () => {
      await renderGroupApp(routes.groupBalances(groupId), ownerView(closingGroup))

      await findBarTitled(closingTexts.waitingTitle)

      expect(toastLiftOnPage()).toBe(liftOfTheArea)
    })

    it('follows the area that holds the bar when its height changes', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      const bar = await findBarTitled(closingTexts.waitingTitle)

      heightOfEveryElement.mockReturnValue(rectOfHeight(200))
      observers.fireFor(floatingAreaOf(bar))

      expect(toastLiftOnPage()).toBe('212px')
    })
  })

  describe('while no closing bar shows', () => {
    it.each([
      ['the owner', ownerView(testGroup)],
      ['a member', anaView(testGroup)],
    ])('lifts the toasts above the + of an open group for %s, so the + stays reachable', async (_name, options) => {
      await renderGroupApp(routes.group(groupId), options)

      await findAddExpenseButton()

      expect(toastLiftOnPage()).toBe(liftOfTheArea)
    })

    it('watches the area that holds the +', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(testGroup))

      const addButton = await findAddExpenseButton()

      expect(observers.observedElements()).toEqual([floatingAreaOf(addButton)])
    })

    it('does not lift the toasts on the Balances tab of an open group, where nothing floats', async () => {
      await renderGroupApp(routes.groupBalances(groupId), ownerView(testGroup))

      await screen.findByRole('link', { name: translated('en', 'members.title') })

      expect(toastLiftOnPage()).toBe('')
    })
  })

  describe('when the bar goes away or changes', () => {
    it('takes the lift away on the Balances tab when the bar disappears because the closing was cancelled', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.groupBalances(groupId),
        liveOptions(anaView(memberClosingGroup), live, {
          [`POST ${closingConfirmPath}`]: () => {
            live.group = memberOpenGroup
            return problemAnswer(400, 'GROUP_NOT_CLOSING')()
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)
      expect(toastLiftOnPage()).toBe(liftOfTheArea)

      fireEvent.click(buttonIn(bar, 'Confirm'))

      await waitFor(() => {
        expect(screen.queryByText(textOf('en', closingTexts.prompt, promptParams))).toBeNull()
      })
      expect(toastLiftOnPage()).toBe('')
    })

    it('keeps the toasts above the + on the Expenses tab when the bar disappears because the closing was cancelled', async () => {
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

      await findAddExpenseButton()
      expect(screen.queryByText(textOf('en', closingTexts.prompt, promptParams))).toBeNull()
      expect(toastLiftOnPage()).toBe(liftOfTheArea)
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
      expect(toastLiftOnPage()).toBe(liftOfTheArea)
    })

    it('takes the lift away when the person leaves the group screen', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      await findBarTitled(closingTexts.waitingTitle)
      expect(toastLiftOnPage()).toBe(liftOfTheArea)

      cleanup()

      expect(toastLiftOnPage()).toBe('')
    })

    it('stops watching the area that holds the bar when the person leaves the group screen', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      const bar = await findBarTitled(closingTexts.waitingTitle)
      expect(observers.observedElements()).toEqual([floatingAreaOf(bar)])

      cleanup()

      expect(observers.observedElements()).toEqual([])
    })
  })
})
