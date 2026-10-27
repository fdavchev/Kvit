import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { categoriesPath, categoryName, freezeTime, isBefore, restoreRealRetryPolicy, unfreezeTime } from '@/test/expenseTestHelpers'
import {
  bigDenarAmountMinor,
  bigDenarSpending,
  eurOnlySpending,
  laterAnswerSpending,
  mkdAndEurSpending,
  mkdOnlySpending,
  noSpendingYet,
  onlyUncategorisedSpending,
  planFields,
  spendingPath,
  spendingWithPlan,
  unknownCategoryId,
  unknownCategorySpending,
} from '@/test/groupSpendingTestData'
import {
  amountElementIn,
  categoryRowsOf,
  currencyRegion,
  expectRows,
  findCurrencyRegion,
  hasClassOnItselfOrAncestorsBelow,
  minHeightStepOf,
  mkdOnlyRows,
  nameElementIn,
  pictographsIn,
  queryCurrencyRegion,
  totalAmountElements,
  totalsTitle,
} from '@/test/groupSpendingTestHelpers'
import { foodCategory, funCategory, groceriesCategory, testCategories, transportCategory } from '@/test/expenseTestData'
import { isCentred } from '@/test/groupEmptyStateTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { jsonAnswer, networkFailureAnswer, neverAnswer, problemAnswer, requestCount } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const crashMessage = 'A screen crashed while rendering'
const serverError = () => new Response(null, { status: 500 })

async function showsGroupScreen(): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: testGroup.name })
}

function crashErrors(consoleError: MockInstance<typeof console.error>): Error[] {
  return consoleError.mock.calls
    .filter(([message]) => message === crashMessage)
    .map(([, error]) => error)
    .filter((error): error is Error => error instanceof Error)
}

function noRegionIsShown(): void {
  expect(queryCurrencyRegion('MKD')).toBeNull()
  expect(queryCurrencyRegion('EUR')).toBeNull()
  expect(queryCurrencyRegion('MKD', 'mk')).toBeNull()
}

function panelOf(element: HTMLElement): HTMLElement {
  const panel = element.closest<HTMLElement>('[data-tab-panel]')
  if (panel === null) {
    throw new Error(`The element "${element.textContent}" is not inside the tab panel`)
  }
  return panel
}

