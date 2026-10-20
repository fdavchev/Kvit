import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { PersonalEntryMonth } from '@/core/services/personalEntries/personalEntriesService'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import {
  budgetNow,
  findEntryList,
  findTotalCard,
  personalEntriesGet,
  plusButton,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { amountField, dismissSheetByTappingBackdrop, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import {
  coffeeEntry,
  entryAnswerRow,
  monthAnswerBody,
  octoberMonth,
  octoberWithSalary,
  salaryIncomeEntry,
} from '@/test/personalEntryTestData'
import {
  choiceButton,
  chooseSpending,
  findChoiceSheet,
  findDetailsSheet,
  personalEntriesPost,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { chooseIncome, findIncomeSheet } from '@/test/personalIncomeTestHelpers'
import { jsonAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const savedIncomeAnswer = { [personalEntriesPost]: jsonAnswer(entryAnswerRow(salaryIncomeEntry)) }

function liveOctober(): { answer: () => Response; next: (month: PersonalEntryMonth) => void } {
  let current = octoberMonth
  return {
    answer: () => Response.json(monthAnswerBody(current)),
    next: (month) => {
      current = month
    },
  }
}

async function waitForNoDialog(): Promise<void> {
  await waitForEveryDialogToClose()
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('BudgetScreen, the "What do you add?" sheet', () => {
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

  describe('opening', () => {
    it.each(languages)('is a glass sheet titled "What do you add?" with a Spending and an Income button (%s)', async (language) => {
      await renderBudget({ language })
      await findTotalCard(language)

      fireEvent.click(plusButton(language))
      const sheet = await findChoiceSheet(language)

      expect(sheet.className.split(/\s+/)).toEqual(expect.arrayContaining(kvitGlassPanelLook.split(/\s+/)))
      expect(within(sheet).getAllByRole('button')).toHaveLength(2)
      expect(choiceButton(sheet, 'spending', language)).toBeTruthy()
      expect(choiceButton(sheet, 'income', language)).toBeTruthy()
    })

    it('gives both buttons a tap height of at least 44 px', async () => {
      await renderBudget()
      await findTotalCard()

      fireEvent.click(plusButton())
      const sheet = await findChoiceSheet()

      for (const button of within(sheet).getAllByRole('button')) {
        expect(button.className.split(/\s+/)).toEqual(expect.arrayContaining([expect.stringMatching(/^min-h-(1[1-9]|20)$/)]))
      }
    })

    it('opens from the round "+ Add" button of a desktop-sized window too', async () => {
      stubDesktopScreen()
      await renderBudget()
      await findTotalCard()

      fireEvent.click(plusButton())

      const sheet = await findChoiceSheet()
      expect(choiceButton(sheet, 'spending')).toBeTruthy()
      expect(choiceButton(sheet, 'income')).toBeTruthy()
    })

    it('sends no request by itself', async () => {
      const { fetchMock } = await renderBudget()
      await findTotalCard()

      fireEvent.click(plusButton())
      await findChoiceSheet()

      expect(requestCount(fetchMock, 'POST', '/api/personal/entries')).toBe(0)
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.title') })).toBeNull()
    })
  })

  describe('choosing Spending', () => {
    it('replaces the choice sheet with the category grid', async () => {
      await renderBudget()
      await findTotalCard()
      fireEvent.click(plusButton())

      const pickSheet = await chooseSpending()

      expect(await within(pickSheet).findByRole('button', { name: 'Coffee' })).toBeTruthy()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.add.title') })).toBeNull()
      })
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.income.title') })).toBeNull()
    })

    it('goes on to the amount after a category tap, as before', async () => {
      await renderBudget()
      await findTotalCard()
      fireEvent.click(plusButton())

      fireEvent.click(await within(await chooseSpending()).findByRole('button', { name: 'Coffee' }))

      expect(await findDetailsSheet(coffeeCategory)).toBeTruthy()
      await waitFor(() => {
        expect(document.activeElement).toBe(amountField())
      })
    })

    it('sends kind Spending and the category when the spending is saved', async () => {
      const { fetchMock } = await renderBudget({ answers: { [personalEntriesPost]: jsonAnswer(entryAnswerRow(coffeeEntry)) } })
      await findTotalCard()
      fireEvent.click(plusButton())

      fireEvent.click(await within(await chooseSpending()).findByRole('button', { name: 'Coffee' }))
      await findDetailsSheet(coffeeCategory)
      typeAmount('90')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ kind: 'Spending', categoryId: coffeeCategory.id })
    })
  })

  describe('choosing Income', () => {
    it('replaces the choice sheet with the income sheet, with no category grid', async () => {
      await renderBudget()
      await findTotalCard()
      fireEvent.click(plusButton())

      const incomeSheet = await chooseIncome()

      expect(incomeSheet).toBeTruthy()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.add.title') })).toBeNull()
      })
      expect(screen.queryByRole('dialog', { name: translated('en', 'budget.entries.pickCategory') })).toBeNull()
    })

    it('focuses the amount of the income sheet', async () => {
      await renderBudget()
      await findTotalCard()
      fireEvent.click(plusButton())
      await chooseIncome()

      await waitFor(() => {
        expect(document.activeElement).toBe(amountField())
      })
    })

    it.each(languages)('writes the income sheet title in %s', async (language) => {
      await renderBudget({ language })
      await findTotalCard(language)
      fireEvent.click(plusButton(language))

      const sheet = await chooseIncome(language)

      expect(within(sheet).getByRole('heading', { name: translated(language, 'budget.income.title') })).toBeTruthy()
    })

    it('adds income in 2 taps and 1 field: Income, amount, Save', async () => {
      const { fetchMock } = await renderBudget({ answers: savedIncomeAnswer })
      await findTotalCard()
      fireEvent.click(plusButton())
      await findChoiceSheet()
      let taps = 0

      fireEvent.click(choiceButton(await findChoiceSheet(), 'income'))
      taps += 1
      await findIncomeSheet()
      typeAmount('65000')
      fireEvent.click(saveButton())
      taps += 1

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
      })
      expect(taps).toBe(2)
      const body = sentBody(fetchMock, 'POST', '/api/personal/entries')
      expect(body).toMatchObject({ kind: 'Income', amountMinor: 6500000, currency: 'MKD' })
      expect(Object.keys(body)).not.toContain('categoryId')
    })

    it('adds income from the round button of a desktop-sized window', async () => {
      stubDesktopScreen()
      const { fetchMock } = await renderBudget({ answers: savedIncomeAnswer })
      await findTotalCard()

      fireEvent.click(plusButton())
      await chooseIncome()
      typeAmount('65000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', '/api/personal/entries')).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'POST', '/api/personal/entries')).toMatchObject({ kind: 'Income' })
    })

    it('shows the new income row and the Spent card from a fresh answer of the server after the save', async () => {
      const october = liveOctober()
      const { fetchMock } = await renderBudget({
        answers: {
          [personalEntriesGet('2026-10')]: october.answer,
          [personalEntriesPost]: () => {
            october.next(octoberWithSalary)
            return Response.json(entryAnswerRow(salaryIncomeEntry))
          },
        },
      })
      const list = await findEntryList()
      expect(within(list).getAllByRole('listitem')).toHaveLength(2)

      fireEvent.click(plusButton())
      await chooseIncome()
      typeAmount('65000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      await waitFor(() => {
        expect(within(screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })).getAllByRole('listitem')).toHaveLength(3)
      })
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(2)
    })

    it('can open the choice sheet again after an income was saved', async () => {
      await renderBudget({ answers: savedIncomeAnswer })
      await findTotalCard()
      fireEvent.click(plusButton())
      await chooseIncome()
      typeAmount('65000')
      fireEvent.click(saveButton())
      await waitForEveryDialogToClose()

      fireEvent.click(plusButton())

      expect(await findChoiceSheet()).toBeTruthy()
    })
  })

  describe('cancelling', () => {
    const cancels: [string, () => void][] = [
      ['Escape', () => fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })],
      ['a tap on the backdrop', () => dismissSheetByTappingBackdrop()],
    ]
    const devices: [string, () => void][] = [
      ['a phone', () => undefined],
      ['a desktop-sized window', () => stubDesktopScreen()],
    ]

    it.each(cancels)('closes with %s without opening the grid or the income sheet', async (_name, cancel) => {
      await renderBudget()
      await findTotalCard()
      fireEvent.click(plusButton())
      await findChoiceSheet()

      cancel()

      await waitForNoDialog()
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(cancels)('puts the focus back on the "+" button when closed with %s', async (_name, cancel) => {
      await renderBudget()
      await findTotalCard()
      plusButton().focus()
      fireEvent.click(plusButton())
      await findChoiceSheet()

      cancel()

      await waitForNoDialog()
      expect(document.activeElement).toBe(plusButton())
    })

    it.each(devices)('puts the focus back on the "+" button on %s', async (_name, prepareDevice) => {
      prepareDevice()
      await renderBudget()
      await findTotalCard()
      plusButton().focus()
      fireEvent.click(plusButton())
      await findChoiceSheet()

      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

      await waitForNoDialog()
      expect(document.activeElement).toBe(plusButton())
    })

    it('sends no request when the choice sheet is cancelled', async () => {
      const { fetchMock } = await renderBudget()
      await findTotalCard()
      const requestsBefore = fetchMock.mock.calls.length
      fireEvent.click(plusButton())
      await findChoiceSheet()

      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

      await waitForNoDialog()
      expect(fetchMock.mock.calls.length).toBe(requestsBefore)
    })
  })
})
