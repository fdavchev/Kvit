import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { dismissSheetByTappingBackdrop, saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { renderHome } from '@/test/homeTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { activePersonalCategories, coffeeCategory, createdTeaCategory } from '@/test/personalCategoryTestData'
import { categoryListAnswer, personalCategoriesGet, personalCategoryCreatePost } from '@/test/personalCategoryTestHelpers'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import {
  createCategoryFromGrid,
  expectAmountToStayFocused,
  expectFocusBackOnNewTile,
  findDetailsSheet,
  findPickSheet,
  openNewCategorySheetFromGrid,
  personalEntriesPost,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { jsonAnswer, requestsOf } from '@/test/requestTestHelpers'
import { seedMe, testGuestMe } from '@/test/testMe'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const spendingAnswers = {
  [personalCategoriesGet]: categoryListAnswer(() => activePersonalCategories),
  [personalEntriesPost]: jsonAnswer(entryAnswerRow(coffeeEntry)),
}

function spendingName(language: Language = 'en'): string {
  return translated(language, 'budget.entries.homeItem')
}

async function openPlusMenu(language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(await screen.findByRole('button', { name: translated(language, 'home.plus') }))
  return screen.getByRole('menu')
}

describe('personal spending from the home screen', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it.each(languages)('puts "Personal spending" last in the plus menu, after the group items (%s)', async (language) => {
    await renderHome({ language, answers: spendingAnswers })

    const menu = await openPlusMenu(language)

    const items = within(menu).getAllByRole('menuitem')
    expect(items.map((item) => item.textContent)).toEqual([
      expect.stringContaining(translated(language, 'home.plusAddTo', { group: 'Greece trip' })),
      expect.stringContaining(translated(language, 'home.plusNewOneBill')),
      expect.stringContaining(translated(language, 'home.plusAnotherGroup')),
      expect.stringContaining(spendingName(language)),
    ])
    expect(items[3]).toBe(within(menu).getByRole('menuitem', { name: spendingName(language) }))
  })

  it('gives the item a 44 px tap height and the plain look, so Add to stays the main item', async () => {
    await renderHome({ answers: spendingAnswers })
    const menu = await openPlusMenu()

    const item = within(menu).getByRole('menuitem', { name: spendingName() })

    expect(Array.from(item.classList)).toContain('min-h-12')
    expect(Array.from(item.classList)).not.toContain('bg-primary')
  })

  it('adds personal spending from Home in 4 taps and 1 field: plus, Personal spending, category, Save', async () => {
    const { fetchMock } = await renderHome({ answers: spendingAnswers })
    let taps = 0

    const menu = await openPlusMenu()
    taps += 1
    fireEvent.click(within(menu).getByRole('menuitem', { name: spendingName() }))
    taps += 1
    const pickSheet = await findPickSheet()
    fireEvent.click(await within(pickSheet).findByRole('button', { name: 'Coffee' }))
    taps += 1
    await findDetailsSheet(coffeeCategory)
    typeAmount('120')
    fireEvent.click(saveButton())
    taps += 1

    await waitFor(() => {
      expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
    })
    expect(taps).toBe(4)
  })

  it('focuses the amount after "+ New" saves a category, so typed digits land in it', async () => {
    await renderHome({ answers: { ...spendingAnswers, [personalCategoryCreatePost]: jsonAnswer(createdTeaCategory) } })
    const plus = await screen.findByRole('button', { name: translated('en', 'home.plus') })
    plus.focus()
    fireEvent.click(plus)
    const item = within(screen.getByRole('menu')).getByRole('menuitem', { name: spendingName() })
    item.focus()
    fireEvent.click(item)

    await createCategoryFromGrid('Tea')
    await findDetailsSheet(createdTeaCategory)

    await expectAmountToStayFocused()
  })

  it.each([
    ['Escape', (newSheet: HTMLElement) => fireEvent.keyDown(newSheet, { key: 'Escape' })],
    ['a tap on the backdrop', () => dismissSheetByTappingBackdrop()],
  ])('puts the focus back on "+ New" in the open grid when the new category is cancelled with %s', async (_name, cancel) => {
    await renderHome({ answers: spendingAnswers })
    const plus = await screen.findByRole('button', { name: translated('en', 'home.plus') })
    plus.focus()
    fireEvent.click(plus)
    const item = within(screen.getByRole('menu')).getByRole('menuitem', { name: spendingName() })
    item.focus()
    fireEvent.click(item)
    const opened = await openNewCategorySheetFromGrid()

    cancel(opened.newSheet)

    await expectFocusBackOnNewTile(opened)
  })

  it('closes the menu when the sheet opens and stays on Home after the save', async () => {
    const { router } = await renderHome({ answers: spendingAnswers })
    const menu = await openPlusMenu()

    fireEvent.click(within(menu).getByRole('menuitem', { name: spendingName() }))
    await findPickSheet()

    expect(screen.queryByRole('menu')).toBeNull()
    fireEvent.click(await within(await findPickSheet()).findByRole('button', { name: 'Coffee' }))
    await findDetailsSheet(coffeeCategory)
    typeAmount('120')
    fireEvent.click(saveButton())
    await waitForEveryDialogToClose()
    expect(router.state.location.pathname).toBe('/')
  })

  it('can open the sheet again after it was closed', async () => {
    await renderHome({ answers: spendingAnswers })
    fireEvent.click(within(await openPlusMenu()).getByRole('menuitem', { name: spendingName() }))
    fireEvent.click(await within(await findPickSheet()).findByRole('button', { name: 'Coffee' }))
    await findDetailsSheet(coffeeCategory)
    typeAmount('120')
    fireEvent.click(saveButton())
    await waitForEveryDialogToClose()

    fireEvent.click(within(await openPlusMenu()).getByRole('menuitem', { name: spendingName() }))

    expect(await findPickSheet()).toBeTruthy()
  })

  it('opens the same sheet from the round button on a desktop-sized window', async () => {
    stubDesktopScreen()
    await renderHome({ answers: spendingAnswers })

    const button = await screen.findByRole('button', { name: spendingName() })
    fireEvent.click(button)

    expect(await findPickSheet()).toBeTruthy()
    expect(button.className.split(/\s+/)).toEqual(expect.arrayContaining(['rounded-full', 'min-h-12', 'border-field-border']))
  })

  describe('for a guest', () => {
    function renderGuestHome() {
      return renderHome({
        answers: { 'GET /api/me': jsonAnswer(testGuestMe) },
        seedCache: seedMe(testGuestMe),
      })
    }

    it('has no Personal spending item in the plus menu', async () => {
      await renderGuestHome()

      const menu = await openPlusMenu()

      expect(within(menu).queryByRole('menuitem', { name: spendingName() })).toBeNull()
    })

    it('has no Personal spending round button on a desktop-sized window', async () => {
      stubDesktopScreen()
      await renderGuestHome()

      await screen.findByRole('link', { name: translated('en', 'home.plusAnotherGroup') })

      expect(screen.queryByRole('button', { name: spendingName() })).toBeNull()
    })
  })
})
