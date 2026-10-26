import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { formatMonthLabel } from '@/shared/utils/calendarMonth'
import { currencyLabel } from '@/shared/utils/formatMoney'
import {
  categoryLimitDelete,
  categoryLimitPut,
  findLimitSheet,
  findLimitsCard,
  limitAmountField,
  limitRowButton,
  limitRowNamed,
  limitRowText,
  limitRows,
  limitSheetTitle,
  monthLimitDelete,
  monthLimitPut,
  monthThatChanges,
  openLimitSheetFromRow,
  queryLimitsCard,
  queryRemoveLimitButton,
  querySetLimitButton,
  removeLimitButton,
  renderBudgetWithLimits,
  saveLimitButton,
  setLimitButtonInCard,
  typeLimit,
  wholeMonthName,
} from '@/test/budgetLimitTestHelpers'
import {
  foodLimit,
  groceriesLimitWithEuros,
  limitRow,
  octoberWithLimits,
  octoberWithOnlyACoffeeLimit,
  petLimitExactlyReached,
  septemberWithLimits,
  wholeMonthLimit,
} from '@/test/budgetLimitTestData'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, monthButton, personalEntriesGet } from '@/test/budgetTestHelpers'
import { dismissSheetByTappingBackdrop } from '@/test/expenseFormTestHelpers'
import { octoberMonth } from '@/test/personalEntryTestData'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import {
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { expectSaveOutsideScrollingFields, expectTitleOutsideScrollingBody } from '@/test/sheetScrollTestHelpers'
import { referenceDangerToastStyling, stylingOf, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function coffeePut(): string {
  return categoryLimitPut(coffeeCategory.id)
}

function coffeeDelete(): string {
  return categoryLimitDelete(coffeeCategory.id)
}

const coffeeUrl = `/api/personal/limits/categories/${coffeeCategory.id}`

const octoberWithCoffeeRaised = {
  ...octoberWithLimits,
  limits: {
    month: wholeMonthLimit,
    categories: [foodLimit, groceriesLimitWithEuros, limitRow(coffeeCategory.id, 1200000, 480000), petLimitExactlyReached],
  },
}

const octoberWithoutCoffee = {
  ...octoberWithLimits,
  limits: { month: wholeMonthLimit, categories: [foodLimit, groceriesLimitWithEuros, petLimitExactlyReached] },
}

const octoberWithMonthLimitRaised = {
  ...octoberWithLimits,
  limits: { ...octoberWithLimits.limits, month: limitRow(null, 3000000, 1750000) },
}

const octoberWithoutMonthLimit = { ...octoberWithLimits, limits: { ...octoberWithLimits.limits, month: null } }

interface SheetOptions {
  language?: Language
  answers?: Record<string, AnswerFactory>
}

async function renderCoffeeSheet(options: SheetOptions = {}) {
  const language = options.language ?? 'en'
  const rendered = await renderBudgetWithLimits({ language, months: [octoberWithLimits], answers: options.answers })
  const card = await findLimitsCard(language)
  const row = limitRowNamed(card, 'Coffee')
  const rowButton = limitRowButton(row)
  const sheet = await openLimitSheetFromRow(card, 'Coffee', language)
  return { ...rendered, card, row, rowButton, sheet }
}

async function openSeptember(): Promise<void> {
  fireEvent.click(monthButton('Sep'))
  await waitFor(() => {
    expect(queryLimitsCard()?.textContent).toContain('Gym')
  })
}

async function openWholeMonthSheetFromSeptember(answers: Record<string, AnswerFactory> = {}) {
  const rendered = await renderBudgetWithLimits({ months: [octoberWithLimits, septemberWithLimits], answers })
  await findLimitsCard()
  await openSeptember()
  const sheet = await openLimitSheetFromRow(queryLimitsCard() ?? document.body, wholeMonthName())
  return { ...rendered, sheet }
}

function successToastTexts(): unknown[] {
  return vi.mocked(toast.success).mock.calls.map(([text]) => text)
}

function errorToastTexts(): unknown[] {
  return vi.mocked(toast.error).mock.calls.map(([text]) => text)
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('BudgetScreen, the set-a-limit sheet', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('what it shows', () => {
    it.each(languages)('is titled with the category and "monthly limit" (%s)', async (language) => {
      const { sheet } = await renderCoffeeSheet({ language })

      expect(within(sheet).getByText(limitSheetTitle('Coffee', language))).toBeTruthy()
    })

    it.each(languages)('is titled with "Whole month" for the whole-month row (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits] })
      const card = await findLimitsCard(language)

      const sheet = await openLimitSheetFromRow(card, wholeMonthName(language), language)

      expect(within(sheet).getByText(limitSheetTitle(wholeMonthName(language), language))).toBeTruthy()
    })

    it('has the current limit of the row typed in, in whole denars', async () => {
      const { sheet } = await renderCoffeeSheet()

      expect(limitAmountField(sheet).value).toBe('4500')
    })

    it('has the current limit of the whole month typed in', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits] })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), wholeMonthName())

      expect(limitAmountField(sheet).value).toBe('25000')
    })

    it('puts the cursor in the amount field at once and asks for whole numbers', async () => {
      const { sheet } = await renderCoffeeSheet()

      await waitFor(() => {
        expect(document.activeElement).toBe(limitAmountField(sheet))
      })
      expect(limitAmountField(sheet).getAttribute('inputmode')).toBe('numeric')
    })

    it.each(languages)('shows MKD as fixed text and offers no currency switch (%s)', async (language) => {
      const { sheet } = await renderCoffeeSheet({ language })

      expect(sheet.textContent).toContain(currencyLabel('MKD', language))
      expect(within(sheet).queryAllByRole('button').map((button) => button.textContent)).toEqual([
        translated(language, 'groupSettings.save'),
        translated(language, 'budget.limits.remove'),
      ])
    })

    it.each(languages)('says that the limit applies from this month on and earlier months keep theirs (%s)', async (language) => {
      const { sheet } = await renderCoffeeSheet({ language })

      expect(within(sheet).getByText(translated(language, 'budget.limits.appliesFromNow'))).toBeTruthy()
    })

    it('says nothing about another month while the current month is looked at', async () => {
      const { sheet } = await renderCoffeeSheet()

      expect(within(sheet).queryByText(/looking at/i)).toBeNull()
    })

    it.each(languages)('adds the line about the month being looked at when it is an earlier month (%s)', async (language) => {
      await renderBudgetWithLimits({ language, months: [octoberWithLimits, septemberWithLimits] })
      await findLimitsCard(language)
      fireEvent.click(monthButton(formatMonthLabel('2026-09', '2026-10', language), language))
      await waitFor(() => {
        expect(queryLimitsCard(language)?.textContent).toContain('Gym')
      })

      const sheet = await openLimitSheetFromRow(queryLimitsCard(language) ?? document.body, wholeMonthName(language), language)

      expect(within(sheet).getByText(translated(language, 'budget.limits.appliesFromNow'))).toBeTruthy()
      expect(
        within(sheet).getByText(
          translated(language, 'budget.limits.viewingPast', { month: formatMonthLabel('2026-09', '2026-10', language) }),
        ),
      ).toBeTruthy()
    })

    it('shows the limit of the earlier month in the field when it is opened from an earlier month', async () => {
      await renderBudgetWithLimits({ months: [octoberWithLimits, septemberWithLimits] })
      await findLimitsCard()
      await openSeptember()

      const sheet = await openLimitSheetFromRow(queryLimitsCard() ?? document.body, wholeMonthName())

      expect(limitAmountField(sheet).value).toBe('20000')
    })

    it('keeps the title outside the scrolling part and Save outside the scrolling fields', async () => {
      const { sheet } = await renderCoffeeSheet()

      expectTitleOutsideScrollingBody(sheet, translated('en', 'budget.limits.sheetTitle', { name: 'Coffee' }))
      expectSaveOutsideScrollingFields(sheet, limitAmountField(sheet), saveLimitButton(sheet))
    })
  })

  describe('when Save can be tapped', () => {
    it('is disabled while the amount is the limit that exists', async () => {
      const { sheet } = await renderCoffeeSheet()

      expect(saveLimitButton(sheet).disabled).toBe(true)
    })

    it('is enabled once the amount is a different valid number of whole denars', async () => {
      const { sheet } = await renderCoffeeSheet()

      typeLimit(sheet, '5000')

      expect(saveLimitButton(sheet).disabled).toBe(false)
    })

    it.each(['4500', '04500'])('is disabled again when the amount is typed back to the same limit (%s)', async (sameAmount) => {
      const { sheet } = await renderCoffeeSheet()
      typeLimit(sheet, '5000')

      typeLimit(sheet, sameAmount)

      expect(saveLimitButton(sheet).disabled).toBe(true)
    })

    it.each(['', '0', '00', '12.5', '12,5', 'abc', '-5', '99999999999'])('is disabled for the amount "%s", which is not a valid number of whole denars above 0', async (text) => {
      const { sheet } = await renderCoffeeSheet()

      typeLimit(sheet, text)

      expect(saveLimitButton(sheet).disabled).toBe(true)
    })

    it('marks the field red for a typed amount with decimals and not for a whole one', async () => {
      const { sheet } = await renderCoffeeSheet()

      typeLimit(sheet, '12.5')
      const markedForDecimals = limitAmountField(sheet).getAttribute('aria-invalid')
      typeLimit(sheet, '12')

      expect(markedForDecimals).toBe('true')
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })
  })

  describe('when Save can be tapped from an earlier month', () => {
    it('is enabled at once, because the limit of this month is not known to the sheet', async () => {
      const { sheet } = await openWholeMonthSheetFromSeptember()

      expect(limitAmountField(sheet).value).toBe('20000')
      expect(saveLimitButton(sheet).disabled).toBe(false)
    })

    it('is enabled when the amount is typed back to the limit of the earlier month', async () => {
      const { sheet } = await openWholeMonthSheetFromSeptember()
      typeLimit(sheet, '21000')

      typeLimit(sheet, '20000')

      expect(saveLimitButton(sheet).disabled).toBe(false)
    })

    it('sends the limit of the earlier month as it is when Save is tapped without typing', async () => {
      const { fetchMock, sheet } = await openWholeMonthSheetFromSeptember({ [monthLimitPut]: () => new Response(null, { status: 204 }) })

      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)[0].body).toEqual({ limitMinor: 2000000 })
    })

    it.each(['', '0', '00', '12.5', '12,5', 'abc', '-5', '99999999999'])('is disabled for the amount "%s", which is not a valid number of whole denars above 0', async (text) => {
      const { sheet } = await openWholeMonthSheetFromSeptember()

      typeLimit(sheet, text)

      expect(saveLimitButton(sheet).disabled).toBe(true)
    })

    it('shows Remove limit, because the earlier month has a limit on that row', async () => {
      const { sheet } = await openWholeMonthSheetFromSeptember()

      expect(queryRemoveLimitButton(sheet)).toBeTruthy()
    })
  })

  describe('saving', () => {
    it('sends PUT /api/personal/limits/categories/{id} with the typed denars as deni and nothing else', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithCoffeeRaised)
      const { fetchMock, sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeePut()]: month.acceptChange },
      })

      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', coffeeUrl)[0].body).toEqual({ limitMinor: 1200000 })
    })

    it('sends PUT /api/personal/limits/month for the whole-month row', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithMonthLimitRaised)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [monthLimitPut]: month.acceptChange },
      })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), wholeMonthName())

      typeLimit(sheet, '30000')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)[0].body).toEqual({ limitMinor: 3000000 })
    })

    it('closes the sheet, shows "Saved", asks for the month again and shows the new limit on the row', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithCoffeeRaised)
      const { fetchMock, sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeePut()]: month.acceptChange },
      })

      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(successToastTexts()).toEqual([translated('en', 'groupSettings.saved')])
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(2)
      const row = limitRowNamed(await findLimitsCard(), 'Coffee')
      expect(limitRowText(row).amounts).toBe(`${money(480000, 'MKD')} / ${money(1200000, 'MKD')}`)
      expect(limitRowText(row).over).toBeNull()
    })

    it('brings the focus back to the row that was tapped', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithCoffeeRaised)
      const { sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeePut()]: month.acceptChange },
      })

      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await settle()

      expect(document.activeElement).toBe(limitRowButton(limitRowNamed(await findLimitsCard(), 'Coffee')))
    })

    it('saves from an earlier month with the same request, keeps that month selected and shows "Saved"', async () => {
      const month = monthThatChanges(septemberWithLimits, septemberWithLimits)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: {
          [personalEntriesGet('2026-09')]: month.monthAnswer,
          [monthLimitPut]: month.acceptChange,
        },
      })
      await findLimitsCard()
      await openSeptember()
      const sheet = await openLimitSheetFromRow(queryLimitsCard() ?? document.body, wholeMonthName())

      typeLimit(sheet, '21000')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)).toHaveLength(1)
      expect(requestsOf(fetchMock, 'PUT', endpoints.personalLimitsMonth)[0].body).toEqual({ limitMinor: 2100000 })
      expect(monthButton('Sep').getAttribute('aria-pressed')).toBe('true')
      expect(successToastTexts()).toEqual([translated('en', 'groupSettings.saved')])
    })

    it('sends one request when Save is tapped twice at once', async () => {
      const { fetchMock, sheet } = await renderCoffeeSheet({ answers: { [coffeePut()]: neverAnswer() } })
      typeLimit(sheet, '12000')

      fireEvent.click(saveLimitButton(sheet))
      fireEvent.click(saveLimitButton(sheet))

      await settle()
      expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(1)
    })

    it('disables Save and Remove limit while the request is out', async () => {
      const { sheet } = await renderCoffeeSheet({ answers: { [coffeePut()]: neverAnswer() } })
      typeLimit(sheet, '12000')

      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(saveLimitButton(sheet).disabled).toBe(true)
      })
      expect((removeLimitButton(sheet) as HTMLButtonElement).disabled).toBe(true)
    })

    it('is two taps and one typed amount from the row: the row, then Save', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithCoffeeRaised)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeePut()]: month.acceptChange },
      })
      const card = await findLimitsCard()
      let taps = 0

      const rowButton = limitRowButton(limitRowNamed(card, 'Coffee'))
      rowButton.focus()
      fireEvent.click(rowButton)
      taps += 1
      const sheet = await findLimitSheet('Coffee')
      await waitFor(() => {
        expect(document.activeElement).toBe(limitAmountField(sheet))
      })
      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))
      taps += 1

      expect(taps).toBe(2)
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(1)
      })
    })
  })

  describe('removing', () => {
    it('shows the Remove limit button only when the row has a limit, which every row on the card has', async () => {
      const { sheet } = await renderCoffeeSheet()

      expect(removeLimitButton(sheet)).toBeTruthy()
    })

    it('sends DELETE /api/personal/limits/categories/{id} at once, without a question', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithoutCoffee)
      const { fetchMock, sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeeDelete()]: month.acceptChange },
      })

      fireEvent.click(removeLimitButton(sheet))

      expect(screen.queryByRole('alertdialog')).toBeNull()
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', coffeeUrl)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'DELETE', coffeeUrl)[0].body).toBeUndefined()
    })

    it('sends DELETE /api/personal/limits/month for the whole-month row', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithoutMonthLimit)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [monthLimitDelete]: month.acceptChange },
      })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), wholeMonthName())

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', endpoints.personalLimitsMonth)).toHaveLength(1)
      })
    })

    it('closes the sheet, shows "Limit removed" as a red toast without Undo, and takes the row off the card', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithoutCoffee)
      const { sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeeDelete()]: month.acceptChange },
      })

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      const shown = await waitFor(() => toastShownWithText(translated('en', 'budget.limits.removed')))
      expect(shown.options.action).toBeUndefined()
      expect(stylingOf(shown)).toEqual(referenceDangerToastStyling())
      await waitFor(() => {
        expect(limitRows(screen.getByRole('region', { name: translated('en', 'budget.limits.title') })).map((row) => limitRowText(row).name)).not.toContain('Coffee')
      })
    })

    it('leaves only the "Set a limit" button when the last limit is removed', async () => {
      const month = monthThatChanges(octoberWithOnlyACoffeeLimit, octoberMonth)
      await renderBudgetWithLimits({
        months: [octoberWithOnlyACoffeeLimit],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeeDelete()]: month.acceptChange },
      })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), 'Coffee')

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(queryLimitsCard()).toBeNull()
      })
      expect(querySetLimitButton()).toBeTruthy()
    })

    it('brings the focus to the "Set a limit" button when the last limit is removed, because the row it came from is gone', async () => {
      const month = monthThatChanges(octoberWithOnlyACoffeeLimit, octoberMonth)
      await renderBudgetWithLimits({
        months: [octoberWithOnlyACoffeeLimit],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeeDelete()]: month.acceptChange },
      })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), 'Coffee')

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(querySetLimitButton())
      })
    })

    it('brings the focus to the "Set a limit" button of the card when a limit is removed and others stay', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithoutCoffee)
      const { sheet } = await renderCoffeeSheet({
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [coffeeDelete()]: month.acceptChange },
      })

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await waitFor(() => {
        expect(limitRows(queryLimitsCard() ?? document.body).map((row) => limitRowText(row).name)).not.toContain('Coffee')
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(setLimitButtonInCard())
      })
    })

    it('brings the focus to the "Set a limit" button of the card when the whole-month limit is removed and others stay', async () => {
      const month = monthThatChanges(octoberWithLimits, octoberWithoutMonthLimit)
      await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: { [personalEntriesGet('2026-10')]: month.monthAnswer, [monthLimitDelete]: month.acceptChange },
      })
      const sheet = await openLimitSheetFromRow(await findLimitsCard(), wholeMonthName())

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await waitFor(() => {
        expect(limitRows(queryLimitsCard() ?? document.body).map((row) => limitRowText(row).name)).not.toContain(wholeMonthName())
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(setLimitButtonInCard())
      })
    })

    it('removes from an earlier month with the same request and keeps that month selected', async () => {
      const month = monthThatChanges(septemberWithLimits, septemberWithLimits)
      const { fetchMock } = await renderBudgetWithLimits({
        months: [octoberWithLimits],
        answers: { [personalEntriesGet('2026-09')]: month.monthAnswer, [monthLimitDelete]: month.acceptChange },
      })
      await findLimitsCard()
      await openSeptember()
      const sheet = await openLimitSheetFromRow(queryLimitsCard() ?? document.body, wholeMonthName())

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', endpoints.personalLimitsMonth)).toHaveLength(1)
      })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(monthButton('Sep').getAttribute('aria-pressed')).toBe('true')
    })

    it('sends one request when Remove limit is tapped twice at once', async () => {
      const { fetchMock, sheet } = await renderCoffeeSheet({ answers: { [coffeeDelete()]: neverAnswer() } })

      fireEvent.click(removeLimitButton(sheet))
      fireEvent.click(removeLimitButton(sheet))

      await settle()
      expect(requestsOf(fetchMock, 'DELETE', coffeeUrl)).toHaveLength(1)
    })

    it.each(languages)('shows a failed removal as an error toast and keeps the sheet and the limit (%s)', async (language) => {
      const { fetchMock, sheet } = await renderCoffeeSheet({ language, answers: { [coffeeDelete()]: networkFailureAnswer() } })

      fireEvent.click(removeLimitButton(sheet, language))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated(language, 'errors.network')])
      })
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(1)
      expect(limitAmountField(sheet, language).value).toBe('4500')
    })

    it('lets Remove limit be tapped again after a failure', async () => {
      const { fetchMock, sheet } = await renderCoffeeSheet({ answers: { [coffeeDelete()]: networkFailureAnswer() } })
      fireEvent.click(removeLimitButton(sheet))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })

      fireEvent.click(removeLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', coffeeUrl)).toHaveLength(2)
      })
    })
  })

  describe('refused saves', () => {
    it.each(languages)('shows the translated message for BUDGET_LIMIT_AMOUNT_INVALID above Save, keeps the typed amount and the sheet, and marks the field red (%s)', async (language) => {
      const { sheet } = await renderCoffeeSheet({
        language,
        answers: { [coffeePut()]: problemAnswer(400, 'BUDGET_LIMIT_AMOUNT_INVALID') },
      })
      typeLimit(sheet, '12000', language)

      fireEvent.click(saveLimitButton(sheet, language))

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toBe(translated(language, 'errors.BUDGET_LIMIT_AMOUNT_INVALID'))
      expect(alert.compareDocumentPosition(saveLimitButton(sheet, language)) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
      expect(limitAmountField(sheet, language).value).toBe('12000')
      expect(limitAmountField(sheet, language).getAttribute('aria-invalid')).toBe('true')
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(errorToastTexts()).toEqual([])
    })

    it('takes the red edge off once the amount is changed and lets Save be tapped again', async () => {
      const { fetchMock, sheet } = await renderCoffeeSheet({
        answers: { [coffeePut()]: problemAnswer(400, 'BUDGET_LIMIT_AMOUNT_INVALID') },
      })
      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))
      await within(sheet).findByRole('alert')

      typeLimit(sheet, '13000')
      expect(limitAmountField(sheet).getAttribute('aria-invalid')).not.toBe('true')
      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(2)
      })
    })

    it.each(languages)('shows the network message when the server cannot be reached (%s)', async (language) => {
      const { sheet } = await renderCoffeeSheet({ language, answers: { [coffeePut()]: networkFailureAnswer() } })
      typeLimit(sheet, '12000', language)

      fireEvent.click(saveLimitButton(sheet, language))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated(language, 'errors.network'))
      expect(screen.getByRole('dialog')).toBe(sheet)
    })

    it('shows the translated message for GUEST_NOT_ALLOWED and keeps the sheet', async () => {
      const { sheet } = await renderCoffeeSheet({ answers: { [coffeePut()]: problemAnswer(403, 'GUEST_NOT_ALLOWED') } })
      typeLimit(sheet, '12000')

      fireEvent.click(saveLimitButton(sheet))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated('en', 'errors.GUEST_NOT_ALLOWED'))
    })

    it('answers a category that no longer exists with an error toast, asks for the month again and closes the sheet', async () => {
      const { fetchMock, sheet } = await renderCoffeeSheet({
        answers: { [coffeePut()]: problemAnswer(404, 'CATEGORY_NOT_FOUND') },
      })
      typeLimit(sheet, '12000')

      fireEvent.click(saveLimitButton(sheet))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated('en', 'errors.CATEGORY_NOT_FOUND')])
      })
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(requestCount(fetchMock, 'GET', endpoints.personalEntriesOfMonth('2026-10'))).toBe(2)
    })

    it('changes nothing on the card after a refusal', async () => {
      const { sheet } = await renderCoffeeSheet({
        answers: { [coffeePut()]: problemAnswer(400, 'BUDGET_LIMIT_AMOUNT_INVALID') },
      })
      typeLimit(sheet, '12000')
      fireEvent.click(saveLimitButton(sheet))
      await within(sheet).findByRole('alert')
      dismissSheetByTappingBackdrop()
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })

      const row = limitRowNamed(await findLimitsCard(), 'Coffee')

      expect(limitRowText(row).amounts).toBe(`${money(480000, 'MKD')} / ${money(450000, 'MKD')}`)
    })
  })

  describe('closing without saving', () => {
    it('sends nothing, closes the sheet and brings the focus back to the row', async () => {
      const { fetchMock, sheet, rowButton } = await renderCoffeeSheet()
      typeLimit(sheet, '12000')

      dismissSheetByTappingBackdrop()

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      await settle()
      expect(document.activeElement).toBe(rowButton)
      expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(0)
      expect(requestsOf(fetchMock, 'DELETE', coffeeUrl)).toHaveLength(0)
    })

    it('opens with the saved limit again, not the unsaved typing', async () => {
      const { sheet, rowButton } = await renderCoffeeSheet()
      typeLimit(sheet, '12000')
      dismissSheetByTappingBackdrop()
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })

      rowButton.focus()
      fireEvent.click(rowButton)
      const reopened = await findLimitSheet('Coffee')

      expect(limitAmountField(reopened).value).toBe('4500')
    })
  })
})
