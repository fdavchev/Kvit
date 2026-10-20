import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import {
  budgetNow,
  findEntryList,
  findTotalCard,
  personalEntriesGet,
  plusButton,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { amountField, saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { coffeeCategory, createdTeaCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import { personalCategoryCreatePost } from '@/test/personalCategoryTestHelpers'
import { coffeeEntry, entryAnswerRow, lunchInEuroEntry, monthAnswerBody, octoberMonth } from '@/test/personalEntryTestData'
import {
  createCategoryFromGrid,
  deleteButton,
  expectAmountToStayFocused,
  findDetailsSheet,
  findPickSheet,
  personalEntriesPost,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { jsonAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const octoberWithNewCoffee: PersonalEntryMonth = {
  month: '2026-10',
  entries: [{ ...coffeeEntry, id: 'f0000000-0000-4000-8000-000000000099', amountMinor: 9000 }, ...octoberMonth.entries],
  totals: [
    {
      currency: 'MKD',
      totalMinor: 21000,
      categories: [{ category: coffeeCategory, totalMinor: 21000, count: 2 }],
    },
    {
      currency: 'EUR',
      totalMinor: 1250,
      categories: [{ category: personalFoodCategory, totalMinor: 1250, count: 1 }],
    },
  ],
}

function liveOctober(): { answer: () => Response; next: (month: PersonalEntryMonth) => void } {
  let current = octoberMonth
  return {
    answer: () => Response.json(monthAnswerBody(current)),
    next: (month) => {
      current = month
    },
  }
}

function entryButton(list: HTMLElement, index: number): HTMLElement {
  const button = within(list).getAllByRole('button')[index]
  if (button === undefined) {
    throw new Error(`The entry list has no entry number ${index + 1}`)
  }
  return button
}

describe('BudgetScreen adding and editing', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  describe('the "+" button', () => {
    it.each(languages)('is a round floating button with its name for screen readers (%s)', async (language) => {
      await renderBudget({ language })
      await findTotalCard(language)

      const button = plusButton(language)

      expect(button.textContent).toBe('+')
      expect(button.className.split(/\s+/)).toEqual(expect.arrayContaining(['size-15', 'rounded-[20px]']))
      expect(button.closest('[data-floating-add]')?.className.split(/\s+/)).toEqual(
        expect.arrayContaining(['sticky', 'bottom-[calc(var(--bottom-bar-height)+16px)]']),
      )
    })

    it('opens the add sheet straight on the category grid, with no menu in between', async () => {
      await renderBudget()
      await findTotalCard()

      fireEvent.click(plusButton())

      expect(await findPickSheet()).toBeTruthy()
      expect(screen.queryByRole('menu')).toBeNull()
    })

    it('adds spending in 2 taps and 1 field: "+", category, amount, Save', async () => {
      const { fetchMock } = await renderBudget({ answers: { [personalEntriesPost]: jsonAnswer(entryAnswerRow(coffeeEntry)) } })
      await findTotalCard()
      let taps = 0

      fireEvent.click(plusButton())
      const pickSheet = await findPickSheet()
      fireEvent.click(await within(pickSheet).findByRole('button', { name: 'Coffee' }))
      taps += 1
      await findDetailsSheet(coffeeCategory)
      typeAmount('90')
      fireEvent.click(saveButton())
      taps += 1

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
      })
      expect(taps).toBe(2)
    })

    it('focuses the amount after "+ New" saves a category, so typed digits land in it', async () => {
      await renderBudget({ answers: { [personalCategoryCreatePost]: jsonAnswer(createdTeaCategory) } })
      await findTotalCard()
      plusButton().focus()
      fireEvent.click(plusButton())

      await createCategoryFromGrid('Tea')
      await findDetailsSheet(createdTeaCategory)

      await expectAmountToStayFocused()
    })

    it('shows the new entry and the new totals after the save, from a fresh answer of the server', async () => {
      const october = liveOctober()
      const { fetchMock } = await renderBudget({
        answers: {
          [personalEntriesGet('2026-10')]: october.answer,
          [personalEntriesPost]: () => {
            october.next(octoberWithNewCoffee)
            return Response.json(entryAnswerRow(coffeeEntry))
          },
        },
      })
      const list = await findEntryList()
      expect(within(list).getAllByRole('listitem')).toHaveLength(2)

      fireEvent.click(plusButton())
      fireEvent.click(await within(await findPickSheet()).findByRole('button', { name: 'Coffee' }))
      await findDetailsSheet(coffeeCategory)
      typeAmount('90')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      await waitFor(() => {
        expect(within(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })).getAllByRole('listitem')).toHaveLength(3)
      })
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(2)
    })

    it('can open the add sheet again after it was closed', async () => {
      await renderBudget({ answers: { [personalEntriesPost]: jsonAnswer(entryAnswerRow(coffeeEntry)) } })
      await findTotalCard()
      fireEvent.click(plusButton())
      fireEvent.click(await within(await findPickSheet()).findByRole('button', { name: 'Coffee' }))
      await findDetailsSheet(coffeeCategory)
      typeAmount('90')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      fireEvent.click(plusButton())

      expect(await findPickSheet()).toBeTruthy()
    })

    it('shows a round "Add spending" button in the page instead of the floating "+" on a desktop-sized window', async () => {
      stubDesktopScreen()
      await renderBudget()
      await findTotalCard()

      const button = plusButton()

      expect(button.textContent).toContain(translated('en', 'budget.screen.add'))
      expect(button.className.split(/\s+/)).toEqual(expect.arrayContaining(['rounded-full', 'min-h-12', 'bg-primary']))
      expect(document.querySelector('[data-floating-add]')).toBeNull()
      fireEvent.click(button)
      expect(await findPickSheet()).toBeTruthy()
    })
  })

  describe('tapping an entry', () => {
    it.each(languages)('opens the sheet filled in with that entry (%s)', async (language) => {
      await renderBudget({ language })
      const list = await findEntryList(language)

      fireEvent.click(entryButton(list, 1))

      const sheet = await findDetailsSheet(personalFoodCategory, language)
      expect(amountField(language).value).toBe('12.50')
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated(language, 'expense.note')).value).toBe(
        lunchInEuroEntry.note,
      )
      expect(deleteButton(sheet, language)).toBeTruthy()
    })

    it('opens the entry the person tapped, not another one with the same category', async () => {
      await renderBudget()
      const list = await findEntryList()

      fireEvent.click(entryButton(list, 0))

      expect(await findDetailsSheet(coffeeCategory)).toBeTruthy()
      expect(amountField().value).toBe('120')
    })

    it('opens no sheet before an entry is tapped', async () => {
      await renderBudget()
      await findEntryList()

      expect(screen.queryByRole('dialog')).toBeNull()
    })
  })
})
