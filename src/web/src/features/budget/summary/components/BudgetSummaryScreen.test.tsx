import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { money, readableText } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, monthAnswers } from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { personalCategoriesWithArchivedGet } from '@/test/personalCategoryTestHelpers'
import { personalCategoriesWithArchived, personalFoodCategory } from '@/test/personalCategoryTestData'
import { octoberMonth } from '@/test/personalEntryTestData'
import {
  emptyOctoberSummary,
  emptyOctoberSummaryWithZeroConvertedTotals,
  incomeOnlySummary,
  januarySummary,
  mkdSummary,
  novemberSummary,
  octoberSummary,
  octoberSummaryOfDenarsOnly,
  spendingOnlyThisMonthSummary,
} from '@/test/personalSummaryTestData'
import {
  amountReadout,
  categoryNamesIn,
  categoryRows,
  currencyCardName,
  emojiOf,
  findCurrencyCard,
  findSummaryHeading,
  nameOf,
  personalSummaryGet,
  previousAmountOf,
  queryCategoryList,
  queryCurrencyCard,
  queryTotalRow,
  renderSummaryScreen,
  summaryPath,
  summaryTitle,
  thisAmountOf,
  totalRow,
} from '@/test/personalSummaryTestHelpers'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { networkFailureAnswer, neverAnswer, problemAnswer, requestCount, type AnswerFactory } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const categoriesUrl = '/api/personal/categories?includeArchived=true'

function failingThenAnswering(failure: AnswerFactory, success: AnswerFactory): AnswerFactory {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : success()
  }
}

