import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { budgetNow } from '@/test/budgetTestHelpers'
import { amountField, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { findIncomeSheet } from '@/test/personalIncomeTestHelpers'
import {
  blockButton,
  blocksIn,
  endsChip,
  repeatsChipIn,
  renderRepeatingScreen,
  sentWritesToRecurringIncomes,
  signedMoneyText,
  weekdayName,
} from '@/test/recurringIncomeTestHelpers'
import {
  catchUpAnswer,
  catchUpPost,
  flatRentRecurringIncome,
  liveRecurringList,
  recurringIncomeAnswerRow,
  recurringIncomeRestartPost,
  recurringIncomeStopPost,
  recurringIncomeUrl,
  recurringIncomesGet,
  recurringIncomesPost,
  recurringListAnswer,
  salaryRecurringIncome,
  tutoringRecurringIncome,
  untitledRecurringIncome,
} from '@/test/recurringIncomeTestData'
import { networkFailureAnswer, neverAnswer, problemAnswer, requestCount, requestsOf, type AnswerFactory } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const salary = salaryRecurringIncome
const flatRent = flatRentRecurringIncome
const tutoring = tutoringRecurringIncome
const three: RecurringIncome[] = [salary, flatRent, tutoring]
const listUrl = '/api/personal/recurring-incomes'

function monthlyOn(day: number): RecurringIncome {
  return {
    ...salary,
    id: `b0000000-0000-4000-8000-0000000000${String(day).padStart(2, '0')}`,
    title: `Day ${day}`,
    dayOfMonth: day,
    nextDate: null,
  }
}

function weeklyOn(dayOfWeek: number): RecurringIncome {
  return {
    ...tutoring,
    id: `c0000000-0000-4000-8000-00000000000${dayOfWeek}`,
    title: `Weekday ${dayOfWeek}`,
    dayOfWeek,
    nextDate: null,
  }
}

function failingThenAnswering(failure: AnswerFactory, success: AnswerFactory): AnswerFactory {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : success()
  }
}

function liveStopAnswers() {
  const live = liveRecurringList(three)
  return {
    [recurringIncomesGet]: live.answer,
    [recurringIncomeStopPost(salary.id)]: () => {
      live.set([flatRent, tutoring])
      return new Response(null, { status: 204 })
    },
    [recurringIncomeRestartPost(salary.id)]: () => {
      live.set(three)
      return new Response(null, { status: 204 })
    },
    [catchUpPost]: catchUpAnswer(1),
  }
}

async function findBlocks(language: 'en' | 'mk' = 'en'): Promise<HTMLElement[]> {
  const list = await screen.findByRole('list', { name: translated(language, 'budget.recurring.title') })
  return within(list).getAllByRole('listitem')
}

function newButton(language: 'en' | 'mk' = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'budget.recurring.newButton') })
}

