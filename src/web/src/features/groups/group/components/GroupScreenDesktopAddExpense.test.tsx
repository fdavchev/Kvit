import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/core/router/routes'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { closingGroup, closingTexts, finishedGroup } from '@/test/closingTestData'
import { anaView, findBarTitled, ownerView, showsGroupScreen } from '@/test/closingTestHelpers'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { installFakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import { clearToastLiftOnPage, rectOfHeight, toastLiftOnPage } from '@/test/toastLiftTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const addExpenseName = translated('en', 'expense.addTitle')

function findAddExpenseLink(): Promise<HTMLElement> {
  return screen.findByRole('link', { name: addExpenseName })
}

function queryAddExpenseLink(): HTMLElement | null {
  return screen.queryByRole('link', { name: addExpenseName })
}

function isInFloatingArea(element: HTMLElement): boolean {
  return element.closest('.sticky') !== null
}

describe('the Add expense button of the group screen on a desktop-sized window', () => {
  beforeEach(() => {
    freezeTime()
    stubDesktopScreen()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows a round Add expense button in the page and no floating +', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(testGroup))

    const addExpense = await findAddExpenseLink()

    expect(screen.getAllByRole('link', { name: addExpenseName })).toHaveLength(1)
    expect(isInFloatingArea(addExpense)).toBe(false)
    expect(addExpense.className.split(/\s+/)).toEqual(expect.arrayContaining(['rounded-full', 'min-h-12', 'bg-primary']))
  })

  it('puts the Add expense button in the row with Members and Group settings', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(testGroup))

    const addExpense = await findAddExpenseLink()
    const members = screen.getByRole('link', { name: translated('en', 'members.title') })
    const settings = screen.getByRole('link', { name: translated('en', 'group.settings') })

    expect(addExpense.parentElement).toBe(members.parentElement)
    expect(addExpense.parentElement).toBe(settings.parentElement)
  })

  it('links to the same add-expense page as the floating +', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(testGroup))

    const addExpense = await findAddExpenseLink()

    expect(addExpense.getAttribute('href')).toBe(routes.groupExpenseNew(groupId))
  })

  it('shows the Add expense button to a member of an open group', async () => {
    await renderGroupApp(routes.group(groupId), anaView(testGroup))

    const addExpense = await findAddExpenseLink()

    expect(isInFloatingArea(addExpense)).toBe(false)
  })

  it('shows the Add expense button to the owner of a Closing group, who may still change it', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(closingGroup))

    const addExpense = await findAddExpenseLink()

    expect(isInFloatingArea(addExpense)).toBe(false)
  })

  it.each([
    ['a member of a Closing group', anaView(closingGroup)],
    ['the owner of a Finished group', ownerView(finishedGroup)],
    ['a member of a Finished group', anaView(finishedGroup)],
  ])('hides the Add expense button from %s, who cannot change the group', async (_name, options) => {
    await renderGroupApp(routes.group(groupId), options)

    await showsGroupScreen()

    expect(queryAddExpenseLink()).toBeNull()
  })

  it.each([
    ['Balances', routes.groupBalances(groupId)],
    ['Activity', routes.groupActivity(groupId)],
  ])('shows no Add expense button on the %s tab, as the floating + does not', async (_name, path) => {
    await renderGroupApp(path, ownerView(testGroup))

    await showsGroupScreen()

    expect(queryAddExpenseLink()).toBeNull()
  })
})

describe('the Add expense button of the group screen on a phone-sized window', () => {
  beforeEach(() => {
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.restoreAllMocks()
  })

  it('shows only the floating + and no round button in the page', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(testGroup))

    const addExpense = await findAddExpenseLink()

    expect(screen.getAllByRole('link', { name: addExpenseName })).toHaveLength(1)
    expect(isInFloatingArea(addExpense)).toBe(true)
    expect(addExpense.getAttribute('href')).toBe(routes.groupExpenseNew(groupId))
  })
})

describe('the lift of the toasts on the group screen on a desktop-sized window', () => {
  const areaHeight = 120

  beforeEach(() => {
    freezeTime()
    stubDesktopScreen()
    installFakeResizeObservers()
    vi.spyOn(Element.prototype, 'getBoundingClientRect').mockReturnValue(rectOfHeight(areaHeight))
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    clearToastLiftOnPage()
  })

  it('still lifts the toasts above the closing bar, which floats without the +', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(closingGroup))

    const bar = await findBarTitled(closingTexts.waitingTitle)
    const addExpense = await findAddExpenseLink()

    expect(isInFloatingArea(bar)).toBe(true)
    expect(isInFloatingArea(addExpense)).toBe(false)
    expect(toastLiftOnPage()).toBe(`${areaHeight + 12}px`)
  })

  it('does not lift the toasts in an open group, where nothing floats', async () => {
    await renderGroupApp(routes.group(groupId), ownerView(testGroup))

    await findAddExpenseLink()

    expect(toastLiftOnPage()).toBe('')
  })
})