describe('GroupScreen with the Totals tab: the totals by category', () => {
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

  describe('one currency', () => {
    it('shows one region named after the currency, with the currency total first and then the category rows in the order of the answer', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const region = await findCurrencyRegion('MKD')

      expect(totalAmountElements(region, 135000, 'MKD')).toHaveLength(1)
      expectRows(region, 'MKD', mkdOnlyRows())
      const list = within(region).getByRole('list')
      expect(isBefore(totalAmountElements(region, 135000, 'MKD')[0], list)).toBe(true)
    })

    it('shows the currency label as a visible heading inside its region', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const region = await findCurrencyRegion('MKD')
      const heading = within(region).getByRole('heading', { name: 'MKD' })

      expect(heading.classList.contains('sr-only')).toBe(false)
    })

    it('shows the title "Totals by category" as a level 2 heading', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })
      await findCurrencyRegion('MKD')

      expect(totalsTitle().textContent).toBe('Totals by category')
    })

    it('shows no region for a currency that is not in the answer', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })
      await findCurrencyRegion('MKD')

      expect(queryCurrencyRegion('EUR')).toBeNull()
    })

    it('shows the euro total and the euro rows in their own currency for a group that spent only euros', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: eurOnlySpending })

      const region = await findCurrencyRegion('EUR')

      expect(totalAmountElements(region, 8250, 'EUR')).toHaveLength(1)
      expectRows(region, 'EUR', [
        { name: categoryName('en', groceriesCategory), amountMinor: 5000, emoji: groceriesCategory.emoji },
        { name: categoryName('en', funCategory), amountMinor: 3250, emoji: funCategory.emoji },
      ])
      expect(queryCurrencyRegion('MKD')).toBeNull()
    })

    it('shows each row with a category name, an amount and an emoji that screen readers skip', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const [foodRow] = categoryRowsOf(await findCurrencyRegion('MKD'))

      expect(nameElementIn(foodRow, categoryName('en', foodCategory))).toBeTruthy()
      expect(amountElementIn(foodRow, 80000, 'MKD')).toBeTruthy()
      expect(within(foodRow).getByText(foodCategory.emoji).closest('[aria-hidden="true"]')).not.toBeNull()
    })

    it('shows the share of each category as nothing more than its amount: no percentages', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const region = await findCurrencyRegion('MKD')

      expect(region.textContent).not.toContain('%')
    })
  })

  describe('the row without a category', () => {
    it.each(languages)('is labelled with the text groupTotals.noCategory and comes last (%s)', async (language) => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending, language })

      const rows = categoryRowsOf(await findCurrencyRegion('MKD', language))
      const lastRow = rows[rows.length - 1]

      expect(rows).toHaveLength(3)
      expect(nameElementIn(lastRow, translated(language, 'groupTotals.noCategory'))).toBeTruthy()
    })

    it('is labelled No category in English and Без категорија in Macedonian', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: onlyUncategorisedSpending, language: 'mk' })

      const [row] = categoryRowsOf(await findCurrencyRegion('MKD', 'mk'))

      expect(nameElementIn(row, 'Без категорија')).toBeTruthy()
      expect(translated('en', 'groupTotals.noCategory')).toBe('No category')
    })

    it('shows a neutral emoji that screen readers skip, which is none of the category emojis', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const rows = categoryRowsOf(await findCurrencyRegion('MKD'))
      const emojis = pictographsIn(rows[2])

      expect(emojis).toHaveLength(1)
      expect(testCategories.map((category) => category.emoji)).not.toContain(emojis[0])
    })

    it('shows it even when it is the only row, without asking for the categories', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), { spending: onlyUncategorisedSpending })

      const region = await findCurrencyRegion('MKD')

      expectRows(region, 'MKD', [{ name: 'No category', amountMinor: 40000 }])
      expect(totalAmountElements(region, 40000, 'MKD').length).toBeGreaterThan(0)
      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(0)
    })

    it('shows it still while the categories are not answered, because it needs no category name', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        spending: onlyUncategorisedSpending,
        answers: { [`GET ${categoriesPath}`]: neverAnswer() },
      })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
    })
  })

  describe('two currencies', () => {
    it('shows two separate regions, the denars first and the euros second', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdAndEurSpending })

      const mkd = await findCurrencyRegion('MKD')
      const eur = currencyRegion('EUR')

      expect(isBefore(mkd, eur)).toBe(true)
      expect(mkd.contains(eur)).toBe(false)
      expect(eur.contains(mkd)).toBe(false)
    })

    it('keeps each total and each list inside its own region and never adds or converts them', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdAndEurSpending })

      const mkd = await findCurrencyRegion('MKD')
      const eur = currencyRegion('EUR')

      expect(totalAmountElements(mkd, 135000, 'MKD')).toHaveLength(1)
      expect(totalAmountElements(eur, 8250, 'EUR')).toHaveLength(1)
      expect(totalAmountElements(mkd, 8250, 'EUR')).toHaveLength(0)
      expect(totalAmountElements(eur, 135000, 'MKD')).toHaveLength(0)
      expectRows(mkd, 'MKD', mkdOnlyRows())
      expectRows(eur, 'EUR', [
        { name: categoryName('en', groceriesCategory), amountMinor: 5000, emoji: groceriesCategory.emoji },
        { name: categoryName('en', funCategory), amountMinor: 3250, emoji: funCategory.emoji },
      ])
    })

    it('shows the denar region in Macedonian under the label ден. and the euro region under EUR', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdAndEurSpending, language: 'mk' })

      const mkd = await findCurrencyRegion('MKD', 'mk')

      expect(within(mkd).getByRole('heading', { name: 'ден.' })).toBeTruthy()
      expect(currencyRegion('EUR', 'mk')).toBeTruthy()
    })

    it('asks for the categories only once for the two currencies', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), { spending: mkdAndEurSpending })
      await findCurrencyRegion('EUR')

      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(1)
    })
  })

  describe('the texts in Macedonian', () => {
    it('shows the category names and the amounts in Macedonian', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending, language: 'mk' })

      const region = await findCurrencyRegion('MKD', 'mk')

      expectRows(region, 'MKD', mkdOnlyRows('mk'), 'mk')
      expect(totalAmountElements(region, 135000, 'MKD', 'mk')).toHaveLength(1)
    })

    it('shows the panel title in Macedonian as Збирови по категорија', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending, language: 'mk' })
      await findCurrencyRegion('MKD', 'mk')

      expect(totalsTitle('mk').textContent).toBe('Збирови по категорија')
    })
  })

  describe('a plan in the answer', () => {
    it('shows the same regions and rows as without a plan', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: spendingWithPlan })

      const region = await findCurrencyRegion('MKD')

      expect(totalAmountElements(region, 135000, 'MKD')).toHaveLength(1)
      expectRows(region, 'MKD', mkdOnlyRows())
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows no error for a plan in another currency than the rows', async () => {
      const euroPlan = { ...spendingWithPlan.plan, amountMinor: 10000, currency: 'EUR', includedOtherCurrencyMinor: 135000 }
      await renderGroupApp(routes.groupTotals(groupId), { spending: { ...spendingWithPlan, plan: euroPlan } })

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows the empty message and no error for a plan with no spending yet', async () => {
      const planOnly = { currencies: [], plan: { ...spendingWithPlan.plan, spentMinor: 0, includedOtherCurrencyMinor: 0 } }
      await renderGroupApp(routes.groupTotals(groupId), { spending: planOnly })

      expect(await screen.findByText(translated('en', 'groupTotals.emptyTitle'))).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
      noRegionIsShown()
    })
  })

  describe('a group with no spending yet', () => {
    it.each([
      ['en', 'No spending yet', 'Totals by category show up here once someone adds an expense.'],
      ['mk', 'Сѐ уште нема трошоци', 'Збировите по категорија ќе се појават тука кога некој ќе додаде трошок.'],
    ] as const)('shows the short message and its hint (%s)', async (language, title, hint) => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: noSpendingYet, language })

      expect((await screen.findByText(translated(language, 'groupTotals.emptyTitle'))).textContent).toBe(title)
      expect(screen.getByText(translated(language, 'groupTotals.emptyHint')).textContent).toBe(hint)
    })

    it('centres the message and the hint in the tab panel', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: noSpendingYet })

      const title = await screen.findByText(translated('en', 'groupTotals.emptyTitle'))
      const hint = screen.getByText(translated('en', 'groupTotals.emptyHint'))

      expect(isCentred(title, panelOf(title))).toBe(true)
      expect(isCentred(hint, panelOf(hint))).toBe(true)
    })

    it('shows no currency region, no list and no error', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), { spending: noSpendingYet })
      await screen.findByText(translated('en', 'groupTotals.emptyTitle'))

      noRegionIsShown()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(within(panelOf(screen.getByText(translated('en', 'groupTotals.emptyTitle')))).queryByRole('list')).toBeNull()
      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(0)
    })

    it('does not show the empty message when there is spending', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })
      await findCurrencyRegion('MKD')

      expect(screen.queryByText(translated('en', 'groupTotals.emptyTitle'))).toBeNull()
      expect(screen.queryByText(translated('en', 'groupTotals.emptyHint'))).toBeNull()
    })
  })

  describe('loading', () => {
    it('shows pulsing placeholders, not a spinner, and no text or region while the spending is being asked', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        answers: { [`GET ${spendingPath}`]: neverAnswer() },
      })
      await showsGroupScreen()

      expect(screen.getByRole('status')).toBeTruthy()
      const look = loadingLook()
      expect(look.hasSpinner).toBe(false)
      expect(look.skeletonCount).toBeGreaterThan(0)
      noRegionIsShown()
      expect(screen.queryByText(translated('en', 'groupTotals.emptyTitle'))).toBeNull()
      expect(screen.queryByText(translated('en', 'groupTotals.emptyHint'))).toBeNull()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows pulsing placeholders and no category name while the categories are being asked', async () => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        spending: mkdOnlySpending,
        answers: { [`GET ${categoriesPath}`]: neverAnswer() },
      })
      await showsGroupScreen()
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(1)
      })

      expect(screen.getByRole('status')).toBeTruthy()
      const look = loadingLook()
      expect(look.hasSpinner).toBe(false)
      expect(look.skeletonCount).toBeGreaterThan(0)
      expect(screen.queryByText(categoryName('en', foodCategory))).toBeNull()
      expect(screen.queryByText(translated('en', 'groupTotals.noCategory'))).toBeNull()
      expect(screen.queryByText(translated('en', 'groupTotals.emptyTitle'))).toBeNull()
    })

    it('keeps the group header and the tabs while loading', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        answers: { [`GET ${spendingPath}`]: neverAnswer() },
      })

      await showsGroupScreen()

      expect(screen.getByRole('link', { name: translated('en', 'groupTotals.tab') }).getAttribute('aria-current')).toBe('page')
    })
  })

  describe('failing', () => {
    it('shows the translated error with a retry button, not the totals, and keeps the group header', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        spending: mkdOnlySpending,
        answers: { [`GET ${spendingPath}`]: serverError },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
      noRegionIsShown()
      expect(screen.queryByText(translated('en', 'groupTotals.emptyTitle'))).toBeNull()
    })

    it('makes the retry button a native button that is at least 44 px high', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        answers: { [`GET ${spendingPath}`]: serverError },
      })

      const retry = await screen.findByRole('button', { name: translated('en', 'common.retry') })

      expect(retry.tagName).toBe('BUTTON')
      expect(minHeightStepOf(retry)).toBeGreaterThanOrEqual(11)
    })

    it('shows the error in Macedonian', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        language: 'mk',
        answers: { [`GET ${spendingPath}`]: serverError },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('mk', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('mk', 'common.retry') })).toBeTruthy()
    })

    it('shows the network message when the spending cannot be reached', async () => {
      await renderGroupApp(routes.groupTotals(groupId), {
        answers: { [`GET ${spendingPath}`]: networkFailureAnswer() },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('asks for the spending again and shows it when the retry button is pressed', async () => {
      let tries = 0
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        answers: {
          [`GET ${spendingPath}`]: () => {
            tries += 1
            return tries === 1 ? serverError() : Response.json(mkdOnlySpending)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(2)
    })

    it('shows the error with a retry button, and asks for the categories again, when the categories cannot be loaded', async () => {
      let tries = 0
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        spending: mkdOnlySpending,
        answers: {
          [`GET ${categoriesPath}`]: () => {
            tries += 1
            return tries === 1 ? serverError() : Response.json({ categories: testCategories })
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findCurrencyRegion('MKD')).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(2)
    })

    it.each([
      ['a list instead of an object', []],
      ['no currencies', { plan: null }],
      ['no plan', { currencies: [] }],
      ['a currency of USD', { currencies: [{ currency: 'USD', totalMinor: 100, categories: [] }], plan: null }],
      ['a negative total', { currencies: [{ currency: 'MKD', totalMinor: -100, categories: [] }], plan: null }],
      ['a total with a fraction', { currencies: [{ currency: 'MKD', totalMinor: 100.5, categories: [] }], plan: null }],
      ['a category row with a text amount', { currencies: [{ currency: 'MKD', totalMinor: 100, categories: [{ categoryId: null, totalMinor: '100' }] }], plan: null }],
      ['a plan with an amount of 0', { ...mkdOnlySpending, plan: { ...spendingWithPlan.plan, amountMinor: 0 } }],
      ['a plan without a currency', { ...mkdOnlySpending, plan: { ...spendingWithPlan.plan, currency: undefined } }],
    ])('shows the generic error with a retry button when the answer has %s', async (_name, body) => {
      await renderGroupApp(routes.groupTotals(groupId), { answers: { [`GET ${spendingPath}`]: jsonAnswer(body) } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      noRegionIsShown()
    })

    it.each(planFields)('shows the generic error when the plan in the answer lacks %s', async (field) => {
      const plan = Object.fromEntries(Object.entries(spendingWithPlan.plan).filter(([name]) => name !== field))
      await renderGroupApp(routes.groupTotals(groupId), {
        answers: { [`GET ${spendingPath}`]: jsonAnswer({ ...mkdOnlySpending, plan }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      noRegionIsShown()
    })

    it.each([
      [404, 'GROUP_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the spending only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${spendingPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(1)
    })

    it('shows a screen error naming the category id when a row has a category that is not in the built-in list', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderGroupApp(routes.groupTotals(groupId), { spending: unknownCategorySpending })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      await waitFor(() => {
        expect(crashErrors(consoleError).map((error) => error.message).join('\n')).toContain(unknownCategoryId)
      })
      noRegionIsShown()
    })
  })

  describe('refreshing', () => {
    it('shows the new totals after the spending is marked out of date under the key groups, the group id, spending', async () => {
      let tries = 0
      const { queryClient, fetchMock } = await renderGroupApp(routes.groupTotals(groupId), {
        answers: {
          [`GET ${spendingPath}`]: () => {
            tries += 1
            return Response.json(tries === 1 ? mkdOnlySpending : laterAnswerSpending)
          },
        },
      })
      const region = await findCurrencyRegion('MKD')
      expect(totalAmountElements(region, 135000, 'MKD')).toHaveLength(1)

      await queryClient.invalidateQueries({ queryKey: ['groups', groupId, 'spending'] })

      await waitFor(() => {
        expect(totalAmountElements(currencyRegion('MKD'), 220000, 'MKD')).toHaveLength(1)
      })
      expectRows(currencyRegion('MKD'), 'MKD', [
        { name: categoryName('en', foodCategory), amountMinor: 150000, emoji: foodCategory.emoji },
        { name: categoryName('en', transportCategory), amountMinor: 70000, emoji: transportCategory.emoji },
      ])
      expect(requestCount(fetchMock, 'GET', spendingPath)).toBe(2)
    })

    it('replaces a currency block that is gone from the new answer', async () => {
      let tries = 0
      const { queryClient } = await renderGroupApp(routes.groupTotals(groupId), {
        answers: {
          [`GET ${spendingPath}`]: () => {
            tries += 1
            return Response.json(tries === 1 ? mkdAndEurSpending : mkdOnlySpending)
          },
        },
      })
      await findCurrencyRegion('EUR')

      await queryClient.invalidateQueries({ queryKey: ['groups', groupId, 'spending'] })

      await waitFor(() => {
        expect(queryCurrencyRegion('EUR')).toBeNull()
      })
      expect(currencyRegion('MKD')).toBeTruthy()
    })
  })

  describe('the look on a 360 px phone', () => {
    it('shows an amount of 999 999 999 900 denars as the total and in the row', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: bigDenarSpending })

      const region = await findCurrencyRegion('MKD')
      const [row] = categoryRowsOf(region)

      expect(totalAmountElements(region, bigDenarAmountMinor, 'MKD')).toHaveLength(1)
      expect(amountElementIn(row, bigDenarAmountMinor, 'MKD')).toBeTruthy()
    })

    it('lets the name wrap inside a column that can shrink, and keeps the amount on one line', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: bigDenarSpending })

      const [row] = categoryRowsOf(await findCurrencyRegion('MKD'))
      const name = nameElementIn(row, 'Stay')
      const amount = amountElementIn(row, bigDenarAmountMinor, 'MKD')

      expect(Array.from(name.classList)).toContain('break-words')
      expect(Array.from(name.classList)).not.toContain('whitespace-nowrap')
      expect(Array.from(name.classList)).not.toContain('truncate')
      expect(hasClassOnItselfOrAncestorsBelow(name, row, ['min-w-0', 'flex-1'])).toBe(true)
      expect(amount.closest('.whitespace-nowrap')).not.toBeNull()
      expect(row.contains(amount.closest('.whitespace-nowrap'))).toBe(true)
    })

    it('keeps the amount of every row on one line', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdAndEurSpending })

      await findCurrencyRegion('MKD')
      const rows = [...categoryRowsOf(currencyRegion('MKD')), ...categoryRowsOf(currencyRegion('EUR'))]

      expect(rows).toHaveLength(5)
      for (const row of rows) {
        const amountText = row.querySelector('.sr-only')
        expect(amountText?.closest('.whitespace-nowrap'), `the amount in "${row.textContent}" should not wrap`).not.toBeNull()
      }
    })

    it('does not make the names unbreakable', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { spending: mkdOnlySpending })

      const rows = categoryRowsOf(await findCurrencyRegion('MKD'))

      for (const [index, expectedName] of ['Food & drinks', 'Transport', 'No category'].entries()) {
        expect(Array.from(nameElementIn(rows[index], expectedName).classList)).toContain('break-words')
      }
    })
  })
})