describe('the repeating income screen', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('what it shows', () => {
    it.each(languages)('has the title "Repeating income" as the page heading and a Back link to the Budget tab (%s)', async (language) => {
      const { router } = await renderRepeatingScreen({ language, recurringIncomes: three })

      expect(await screen.findByRole('heading', { level: 1, name: translated(language, 'budget.recurring.title') })).toBeTruthy()
      const back = screen.getByRole('link', { name: translated(language, 'common.back') })
      expect(back.getAttribute('href')).toBe(routes.budget)
      expect(router.state.location.pathname).toBe(routes.budgetRepeating)
    })

    it('shows one block for each income, with the name, the amount, the rhythm and the next date', async () => {
      await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()

      const [salaryBlock, rentBlock, tutoringBlock] = blocksIn(document.body)

      expect(within(salaryBlock).getByText('Salary')).toBeTruthy()
      expect(within(salaryBlock).getByText(signedMoneyText(3000000, 'MKD'), { normalizer: plainSpaces }).classList.contains('text-positive')).toBe(true)
      expect(within(salaryBlock).getByText('Every month on the 1st')).toBeTruthy()
      expect(within(salaryBlock).getByText('next 1 Nov')).toBeTruthy()
      expect(within(rentBlock).getByText(signedMoneyText(25000, 'EUR'), { normalizer: plainSpaces })).toBeTruthy()
      expect(within(rentBlock).getByText('Every month on the 5th')).toBeTruthy()
      expect(within(tutoringBlock).getByText('Every week on Tuesday')).toBeTruthy()
      expect(within(tutoringBlock).getByText('next 20 Oct')).toBeTruthy()
    })

    it('says "Income" for an income with no name and leaves out "next" when there is none', async () => {
      await renderRepeatingScreen({ recurringIncomes: [untitledRecurringIncome, salary, flatRent] })
      await findBlocks()

      const block = blocksIn(document.body)[0]

      expect(within(block).getByText(translated('en', 'budget.income.listTitle'))).toBeTruthy()
      expect(within(block).queryByText(/^next/)).toBeNull()
    })

    it.each([
      [1, '1st'],
      [2, '2nd'],
      [3, '3rd'],
      [4, '4th'],
      [11, '11th'],
      [12, '12th'],
      [13, '13th'],
      [21, '21st'],
      [22, '22nd'],
      [23, '23rd'],
      [31, '31st'],
    ])('writes the day %i of the month as the %s', async (day, ordinal) => {
      await renderRepeatingScreen({ recurringIncomes: [monthlyOn(day)] })
      await findBlocks()

      expect(within(blocksIn(document.body)[0]).getByText(`Every month on the ${ordinal}`)).toBeTruthy()
    })

    it.each([
      [0, 'Sunday'],
      [1, 'Monday'],
      [2, 'Tuesday'],
      [3, 'Wednesday'],
      [4, 'Thursday'],
      [5, 'Friday'],
      [6, 'Saturday'],
    ])('writes the weekday %i as %s', async (dayOfWeek, name) => {
      await renderRepeatingScreen({ recurringIncomes: [weeklyOn(dayOfWeek)] })
      await findBlocks()

      expect(within(blocksIn(document.body)[0]).getByText(`Every week on ${name}`)).toBeTruthy()
    })

    it.each(languages)('writes the rhythm and the next date in %s from the tables of the app', async (language) => {
      await renderRepeatingScreen({ language, recurringIncomes: [tutoring, salary, flatRent] })
      await findBlocks(language)

      const block = blocksIn(document.body, language)[0]

      expect(within(block).getByText(translated(language, 'budget.recurring.weekly', { weekday: weekdayName(2, language) }))).toBeTruthy()
      expect(
        within(block).getByText(translated(language, 'budget.recurring.next', { day: language === 'en' ? '20 Oct' : '20 окт' })),
      ).toBeTruthy()
    })

    it('shows Edit and Stop in every block', async () => {
      await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()

      for (const block of blocksIn(document.body)) {
        expect(blockButton(block, 'expense.edit')).toBeTruthy()
        expect(blockButton(block, 'budget.recurring.stop')).toBeTruthy()
      }
    })

    it('shows the "+ New repeating income" button when there are incomes too', async () => {
      await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()

      expect(newButton()).toBeTruthy()
    })

    it('asks only for the list, and sends no catch-up of its own', async () => {
      const { fetchMock } = await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()
      await new Promise((resolve) => setTimeout(resolve, 50))

      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(1)
      expect(requestCount(fetchMock, 'POST', `${listUrl}/catch-up`)).toBe(0)
    })
  })

  describe('with no repeating income yet', () => {
    it.each(languages)('shows the empty text and the "+ New repeating income" button, with no list (%s)', async (language) => {
      await renderRepeatingScreen({ language, recurringIncomes: [] })

      expect(await screen.findByText(translated(language, 'budget.recurring.empty'))).toBeTruthy()
      expect(newButton(language)).toBeTruthy()
      expect(screen.queryByRole('list', { name: translated(language, 'budget.recurring.title') })).toBeNull()
    })
  })

  describe('loading and failing', () => {
    it('shows the loading state while the list is on its way, and the title and Back link already', async () => {
      await renderRepeatingScreen({ answers: { [recurringIncomesGet]: neverAnswer() } })

      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'budget.recurring.title') })).toBeTruthy()
      expect(screen.getByRole('status').textContent).toContain(translated('en', 'common.loading'))
      expect(screen.getByRole('link', { name: translated('en', 'common.back') })).toBeTruthy()
      expect(screen.queryByText(translated('en', 'budget.recurring.empty'))).toBeNull()
    })

    it.each(languages)('shows the translated message with Try again when the list cannot be loaded (%s)', async (language) => {
      await renderRepeatingScreen({ language, answers: { [recurringIncomesGet]: networkFailureAnswer() } })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated(language, 'errors.network'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
    })

    it('asks again and shows the blocks when Try again is pressed', async () => {
      const { fetchMock } = await renderRepeatingScreen({
        answers: { [recurringIncomesGet]: failingThenAnswering(networkFailureAnswer(), recurringListAnswer(three)) },
      })
      const alert = await screen.findByRole('alert')

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(3)
      })
      expect(requestCount(fetchMock, 'GET', listUrl)).toBe(2)
    })
  })

  describe('"+ New repeating income"', () => {
    const createAnswers = () => {
      const live = liveRecurringList([])
      return {
        [recurringIncomesGet]: live.answer,
        [recurringIncomesPost]: () => {
          live.set([salary])
          return Response.json(recurringIncomeAnswerRow(salary))
        },
        [catchUpPost]: catchUpAnswer(0),
      }
    }

    it('opens the income sheet with Repeats already on Monthly, an Ends chip and the hint', async () => {
      await renderRepeatingScreen({ recurringIncomes: [] })
      await screen.findByText(translated('en', 'budget.recurring.empty'))

      fireEvent.click(newButton())

      const sheet = await findIncomeSheet()
      expect(repeatsChipIn(sheet, 'Monthly')).toBeTruthy()
      expect(endsChip(sheet, null)).toBeTruthy()
      expect(within(sheet).getByText('Every month on the 19th')).toBeTruthy()
    })

    it('saves a monthly repeating income in 1 tap and 1 field after the button: Save', async () => {
      const { fetchMock } = await renderRepeatingScreen({ answers: createAnswers() })
      await screen.findByText(translated('en', 'budget.recurring.empty'))
      let taps = 0

      fireEvent.click(newButton())
      taps += 1
      await findIncomeSheet()
      typeAmount('30000')
      fireEvent.click(saveButton())
      taps += 1

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'POST', listUrl)).toHaveLength(1)
      })
      expect(taps).toBe(2)
      expect(sentBody(fetchMock, 'POST', listUrl)).toMatchObject({
        frequency: 'Monthly',
        dayOfMonth: 19,
        dayOfWeek: null,
        startsOn: '2026-10-19',
        endsOn: null,
        amountMinor: 3000000,
      })
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([`POST ${listUrl}`, `POST ${listUrl}/catch-up`])
    })

    it('shows the new block and no empty text after the save', async () => {
      await renderRepeatingScreen({ answers: createAnswers() })
      await screen.findByText(translated('en', 'budget.recurring.empty'))
      fireEvent.click(newButton())
      await findIncomeSheet()

      typeAmount('30000')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(1)
      })
      expect(within(blocksIn(document.body)[0]).getByText('Salary')).toBeTruthy()
      expect(screen.queryByText(translated('en', 'budget.recurring.empty'))).toBeNull()
    })
  })

  describe('Edit', () => {
    it('opens the income sheet filled in with that income, in 1 tap', async () => {
      await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()

      fireEvent.click(blockButton(blocksIn(document.body)[1], 'expense.edit'))

      const sheet = await findIncomeSheet()
      expect(amountField().value).toBe('250')
      expect(within(sheet).getByLabelText<HTMLTextAreaElement>(translated('en', 'expense.note')).value).toBe('Flat rent')
      expect(repeatsChipIn(sheet, 'Monthly')).toBeTruthy()
    })
  })

  describe('Stop', () => {
    it('stops that income, shows "Stopped" with Undo, and the block goes away', async () => {
      const { fetchMock } = await renderRepeatingScreen({ answers: liveStopAnswers() })
      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(3)
      })

      fireEvent.click(blockButton(blocksIn(document.body)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(2)
      })
      expect(requestCount(fetchMock, 'POST', `${recurringIncomeUrl(salary.id)}/stop`)).toBe(1)
      expect(toastShownWithText(translated('en', 'budget.recurring.stopped')).options.action).toMatchObject({
        label: translated('en', 'common.undo'),
      })
      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it('brings the block back with Undo', async () => {
      const { fetchMock } = await renderRepeatingScreen({ answers: liveStopAnswers() })
      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(3)
      })
      fireEvent.click(blockButton(blocksIn(document.body)[0], 'budget.recurring.stop'))
      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(2)
      })

      pressToastAction(toastShownWithText(translated('en', 'budget.recurring.stopped')))

      await waitFor(() => {
        expect(blocksIn(document.body)).toHaveLength(3)
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'budget.entries.restored'))
      expect(sentWritesToRecurringIncomes(fetchMock)).toEqual([
        `POST ${recurringIncomeUrl(salary.id)}/stop`,
        `POST ${recurringIncomeUrl(salary.id)}/restart`,
        `POST ${listUrl}/catch-up`,
      ])
    })

    it('shows the error as a toast and keeps the blocks when the stop is refused', async () => {
      await renderRepeatingScreen({
        recurringIncomes: three,
        answers: { [recurringIncomeStopPost(salary.id)]: problemAnswer(403, 'GUEST_NOT_ALLOWED') },
      })
      await findBlocks()

      fireEvent.click(blockButton(blocksIn(document.body)[0], 'budget.recurring.stop'))

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.GUEST_NOT_ALLOWED'))
      })
      expect(blocksIn(document.body)).toHaveLength(3)
    })
  })

  describe('Back', () => {
    it('goes back to the Budget tab', async () => {
      const { router } = await renderRepeatingScreen({ recurringIncomes: three })
      await findBlocks()

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'nav.budget') })).toBeTruthy()
    })
  })
})
