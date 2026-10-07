import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { Group } from '@/core/services/groups/groupsService'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { routes } from '@/core/router/routes'
import {
  dinnerExpenseId,
  dinnerRow,
  expenseRows,
  hotelRow,
  taxiRow,
  testCategories,
  taxiExpenseId,
  transportCategory,
  otherCategory,
  foodCategory,
} from '@/test/expenseTestData'
import {
  categoriesPath,
  categoryName,
  expensesPath,
  freezeTime,
  isBefore,
  macedonianDay,
  plainSpaces,
  startsWith,
  unfreezeTime,
  restoreRealRetryPolicy,
} from '@/test/expenseTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'
import { GroupScreen } from './GroupScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

interface RenderOptions {
  language?: Language
  group?: Group
  timeZone?: string
  expenses?: ExpenseListRow[]
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderGroup(options: RenderOptions = {}) {
  const group = options.group ?? testGroup
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET /api/groups/${group.id}`]: jsonAnswer(group),
    [`GET ${expensesPath}`]: jsonAnswer({ expenses: options.expenses ?? expenseRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId', element: <GroupScreen /> },
      { path: '/groups/:groupId/expenses/new', element: <p>add expense page</p> },
      { path: '/groups/:groupId/expenses/deleted', element: <p>recently deleted expenses page</p> },
      { path: '/groups/:groupId/expenses/:expenseId', element: <p>expense detail page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.group(group.id),
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...testMe, language, timeZone: options.timeZone ?? testMe.timeZone })(queryClient)
        options.seedCache?.(queryClient)
      },
    },
  )
  return { fetchMock, ...rendered }
}

async function showsExpenses(): Promise<void> {
  await screen.findByRole('link', { name: /Dinner/ })
}

function rowOf(title: string): HTMLElement {
  return screen.getByRole('link', { name: new RegExp(title) })
}

function addExpenseLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'expense.addTitle') })
}

function recentlyDeletedLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: startsWith(translated(language, 'groups.recentlyDeletedLink')) })
}

function expectRowsUnderLabels(labelsWithRows: [string, HTMLElement[]][]): void {
  const labels = labelsWithRows.map(([text]) => screen.getByText(text))
  labelsWithRows.forEach(([text, rows], index) => {
    const label = labels[index]
    const nextLabel = labels[index + 1]
    for (const row of rows) {
      expect(isBefore(label, row), `the row should come after the label "${text}"`).toBe(true)
      if (nextLabel !== undefined) {
        expect(isBefore(row, nextLabel), `the row should come before the next label after "${text}"`).toBe(true)
      }
    }
  })
}

describe('GroupScreen expenses', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the rows', () => {
    it('shows one row for every expense of the group', async () => {
      await renderGroup()
      await showsExpenses()

      for (const title of ['Dinner', 'Taxi', 'Groceries', 'Hotel', 'Gift']) {
        expect(rowOf(title)).toBeTruthy()
      }
    })

    it.each([
      [dinnerRow, 'Dinner'],
      [taxiRow, 'Taxi'],
      [hotelRow, 'Hotel'],
    ])('shows the title and the amount of the expense %#', async (row, title) => {
      await renderGroup()
      await showsExpenses()

      const rowElement = rowOf(title)

      expect(within(rowElement).getByText(title)).toBeTruthy()
      expect(plainSpaces(rowElement.textContent ?? '')).toContain(
        plainSpaces(formatMoney(row.amountMinor, row.currency, 'en')),
      )
    })

    it('shows the amount of an EUR expense in euros with two decimals', async () => {
      await renderGroup()
      await showsExpenses()

      expect(plainSpaces(rowOf('Groceries').textContent ?? '')).toContain('€45.00')
    })

    it('shows the emoji of the category of every expense', async () => {
      await renderGroup()
      await showsExpenses()

      expect(within(rowOf('Dinner')).getByText(foodCategory.emoji)).toBeTruthy()
      expect(within(rowOf('Taxi')).getByText(transportCategory.emoji)).toBeTruthy()
      expect(within(rowOf('Other')).getByText(otherCategory.emoji)).toBeTruthy()
    })

    it('shows who paid and the share of the person in the first line of every row', async () => {
      await renderGroup()
      await showsExpenses()

      const line = translated('en', 'expenses.paidByShare', {
        name: 'Filip',
        amount: formatMoney(60000, 'MKD', 'en'),
      })

      expect(within(rowOf('Dinner')).getByText(plainSpaces(line), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('shows the share of an EUR expense in euros', async () => {
      await renderGroup()
      await showsExpenses()

      const line = translated('en', 'expenses.paidByShare', {
        name: 'Marko',
        amount: formatMoney(1125, 'EUR', 'en'),
      })

      expect(within(rowOf('Groceries')).getByText(plainSpaces(line), { normalizer: plainSpaces })).toBeTruthy()
    })

    it("says 'you're not in the split' when the person has no share", async () => {
      await renderGroup()
      await showsExpenses()

      const line = translated('en', 'expenses.paidByNotInSplit', { name: 'Grandma' })

      expect(within(rowOf('Other')).getByText(line)).toBeTruthy()
    })

    it('shows the name of the category when the expense has no title', async () => {
      await renderGroup()
      await showsExpenses()

      expect(within(rowOf('Other')).getByText(categoryName('en', otherCategory))).toBeTruthy()
    })

    it('shows a dash as the title of an expense with neither a title nor a category', async () => {
      await renderGroup({ expenses: [{ ...dinnerRow, title: null, categoryId: null }] })

      const row = await screen.findByRole('link', { name: /\u2014/ })

      expect(within(row).getByText('\u2014')).toBeTruthy()
    })

    it('shows a title that is written in Macedonian as it is', async () => {
      await renderGroup({ expenses: [{ ...dinnerRow, title: 'Вечера' }] })

      expect(await screen.findByRole('link', { name: /Вечера/ })).toBeTruthy()
    })

    it('links every row to the detail of its expense', async () => {
      await renderGroup()
      await showsExpenses()

      expect(rowOf('Dinner').getAttribute('href')).toBe(routes.groupExpense(testGroup.id, dinnerExpenseId))
      expect(rowOf('Taxi').getAttribute('href')).toBe(routes.groupExpense(testGroup.id, taxiExpenseId))
    })

    it('opens the detail of the expense when its row is pressed', async () => {
      const { router } = await renderGroup()
      await showsExpenses()

      fireEvent.click(rowOf('Taxi'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpense(testGroup.id, taxiExpenseId))
      })
    })
  })

  describe('grouping by date', () => {
    it('writes Today and Yesterday with their dates and the other days with the date only, in English', async () => {
      await renderGroup()
      await showsExpenses()

      expect(screen.getByText('Today · 6 Oct')).toBeTruthy()
      expect(screen.getByText('Yesterday · 5 Oct')).toBeTruthy()
      expect(screen.getByText('3 Oct')).toBeTruthy()
    })

    it('adds the year to a day that is not in this year', async () => {
      await renderGroup()
      await showsExpenses()

      expect(screen.getByText('24 Dec 2025')).toBeTruthy()
    })

    it('shows every date label once even when a day has several expenses', async () => {
      await renderGroup()
      await showsExpenses()

      expect(screen.getAllByText('Yesterday · 5 Oct')).toHaveLength(1)
      expect(screen.getAllByText('3 Oct')).toHaveLength(1)
    })

    it('puts every row under the label of its own date, newest day first', async () => {
      await renderGroup()
      await showsExpenses()

      expectRowsUnderLabels([
        ['Today · 6 Oct', [rowOf('Dinner')]],
        ['Yesterday · 5 Oct', [rowOf('Taxi'), rowOf('Groceries')]],
        ['3 Oct', [rowOf('Hotel'), rowOf('Other')]],
        ['24 Dec 2025', [rowOf('Gift')]],
      ])
    })

    it('keeps the order of the answer inside one day', async () => {
      await renderGroup()
      await showsExpenses()

      expect(isBefore(rowOf('Taxi'), rowOf('Groceries'))).toBe(true)
      expect(isBefore(rowOf('Hotel'), rowOf('Other'))).toBe(true)
    })

    it('writes the labels in Macedonian with the date the way Intl writes it for Macedonian', async () => {
      await renderGroup({ language: 'mk' })
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.getByText(`${translated('mk', 'expenses.today')} · ${macedonianDay('2026-10-06', false)}`)).toBeTruthy()
      expect(
        screen.getByText(`${translated('mk', 'expenses.yesterday')} · ${macedonianDay('2026-10-05', false)}`),
      ).toBeTruthy()
      expect(screen.getByText(macedonianDay('2026-10-03', false))).toBeTruthy()
      expect(screen.getByText(macedonianDay('2025-12-24', true))).toBeTruthy()
    })

    it('decides what Today is from the time zone of the account, not from the clock of the phone', async () => {
      await renderGroup({ timeZone: 'Pacific/Pago_Pago' })
      await showsExpenses()

      expect(screen.getByText('Today · 5 Oct')).toBeTruthy()
      expect(screen.getByText('6 Oct')).toBeTruthy()
      expect(screen.queryByText('Yesterday · 5 Oct')).toBeNull()
    })

    it('writes a day in the future with its date only', async () => {
      await renderGroup({ expenses: [{ ...dinnerRow, expenseDate: '2026-12-12' }, hotelRow] })
      await showsExpenses()

      expect(screen.getByText('12 Dec')).toBeTruthy()
      expect(screen.queryByText(/Today/)).toBeNull()
    })
  })

  describe('in Macedonian', () => {
    it('writes who paid, the share and the amounts in Macedonian', async () => {
      await renderGroup({ language: 'mk' })
      await screen.findByRole('link', { name: /Dinner/ })

      const line = translated('mk', 'expenses.paidByShare', {
        name: 'Filip',
        amount: formatMoney(60000, 'MKD', 'mk'),
      })
      const dinner = screen.getByRole('link', { name: /Dinner/ })

      expect(within(dinner).getByText(plainSpaces(line), { normalizer: plainSpaces })).toBeTruthy()
      expect(plainSpaces(dinner.textContent ?? '')).toContain(
        plainSpaces(formatMoney(240000, 'MKD', 'mk')),
      )
    })

    it('writes the name of a category in Macedonian when the expense has no title', async () => {
      await renderGroup({ language: 'mk' })
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.getByText(categoryName('mk', otherCategory))).toBeTruthy()
    })

    it('says in Macedonian that the person is not in the split', async () => {
      await renderGroup({ language: 'mk' })
      await screen.findByRole('link', { name: /Dinner/ })

      expect(screen.getByText(translated('mk', 'expenses.paidByNotInSplit', { name: 'Grandma' }))).toBeTruthy()
    })
  })

  describe('with no expenses', () => {
    it.each(languages)('shows the empty text and no date label (%s)', async (language) => {
      await renderGroup({ language, expenses: [] })

      expect(await screen.findByText(translated(language, 'expenses.empty'))).toBeTruthy()
      expect(screen.queryByText(translated(language, 'expenses.today'), { exact: false })).toBeNull()
    })

    it('does not show the empty text while there are expenses', async () => {
      await renderGroup()
      await showsExpenses()

      expect(screen.queryByText(translated('en', 'expenses.empty'))).toBeNull()
    })
  })

  describe('the Add button', () => {
    it.each(languages)('is a round + link named Add expense that opens the Add screen of this group (%s)', async (language) => {
      await renderGroup({ language })
      await screen.findByRole('link', { name: /Dinner/ })

      expect(addExpenseLink(language).getAttribute('href')).toBe(routes.groupExpenseNew(testGroup.id))
    })

    it('is shown when the group has no expenses yet', async () => {
      await renderGroup({ expenses: [] })
      await screen.findByText(translated('en', 'expenses.empty'))

      expect(addExpenseLink().getAttribute('href')).toBe(routes.groupExpenseNew(testGroup.id))
    })

    it('opens the Add screen when it is pressed', async () => {
      const { router } = await renderGroup()
      await showsExpenses()

      fireEvent.click(addExpenseLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroup.id))
      })
    })
  })

  describe('the Recently deleted link', () => {
    it.each(languages)('is a quiet link under the list to the deleted expenses of this group (%s)', async (language) => {
      await renderGroup({ language })
      await screen.findByRole('link', { name: /Dinner/ })

      const link = recentlyDeletedLink(language)

      expect(link.getAttribute('href')).toBe(routes.groupExpensesDeleted(testGroup.id))
      expect(isBefore(rowOf('Gift'), link)).toBe(true)
    })

    it('is shown when the group has no expenses', async () => {
      await renderGroup({ expenses: [] })
      await screen.findByText(translated('en', 'expenses.empty'))

      expect(recentlyDeletedLink().getAttribute('href')).toBe(routes.groupExpensesDeleted(testGroup.id))
    })

    it('opens the deleted expenses when it is pressed', async () => {
      const { router } = await renderGroup()
      await showsExpenses()

      fireEvent.click(recentlyDeletedLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpensesDeleted(testGroup.id))
      })
    })
  })

  describe('with the Add people card', () => {
    function addPeopleHeading(): HTMLElement {
      return screen.getByRole('heading', { name: translated('en', 'group.addPeople') })
    }

    it('shows the card above the empty text of the expenses when the group has one person and no expenses', async () => {
      await renderGroup({ group: groupOf({ memberCount: 1 }), expenses: [] })

      const emptyText = await screen.findByText(translated('en', 'expenses.empty'))

      expect(addPeopleHeading()).toBeTruthy()
      expect(screen.getByRole('button', { name: translated('en', 'group.addName') })).toBeTruthy()
      expect(isBefore(addPeopleHeading(), emptyText)).toBe(true)
    })

    it('shows no card when the group has one person and has expenses', async () => {
      await renderGroup({ group: groupOf({ memberCount: 1 }) })
      await showsExpenses()

      expect(screen.queryByRole('heading', { name: translated('en', 'group.addPeople') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'group.addName') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'group.shareLink') })).toBeNull()
    })

    it('shows no card when the group has several people', async () => {
      await renderGroup({ group: groupOf({ memberCount: 3 }) })
      await showsExpenses()

      expect(screen.queryByRole('heading', { name: translated('en', 'group.addPeople') })).toBeNull()
    })

    it('keeps the + button and the Recently deleted link when the card is shown', async () => {
      await renderGroup({ group: groupOf({ memberCount: 1 }), expenses: [] })
      await screen.findByText(translated('en', 'expenses.empty'))

      expect(addExpenseLink().getAttribute('href')).toBe(routes.groupExpenseNew(testGroup.id))
      expect(recentlyDeletedLink().getAttribute('href')).toBe(routes.groupExpensesDeleted(testGroup.id))
      expect(isBefore(screen.getByText(translated('en', 'expenses.empty')), recentlyDeletedLink())).toBe(true)
    })
  })

  describe('loading and failing', () => {
    it('shows a loading spinner while the expenses are being asked', async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn<typeof fetch>(async (input) => {
          const url = String(input)
          if (url === `/api/groups/${testGroup.id}`) {
            return Response.json(testGroup)
          }
          if (url === categoriesPath) {
            return Response.json({ categories: testCategories })
          }
          return new Promise<Response>(() => {})
        }),
      )
      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId', element: <GroupScreen /> }],
        routes.group(testGroup.id),
        { seedCache: seedMe(testMe) },
      )
      await screen.findByRole('heading', { level: 1, name: testGroup.name })

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it('shows the translated error with a retry button when the expenses cannot be loaded, and keeps the group header', async () => {
      await renderGroup({ answers: { [`GET ${expensesPath}`]: () => new Response(null, { status: 500 }) } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    })

    it.each([
      [404, 'GROUP_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the expenses only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderGroup({
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${expensesPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', expensesPath)).toBe(1)
    })

    it('shows the network message when the expenses cannot be reached', async () => {
      await renderGroup({ answers: { [`GET ${expensesPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('asks for the expenses again and shows them when the retry button is pressed', async () => {
      let tries = 0
      await renderGroup({
        answers: {
          [`GET ${expensesPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json({ expenses: expenseRows })
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await showsExpenses()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows no retry button but a Go to Groups link when the group is not found', async () => {
      await renderGroup({
        answers: {
          [`GET /api/groups/${testGroup.id}`]: () =>
            new Response(JSON.stringify({ status: 404, errorCode: 'GROUP_NOT_FOUND' }), {
              status: 404,
              headers: { 'content-type': 'application/problem+json' },
            }),
          [`GET ${expensesPath}`]: () =>
            new Response(JSON.stringify({ status: 404, errorCode: 'GROUP_NOT_FOUND' }), {
              status: 404,
              headers: { 'content-type': 'application/problem+json' },
            }),
        },
      })

      await screen.findByRole('link', { name: translated('en', 'groups.goToGroups') })

      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
      expect(screen.queryByRole('link', { name: translated('en', 'expense.addTitle') })).toBeNull()
    })
  })

  it('keeps the expenses in the cache under the key groups, the group id, expenses and the categories under the key categories', async () => {
    const { queryClient } = await renderGroup()
    await showsExpenses()

    expect(queryClient.getQueryData(['groups', testGroup.id, 'expenses'])).toEqual(expenseRows)
    expect(queryClient.getQueryData(['categories'])).toEqual(testCategories)
  })

  it('asks for the expenses of this group only once and never asks for the members', async () => {
    const { fetchMock } = await renderGroup()
    await showsExpenses()

    const urls = fetchMock.mock.calls.map(([url]) => String(url))
    expect(urls.filter((url) => url === expensesPath)).toHaveLength(1)
    expect(urls).not.toContain(`/api/groups/${testGroup.id}/members`)
  })
})