describe('the monthly summary screen', () => {
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

  describe('the header', () => {
    it.each(languages)('has the two months as the page heading and a Back link to the Budget tab (%s)', async (language) => {
      const { router } = await renderSummaryScreen({ language })

      expect(await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10', language))).toBeTruthy()
      const back = screen.getByRole('link', { name: translated(language, 'common.back') })
      expect(back.getAttribute('href')).toBe(routes.budget)
      expect(router.state.location.pathname).toBe(routes.budgetSummary)
    })

    it('writes the heading as "Oct vs Sep" with the short month names of the month strip', async () => {
      await renderSummaryScreen()

      const heading = await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))

      expect(heading.textContent).toBe('Oct vs Sep')
    })

    it('adds the year to the months that are not in the current year, across the new year', async () => {
      await renderSummaryScreen({ path: summaryPath('2026-01'), summaries: [januarySummary] })

      const heading = await findSummaryHeading(summaryTitle('2026-01', '2025-12', '2026-10'))

      expect(heading.textContent).toBe('Jan 2026 vs Dec 2025')
    })

    it('has no month picker and no button: the month comes from the address', async () => {
      await renderSummaryScreen({ summaries: [octoberSummaryOfDenarsOnly] })
      await findCurrencyCard('MKD')

      expect(screen.queryByRole('group', { name: translated('en', 'budget.screen.months') })).toBeNull()
      expect(within(screen.getByRole('main')).queryAllByRole('button')).toEqual([])
    })

    it('uses the narrow 40rem column of the Budget tab on a computer, with one card under the other', async () => {
      await renderSummaryScreen()
      const mkdCard = await findCurrencyCard('MKD')

      const mainClasses = Array.from(screen.getByRole('main').classList)

      expect(mainClasses).toContain('lg:max-w-[40rem]')
      expect(mainClasses).not.toContain('lg:max-w-[64rem]')
      expect(Array.from(mkdCard.classList).filter((name) => name.startsWith('lg:'))).toEqual([])
    })
  })

  describe('the month', () => {
    it('asks for the month in the address and for nothing else about the person money', async () => {
      const { fetchMock } = await renderSummaryScreen({ path: summaryPath('2026-10') })
      await findCurrencyCard('MKD')

      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(1)
      expect(requestCount(fetchMock, 'GET', '/api/personal/entries?month=2026-10')).toBe(0)
    })

    it('works when opened directly by an address with another month', async () => {
      const { fetchMock } = await renderSummaryScreen({ path: summaryPath('2026-01'), summaries: [januarySummary] })

      await findSummaryHeading(summaryTitle('2026-01', '2025-12', '2026-10'))
      await findCurrencyCard('MKD')

      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-01')).toBe(1)
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(0)
    })

    it('opens for the current month when the address has no month', async () => {
      const { fetchMock } = await renderSummaryScreen({ path: routes.budgetSummary })

      expect(await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))).toBeTruthy()
      await findCurrencyCard('MKD')
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(1)
    })

    it('takes the current month in the time zone of the person, not in UTC', async () => {
      vi.setSystemTime(new Date('2026-10-31T23:30:00Z'))

      const { fetchMock } = await renderSummaryScreen({
        path: routes.budgetSummary,
        timeZone: 'Europe/Skopje',
        summaries: [novemberSummary],
      })

      expect(await findSummaryHeading(summaryTitle('2026-11', '2026-10', '2026-11'))).toBeTruthy()
      await findCurrencyCard('MKD')
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-11')).toBe(1)
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(0)
    })

    it('shows the title and the Back link at once for a month in the address, before the answer', async () => {
      await renderSummaryScreen({
        path: summaryPath('2026-01'),
        answers: { [personalSummaryGet('2026-01')]: neverAnswer() },
      })

      expect(await findSummaryHeading(summaryTitle('2026-01', '2025-12', '2026-10'))).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('en', 'common.back') })).toBeTruthy()
    })

    it('fails with the screen error and sends no request when the month in the address is not a month', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})

      const { fetchMock } = await renderSummaryScreen({ path: `${routes.budgetSummary}?month=october` })

      expect(await screen.findByRole('alert')).toBeTruthy()
      expect(screen.getByText(translated('en', 'errors.generic'))).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=october')).toBe(0)
    })
  })

  describe('one card for each currency', () => {
    it('shows the MKD card first and the EUR card after it, each as a region named after its currency', async () => {
      await renderSummaryScreen()

      const mkdCard = await findCurrencyCard('MKD')
      const eurCard = await findCurrencyCard('EUR')

      expect(isBefore(mkdCard, eurCard)).toBe(true)
      expect(screen.getAllByRole('region')).toHaveLength(2)
    })

    it.each(languages)('names the cards with the currency label of the app, "MKD" or "ден." and "EUR" (%s)', async (language) => {
      await renderSummaryScreen({ language })

      for (const currency of ['MKD', 'EUR'] as const) {
        const card = await findCurrencyCard(currency, language)
        expect(within(card).getByRole('heading', { level: 2, name: currencyCardName(currency, language) })).toBeTruthy()
      }
    })

    it('shows only the MKD card when there is nothing in euros in either month', async () => {
      await renderSummaryScreen({ summaries: [octoberSummaryOfDenarsOnly] })
      await findCurrencyCard('MKD')

      expect(queryCurrencyCard('EUR')).toBeNull()
    })

    it('never adds denars and euros together: no card holds an amount of the other currency', async () => {
      await renderSummaryScreen()

      const mkdCard = await findCurrencyCard('MKD')
      const eurCard = await findCurrencyCard('EUR')

      expect(readableText(mkdCard)).not.toContain('€')
      expect(readableText(eurCard)).not.toContain('MKD')
    })

    it.each(languages)('shows Spent with this month and the previous month, in the money format of the app (%s)', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)

      const row = totalRow(card, 'spending')

      expect(within(row).getByText(translated(language, 'budget.screen.spent'))).toBeTruthy()
      expect(amountReadout(thisAmountOf(row))).toBe(money(mkdSummary.spendingThisMinor, 'MKD', language))
      expect(amountReadout(previousAmountOf(row))).toBe(money(mkdSummary.spendingPreviousMinor, 'MKD', language))
      expect(readableText(previousAmountOf(row))).toContain(translated(language, 'budget.summary.was'))
    })

    it.each(languages)('shows Income with this month and the previous month (%s)', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)

      const row = totalRow(card, 'income')

      expect(within(row).getByText(translated(language, 'budget.screen.income'))).toBeTruthy()
      expect(amountReadout(thisAmountOf(row))).toBe(money(mkdSummary.incomeThisMinor, 'MKD', language))
      expect(amountReadout(previousAmountOf(row))).toBe(money(mkdSummary.incomePreviousMinor, 'MKD', language))
    })

    it('shows the euro card with its own amounts in euros', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('EUR')

      expect(amountReadout(thisAmountOf(totalRow(card, 'spending')))).toBe(money(12500, 'EUR'))
      expect(amountReadout(previousAmountOf(totalRow(card, 'spending')))).toBe(money(10000, 'EUR'))
      expect(amountReadout(thisAmountOf(totalRow(card, 'income')))).toBe(money(0, 'EUR'))
      expect(amountReadout(previousAmountOf(totalRow(card, 'income')))).toBe(money(20000, 'EUR'))
    })

    it('shows Spent before Income in a card', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      expect(isBefore(totalRow(card, 'spending'), totalRow(card, 'income'))).toBe(true)
    })

    it('leaves out the Income row when income is 0 in both months', async () => {
      await renderSummaryScreen({ summaries: [spendingOnlyThisMonthSummary] })
      const card = await findCurrencyCard('MKD')

      expect(totalRow(card, 'spending')).toBeTruthy()
      expect(queryTotalRow(card, 'income')).toBeNull()
      expect(within(card).queryByText(translated('en', 'budget.screen.income'))).toBeNull()
    })

    it('leaves out the Spent row and the category list when there is no spending in either month', async () => {
      await renderSummaryScreen({ summaries: [incomeOnlySummary] })
      const card = await findCurrencyCard('MKD')

      expect(totalRow(card, 'income')).toBeTruthy()
      expect(queryTotalRow(card, 'spending')).toBeNull()
      expect(queryCategoryList(card)).toBeNull()
    })
  })

  describe('the categories', () => {
    it.each(languages)('lists every category row with its emoji and name, in a list named "Totals by category" (%s)', async (language) => {
      await renderSummaryScreen({ language })
      const card = await findCurrencyCard('MKD', language)

      const rows = categoryRows(card, language)

      expect(rows).toHaveLength(5)
      expect(rows.map((row) => emojiOf(row).textContent)).toEqual(['\u{2615}', '\u{1F37D}\u{FE0F}', '\u{1F6D2}', '\u{1F3CB}\u{FE0F}', '\u{1F436}'])
    })

    it('hides the emoji from a screen reader', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      for (const row of categoryRows(card)) {
        expect(emojiOf(row).getAttribute('aria-hidden')).toBe('true')
      }
    })

    it('sorts the rows by this month, the highest first, and keeps the order of the server for equal amounts', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      expect(categoryNamesIn(card)).toEqual(
        [
          personalCategoryLabel('en', personalCategoriesWithArchived[2]),
          personalCategoryLabel('en', personalCategoriesWithArchived[0]),
          personalCategoryLabel('en', personalCategoriesWithArchived[1]),
          personalCategoryLabel('en', personalCategoriesWithArchived[4]),
          personalCategoryLabel('en', personalCategoriesWithArchived[3]),
        ],
      )
    })

    it('shows this month and the previous month of each category', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const [coffee, food, groceries, gym, dog] = categoryRows(card)

      expect(amountReadout(thisAmountOf(coffee))).toBe(money(900000, 'MKD'))
      expect(amountReadout(previousAmountOf(coffee))).toBe(money(0, 'MKD'))
      expect(amountReadout(thisAmountOf(food))).toBe(money(720000, 'MKD'))
      expect(amountReadout(previousAmountOf(food))).toBe(money(758000, 'MKD'))
      expect(amountReadout(thisAmountOf(groceries))).toBe(money(650000, 'MKD'))
      expect(amountReadout(previousAmountOf(groceries))).toBe(money(650000, 'MKD'))
      expect(amountReadout(thisAmountOf(gym))).toBe(money(650000, 'MKD'))
      expect(amountReadout(previousAmountOf(gym))).toBe(money(100000, 'MKD'))
      expect(amountReadout(thisAmountOf(dog))).toBe(money(0, 'MKD'))
      expect(amountReadout(previousAmountOf(dog))).toBe(money(239000, 'MKD'))
    })

    it('shows a category that was only in the previous month last, with 0 for this month and no extra word', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      const rows = categoryRows(card)
      const dog = rows[rows.length - 1]

      expect(nameOf(dog).textContent).toBe('Dog')
      expect(amountReadout(thisAmountOf(dog))).toBe(money(0, 'MKD'))
      expect(readableText(dog)).not.toMatch(/gone/i)
    })

    it('names an archived category with its own name', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('MKD')

      expect(categoryNamesIn(card)).toContain('Gym')
    })

    it('shows the rows of the euro card from the euro rows only', async () => {
      await renderSummaryScreen()
      const card = await findCurrencyCard('EUR')

      const rows = categoryRows(card)

      expect(rows).toHaveLength(1)
      expect(amountReadout(thisAmountOf(rows[0]))).toBe(money(12500, 'EUR'))
      expect(amountReadout(previousAmountOf(rows[0]))).toBe(money(10000, 'EUR'))
    })

    it('draws no list when a currency has no category rows', async () => {
      await renderSummaryScreen({ summaries: [novemberSummary], path: summaryPath('2026-11') })
      const card = await findCurrencyCard('MKD')

      expect(queryCategoryList(card)).toBeNull()
    })

    it('asks for the category list that includes archived categories, once', async () => {
      const { fetchMock } = await renderSummaryScreen()
      await findCurrencyCard('MKD')

      expect(requestCount(fetchMock, 'GET', categoriesUrl)).toBe(1)
    })

    it('never asks for the category list when no currency has a category row', async () => {
      const { fetchMock } = await renderSummaryScreen({ path: summaryPath('2026-11'), summaries: [novemberSummary] })
      await findCurrencyCard('MKD')

      expect(requestCount(fetchMock, 'GET', categoriesUrl)).toBe(0)
    })

    it('fails with the screen error, naming the category, when a row has a category that is not in the list', async () => {
      const errors = vi.spyOn(console, 'error').mockImplementation(() => {})

      await renderSummaryScreen({ categories: [personalFoodCategory] })

      expect(await screen.findByText(translated('en', 'errors.generic'))).toBeTruthy()
      const missingIds = personalCategoriesWithArchived.slice(1).map((category) => category.id)
      const named = errors.mock.calls.some((call) =>
        call.some(
          (argument) => argument instanceof Error && missingIds.some((missingId) => argument.message.includes(missingId)),
        ),
      )
      expect(named).toBe(true)
    })
  })

  describe('with nothing to compare', () => {
    it.each(languages)('shows the empty text and a link to the Budget tab, with no card (%s)', async (language) => {
      await renderSummaryScreen({ language, summaries: [emptyOctoberSummary] })

      expect(await screen.findByText(translated(language, 'budget.summary.empty'))).toBeTruthy()
      const toBudget = screen.getByRole('link', { name: translated(language, 'budget.summary.toBudget') })
      expect(toBudget.getAttribute('href')).toBe(routes.budget)
      expect(screen.queryAllByRole('region')).toEqual([])
    })

    it('keeps the title and the Back link in the empty state', async () => {
      await renderSummaryScreen({ summaries: [emptyOctoberSummary] })

      expect(await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('en', 'common.back') })).toBeTruthy()
    })

    it('goes to the Budget tab with one tap on the link', async () => {
      const { router } = await renderSummaryScreen({ summaries: [emptyOctoberSummary] })
      await screen.findByText(translated('en', 'budget.summary.empty'))

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'budget.summary.toBudget') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
    })

    it('shows the empty state, and no converted card, when the setting is on and the converted totals are all zeros', async () => {
      await renderSummaryScreen({
        budgetTotalCurrency: 'MKD',
        summaries: [emptyOctoberSummaryWithZeroConvertedTotals],
      })

      expect(await screen.findByText(translated('en', 'budget.summary.empty'))).toBeTruthy()
      expect(screen.queryAllByRole('region')).toEqual([])
    })

    it('does not ask for the category list', async () => {
      const { fetchMock } = await renderSummaryScreen({ summaries: [emptyOctoberSummary] })
      await screen.findByText(translated('en', 'budget.summary.empty'))

      expect(requestCount(fetchMock, 'GET', categoriesUrl)).toBe(0)
    })
  })

  describe('loading and failing', () => {
    it('shows skeletons in a status while the summary is on its way, with the title and Back link already', async () => {
      await renderSummaryScreen({ answers: { [personalSummaryGet('2026-10')]: neverAnswer() } })

      expect(await findSummaryHeading(summaryTitle('2026-10', '2026-09', '2026-10'))).toBeTruthy()
      const status = screen.getByRole('status')
      expect(status.textContent).toContain(translated('en', 'common.loading'))
      expect(status.querySelectorAll('[data-skeleton]').length).toBeGreaterThanOrEqual(3)
      expect(status.querySelector('.animate-spin')).toBeNull()
      expect(screen.getByRole('link', { name: translated('en', 'common.back') })).toBeTruthy()
      expect(screen.queryByText(translated('en', 'budget.summary.empty'))).toBeNull()
      expect(screen.queryAllByRole('region')).toEqual([])
    })

    it('shows skeletons, not the cards, while only the category list is on its way', async () => {
      await renderSummaryScreen({ answers: { [personalCategoriesWithArchivedGet]: neverAnswer() } })

      await waitFor(() => {
        expect(screen.getByRole('status')).toBeTruthy()
      })
      expect(screen.queryAllByRole('region')).toEqual([])
    })

    it.each(languages)('shows the translated message with Try again when the summary cannot be loaded (%s)', async (language) => {
      await renderSummaryScreen({ language, answers: { [personalSummaryGet('2026-10')]: networkFailureAnswer() } })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated(language, 'errors.network'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
      expect(screen.getByRole('link', { name: translated(language, 'common.back') })).toBeTruthy()
    })

    it('shows the text of the server code when the month is refused', async () => {
      await renderSummaryScreen({
        answers: { [personalSummaryGet('2026-10')]: problemAnswer(400, 'PERSONAL_ENTRY_MONTH_INVALID') },
      })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.PERSONAL_ENTRY_MONTH_INVALID'))).toBeTruthy()
    })

    it('shows the generic message when the answer cannot be read', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderSummaryScreen({
        answers: { [personalSummaryGet('2026-10')]: () => Response.json({ ...octoberSummary, currencies: null }) },
      })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.generic'))).toBeTruthy()
    })

    it('asks again and shows the cards when Try again is pressed', async () => {
      const { fetchMock } = await renderSummaryScreen({
        answers: {
          [personalSummaryGet('2026-10')]: failingThenAnswering(networkFailureAnswer(), () => Response.json(octoberSummary)),
        },
      })
      const alert = await screen.findByRole('alert')

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findCurrencyCard('MKD')).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(2)
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows the error with Try again when the category list cannot be loaded, and asks only for the list again', async () => {
      const { fetchMock } = await renderSummaryScreen({
        answers: {
          [personalCategoriesWithArchivedGet]: failingThenAnswering(networkFailureAnswer(), () =>
            Response.json({ categories: personalCategoriesWithArchived }),
          ),
        },
      })
      const alert = await screen.findByRole('alert')
      expect(within(alert).getByText(translated('en', 'errors.network'))).toBeTruthy()

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findCurrencyCard('MKD')).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', categoriesUrl)).toBe(2)
      expect(requestCount(fetchMock, 'GET', '/api/personal/summary?month=2026-10')).toBe(1)
    })
  })

  describe('Back', () => {
    it('goes back to the Budget tab', async () => {
      const { router } = await renderSummaryScreen({ answers: monthAnswers([octoberMonth]) })
      await findCurrencyCard('MKD')

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.budget)
      })
    })
  })
})
