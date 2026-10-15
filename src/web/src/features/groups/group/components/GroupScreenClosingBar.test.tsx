import { cleanup, fireEvent, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { activityEventOf } from '@/test/activityTestData'
import {
  activityTab,
  balancesTab,
  expensesTab,
  renderGroupApp,
  stubGroupApp,
  type GroupAppOptions,
} from '@/test/activityTestHelpers'
import { greeceTripBalances } from '@/test/balancesTestData'
import { findBalanceCards } from '@/test/balancesTestHelpers'
import {
  anaClosingRow,
  closingEndDay,
  closingGroup,
  closingGroupAnaConfirmed,
  closingTexts,
  finishedBarText,
  finishedGroup,
  finishedGroupWithoutAnswers,
  petarClosingRow,
  readyToCloseGroup,
  textOf,
} from '@/test/closingTestData'
import {
  anaView,
  ancestorLooks,
  buttonLabelsIn,
  findBarTitled,
  findClosingBar,
  ownerView,
  plainTextOf,
  showsGroupScreen,
} from '@/test/closingTestHelpers'
import {
  categoriesPath,
  expensesPath,
  freezeTime,
  groupPath,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { macedonianOf } from '@/test/englishTextKeys'
import { groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from '@/core/router/router'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const addExpenseLinkName = 'Add expense'

const openGroup = testGroup
const closingGroupAnaWaitsPetarConfirmed = groupOf({
  memberCount: 5,
  status: 'Closing',
  closingStartedAt: closingGroup.closingStartedAt,
  closingEndsAt: closingGroup.closingEndsAt,
  closingConfirmedBy: [petarClosingRow],
  closingWaitingFor: [anaClosingRow],
})

function promptParams(owner: string = 'Filip'): Record<string, string> {
  return { name: owner, group: testGroup.name }
}

async function layoutOf(options: GroupAppOptions) {
  await renderGroupApp(routes.group(groupId), options)
  const firstRow = await screen.findByRole('link', { name: /Dinner/ })
  const plus = screen.queryByRole('link', { name: addExpenseLinkName })
  const layout = { list: ancestorLooks(firstRow), plus: plus === null ? null : ancestorLooks(plus) }
  cleanup()
  vi.unstubAllGlobals()
  return layout
}

function accessibleNameOf(element: HTMLElement): string {
  const label = element.getAttribute('aria-label')
  if (label !== null) {
    return label
  }
  const ids = (element.getAttribute('aria-labelledby') ?? '').split(' ').filter((id) => id !== '')
  return ids.map((id) => document.getElementById(id)?.textContent ?? '').join(' ')
}

describe('the closing bar on the group screen', () => {
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

  describe('for the owner of an open group where everyone is kvit', () => {
    it('asks whether to close the group, lists the two checks and has one button to close it', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(readyToCloseGroup))

      const bar = await findBarTitled(closingTexts.readyTitle)

      expect(within(bar).getByText(closingTexts.checkBalances.en)).toBeTruthy()
      expect(within(bar).getByText(closingTexts.checkPayments.en)).toBeTruthy()
      expect(buttonLabelsIn(bar)).toEqual([closingTexts.start.en])
    })

    it.each(languages)('writes the question, the checks and the button in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), ownerView(readyToCloseGroup, language))

      const bar = await findBarTitled(closingTexts.readyTitle, language)

      expect(within(bar).getByText(closingTexts.checkBalances[language])).toBeTruthy()
      expect(within(bar).getByText(closingTexts.checkPayments[language])).toBeTruthy()
      expect(buttonLabelsIn(bar)).toEqual([closingTexts.start[language]])
    })

    it.each([
      ['Expenses', routes.group(groupId)],
      ['Balances', routes.groupBalances(groupId)],
      ['Activity', routes.groupActivity(groupId)],
    ])('shows the bar on the %s tab', async (_tab, path) => {
      await renderGroupApp(path, ownerView(readyToCloseGroup))

      expect(await findBarTitled(closingTexts.readyTitle)).toBeTruthy()
    })

    it('is a named part of the page, so a screen reader can find it', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(readyToCloseGroup))

      const bar = await findBarTitled(closingTexts.readyTitle)

      expect(accessibleNameOf(bar).trim()).not.toBe('')
    })

    it('shows no badge next to the name of the group', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(readyToCloseGroup))
      await findBarTitled(closingTexts.readyTitle)

      expect(screen.queryByText(closingTexts.badgeClosing.en)).toBeNull()
      expect(screen.queryByText(closingTexts.badgeFinished.en)).toBeNull()
    })

    it('does not use a toast, because the bar stays until the person answers', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(readyToCloseGroup))
      await findBarTitled(closingTexts.readyTitle)

      expect(shownToastTexts()).toEqual([])
    })

    it('asks only for the signed-in person, the group, its expenses and the categories, so the bar costs no extra request', async () => {
      const fetchMock = stubGroupApp(ownerView(readyToCloseGroup))
      await renderRoutesWithProviders(routeObjects, routes.group(groupId))
      await findBarTitled(closingTexts.readyTitle)

      const urls = fetchMock.mock.calls.map(([url]) => String(url)).sort()
      expect(urls).toEqual(['/api/me', '/api/me/active', groupPath, expensesPath, categoriesPath].sort())
    })
  })

  describe('for an open group where the bar has nothing to say', () => {
    it('shows nothing to the owner when canStartClosing is false', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(openGroup))
      await showsGroupScreen()
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      expect(screen.queryByRole('button', { name: closingTexts.start.en })).toBeNull()
      expect(screen.queryByRole('button', { name: 'Confirm' })).toBeNull()
    })

    it('shows nothing to the owner of a brand-new group with no expense, because the server says canStartClosing is false', async () => {
      await renderGroupApp(routes.group(groupId), { ...ownerView(openGroup), expenses: [] })
      await showsGroupScreen()
      await screen.findByText(translated('en', 'expenses.emptyTitle'))

      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      expect(screen.queryByRole('button', { name: closingTexts.start.en })).toBeNull()
    })

    it('shows nothing to a member who is not the owner, even if the answer says canStartClosing', async () => {
      await renderGroupApp(routes.group(groupId), anaView(readyToCloseGroup))
      await showsGroupScreen()
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      expect(screen.queryByRole('button', { name: closingTexts.start.en })).toBeNull()
    })

    it('shows no badge', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(openGroup))
      await showsGroupScreen()

      expect(screen.queryByText(closingTexts.badgeClosing.en)).toBeNull()
      expect(screen.queryByText(closingTexts.badgeFinished.en)).toBeNull()
    })

    it('shows the add expense button as before', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(openGroup))

      expect(await screen.findByRole('link', { name: addExpenseLinkName })).toBeTruthy()
    })
  })

  describe('for the owner while the group is being closed', () => {
    it('says it is waiting for others and names each person who has not answered', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))

      const bar = await findBarTitled(closingTexts.waitingTitle)

      expect(within(bar).getByText(textOf('en', closingTexts.personWaiting, { name: anaClosingRow.name }))).toBeTruthy()
      expect(within(bar).getByText(textOf('en', closingTexts.personWaiting, { name: petarClosingRow.name }))).toBeTruthy()
    })

    it('shows who confirmed and who has not answered when one person confirmed', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroupAnaConfirmed))

      const bar = await findBarTitled(closingTexts.waitingTitle)

      expect(within(bar).getByText(textOf('en', closingTexts.personConfirmed, { name: 'Ana' }))).toBeTruthy()
      expect(within(bar).getByText(textOf('en', closingTexts.personWaiting, { name: 'Petar' }))).toBeTruthy()
      expect(within(bar).queryByText(textOf('en', closingTexts.personWaiting, { name: 'Ana' }))).toBeNull()
      expect(within(bar).queryByText(textOf('en', closingTexts.personConfirmed, { name: 'Petar' }))).toBeNull()
    })

    it.each(languages)('shows the day the closing ends and says that changing an expense or a payment cancels it (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup, language))

      const bar = await findBarTitled(closingTexts.waitingTitle, language)

      expect(plainTextOf(bar)).toContain(closingEndDay[language])
      expect(within(bar).getByText(closingTexts.changeCancels[language])).toBeTruthy()
    })

    it('has no buttons in the bar, because the owner cannot answer or cancel', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))

      const bar = await findBarTitled(closingTexts.waitingTitle)

      expect(buttonLabelsIn(bar)).toEqual([])
    })

    it.each(languages)('writes the people and the title in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroupAnaConfirmed, language))

      const bar = await findBarTitled(closingTexts.waitingTitle, language)

      expect(within(bar).getByText(textOf(language, closingTexts.personConfirmed, { name: 'Ana' }))).toBeTruthy()
      expect(within(bar).getByText(textOf(language, closingTexts.personWaiting, { name: 'Petar' }))).toBeTruthy()
    })

    it('does not offer to close the group again', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))
      await findBarTitled(closingTexts.waitingTitle)

      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      expect(screen.queryByRole('button', { name: closingTexts.start.en })).toBeNull()
    })
  })

  describe('for a member who has not answered yet', () => {
    it('says who wants to close which group and offers Confirm and Object', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroup))

      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams())

      expect(buttonLabelsIn(bar).sort()).toEqual(['Confirm', closingTexts.object.en])
    })

    it.each(languages)('writes the prompt and both buttons in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroup, language))

      const bar = await findBarTitled(closingTexts.prompt, language, promptParams())

      expect(buttonLabelsIn(bar).sort()).toEqual(
        [language === 'en' ? 'Confirm' : macedonianOf('Confirm'), closingTexts.object[language]].sort(),
      )
    })

    it('finds the member by the id of the member, so a member who is listed as waiting gets the prompt although somebody else confirmed', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroupAnaWaitsPetarConfirmed))

      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams())

      expect(buttonLabelsIn(bar).sort()).toEqual(['Confirm', closingTexts.object.en])
    })

    it('does not show the lines of the owner', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroup))
      await findBarTitled(closingTexts.prompt, 'en', promptParams())

      expect(screen.queryByText(closingTexts.waitingTitle.en)).toBeNull()
      expect(screen.queryByText(closingTexts.changeCancels.en)).toBeNull()
    })

    it('does not use a toast, because the bar stays until the person answers', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroup))
      await findBarTitled(closingTexts.prompt, 'en', promptParams())

      expect(shownToastTexts()).toEqual([])
    })

    it('stays on the screen when the person opens the other tabs', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroup))
      await findBarTitled(closingTexts.prompt, 'en', promptParams())

      fireEvent.click(balancesTab())
      expect(await findBarTitled(closingTexts.prompt, 'en', promptParams())).toBeTruthy()

      fireEvent.click(activityTab())
      expect(await findBarTitled(closingTexts.prompt, 'en', promptParams())).toBeTruthy()

      fireEvent.click(expensesTab())
      expect(await findBarTitled(closingTexts.prompt, 'en', promptParams())).toBeTruthy()
    })
  })

  describe('for a member who has confirmed', () => {
    it('says that the person confirmed, who is still waited for and when it ends, and offers only Object', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroupAnaConfirmed))

      const bar = await findBarTitled(closingTexts.youConfirmed)

      expect(within(bar).getByText(textOf('en', closingTexts.waitingFor, { names: 'Petar' }))).toBeTruthy()
      expect(plainTextOf(bar)).toContain(closingEndDay.en)
      expect(buttonLabelsIn(bar)).toEqual([closingTexts.object.en])
    })

    it.each(languages)('writes the bar in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroupAnaConfirmed, language))

      const bar = await findBarTitled(closingTexts.youConfirmed, language)

      expect(within(bar).getByText(textOf(language, closingTexts.waitingFor, { names: 'Petar' }))).toBeTruthy()
      expect(plainTextOf(bar)).toContain(closingEndDay[language])
      expect(buttonLabelsIn(bar)).toEqual([closingTexts.object[language]])
    })

    it('does not offer Confirm again', async () => {
      await renderGroupApp(routes.group(groupId), anaView(closingGroupAnaConfirmed))
      await findBarTitled(closingTexts.youConfirmed)

      expect(screen.queryByRole('button', { name: 'Confirm' })).toBeNull()
    })
  })

  describe('for a finished group', () => {
    it('tells a member that the group is finished and read-only, with no button', async () => {
      await renderGroupApp(routes.group(groupId), anaView(finishedGroup))

      const bar = await findClosingBar(finishedBarText.en)

      expect(buttonLabelsIn(bar)).toEqual([])
    })

    it('offers the owner one button to unlock the group', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(finishedGroup))

      const bar = await findClosingBar(finishedBarText.en)

      expect(buttonLabelsIn(bar)).toEqual([closingTexts.unlock.en])
    })

    it.each(languages)('writes the bar in the language of the person (%s)', async (language) => {
      await renderGroupApp(routes.group(groupId), ownerView(finishedGroup, language))

      const bar = await findClosingBar(finishedBarText[language], language)

      expect(buttonLabelsIn(bar)).toEqual([closingTexts.unlock[language]])
    })

    it('shows the bar also when nobody had to confirm, because the group finished at once', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(finishedGroupWithoutAnswers))

      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
    })

    it.each([
      ['Expenses', routes.group(groupId)],
      ['Balances', routes.groupBalances(groupId)],
      ['Activity', routes.groupActivity(groupId)],
    ])('shows the bar on the %s tab', async (_tab, path) => {
      await renderGroupApp(path, anaView(finishedGroup))

      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
    })
  })

  describe('the badge beside the name of the group', () => {
    const badgeCases = [
      ...languages.flatMap((language) => [
        [`Closing for the owner (${language})`, ownerView(closingGroup, language), closingTexts.badgeClosing[language], language],
        [`Closing for a member (${language})`, anaView(closingGroup, language), closingTexts.badgeClosing[language], language],
        [`Finished for the owner (${language})`, ownerView(finishedGroup, language), closingTexts.badgeFinished[language], language],
        [`Finished for a member (${language})`, anaView(finishedGroup, language), closingTexts.badgeFinished[language], language],
      ] as const),
    ]

    it.each(badgeCases)('shows %s next to the name of the group', async (_name, options, badgeText) => {
      await renderGroupApp(routes.group(groupId), options)

      const heading = await screen.findByRole('heading', { level: 1, name: new RegExp(`^${testGroup.name}`) })
      const badge = await screen.findByText(badgeText)

      expect(heading.contains(badge) || heading.parentElement?.contains(badge)).toBe(true)
    })

    it('keeps the name of the group as the first words of the heading', async () => {
      await renderGroupApp(routes.group(groupId), ownerView(closingGroup))

      const heading = await screen.findByRole('heading', { level: 1, name: new RegExp(`^${testGroup.name}`) })

      expect((heading.textContent ?? '').startsWith(testGroup.name)).toBe(true)
    })
  })

  describe('the add expense button and the room for the bar', () => {
    const buttonCases: [string, GroupAppOptions, boolean][] = [
      ['an open group, for the owner', ownerView(openGroup), true],
      ['an open group, for a member', anaView(openGroup), true],
      ['a group that is being closed, for the owner', ownerView(closingGroup), true],
      ['a group that is being closed, for a member', anaView(closingGroup), false],
      ['a group that is being closed, for a member who confirmed', anaView(closingGroupAnaConfirmed), false],
      ['a finished group, for the owner', ownerView(finishedGroup), false],
      ['a finished group, for a member', anaView(finishedGroup), false],
    ]

    it.each(buttonCases)('shows the button as expected for %s', async (_name, options, isShown) => {
      await renderGroupApp(routes.group(groupId), options)
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.queryByRole('link', { name: addExpenseLinkName }) !== null).toBe(isShown)
    })

    it.each([
      ['the owner sees the question to close', ownerView(readyToCloseGroup)],
      ['the owner waits for others', ownerView(closingGroup)],
      ['a member has to answer', anaView(closingGroup)],
      ['a member confirmed', anaView(closingGroupAnaConfirmed)],
      ['the group is finished', anaView(finishedGroup)],
    ])('gives the list more room at the bottom when %s, so the bar never hides the last row', async (_name, options) => {
      const withoutBar = await layoutOf(ownerView(openGroup))

      const withBar = await layoutOf(options)

      expect(withBar.list).not.toEqual(withoutBar.list)
    })

    it.each([
      ['the owner sees the question to close', ownerView(readyToCloseGroup)],
      ['the owner waits for others', ownerView(closingGroup)],
    ])('lifts the add expense button above the bar when %s', async (_name, options) => {
      const withoutBar = await layoutOf(ownerView(openGroup))

      const withBar = await layoutOf(options)

      expect(withBar.plus).not.toBeNull()
      expect(withBar.plus).not.toEqual(withoutBar.plus)
    })

    it('gives the Balances tab more room at the bottom when the bar shows', async () => {
      await renderGroupApp(routes.groupBalances(groupId), { ...ownerView(openGroup), balances: greeceTripBalances })
      const [cardWithoutBar] = await findBalanceCards()
      const withoutBar = ancestorLooks(cardWithoutBar)
      cleanup()
      vi.unstubAllGlobals()

      await renderGroupApp(routes.groupBalances(groupId), { ...ownerView(closingGroup), balances: greeceTripBalances })
      const [cardWithBar] = await findBalanceCards()

      expect(ancestorLooks(cardWithBar)).not.toEqual(withoutBar)
    })

    it('gives the Activity tab more room at the bottom when the bar shows', async () => {
      const events = [activityEventOf({ type: 'GroupCreated', data: { name: 'Greece trip' } })]
      await renderGroupApp(routes.groupActivity(groupId), { ...ownerView(openGroup), events })
      const withoutBar = ancestorLooks(await screen.findByText('Ana created the group'))
      cleanup()
      vi.unstubAllGlobals()

      await renderGroupApp(routes.groupActivity(groupId), { ...ownerView(closingGroup), events })
      const withBar = ancestorLooks(await screen.findByText('Ana created the group'))

      expect(withBar).not.toEqual(withoutBar)
    })
  })
})
