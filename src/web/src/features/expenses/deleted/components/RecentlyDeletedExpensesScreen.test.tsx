import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { DeletedExpenseRow } from '@/core/services/expenses/expensesService'
import { formatMoney } from '@/shared/utils/formatMoney'
import {
  deletedRows,
  funCategory,
  healthCategory,
  museumDeletedRow,
  museumExpenseId,
  pharmacyDeletedRow,
  pharmacyExpenseId,
  testCategories,
} from '@/test/expenseTestData'
import {
  categoriesPath,
  categoryName,
  deletedExpensesPath,
  elementWithAll,
  expenseRestorePathOf,
  expensesPath,
  findErrorText,
  freezeTime,
  groupPath,
  plainSpaces,
  unfreezeTime,
  restoreRealRetryPolicy,
} from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { RecentlyDeletedExpensesScreen } from './RecentlyDeletedExpensesScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const museumRestorePath = expenseRestorePathOf(museumExpenseId)
const pharmacyRestorePath = expenseRestorePathOf(pharmacyExpenseId)
const maxRowExtra = 90

interface RenderOptions {
  language?: Language
  rows?: DeletedExpenseRow[]
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderDeleted(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${deletedExpensesPath}`]: jsonAnswer({ expenses: options.rows ?? deletedRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${museumRestorePath}`]: noContentAnswer(),
    [`POST ${pharmacyRestorePath}`]: noContentAnswer(),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/deleted', element: <RecentlyDeletedExpensesScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpensesDeleted(testGroupId),
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...testMe, language })(queryClient)
        options.seedCache?.(queryClient)
      },
    },
  )
  return { fetchMock, ...rendered }
}

async function showsRows(): Promise<void> {
  await screen.findByText(/Museum tickets/)
}

function money(amountMinor: number, language: Language = 'en'): string {
  return plainSpaces(formatMoney(amountMinor, 'MKD', language))
}

function restoreButtons(language: Language = 'en'): HTMLElement[] {
  return screen.queryAllByRole('button', { name: translated(language, 'recentlyDeleted.restore') })
}

function rowOf(title: string, otherTitles: string[]): HTMLElement {
  let row: HTMLElement = elementWithAll([title], maxRowExtra)
  for (;;) {
    const parent = row.parentElement
    const parentText = plainSpaces(parent?.textContent ?? '')
    if (parent === null || otherTitles.some((other) => parentText.includes(other))) {
      return row
    }
    row = parent
  }
}

function museumRow(): HTMLElement {
  return rowOf('Museum tickets', ['Pharmacy'])
}

function pharmacyRow(): HTMLElement {
  return rowOf('Pharmacy', ['Museum tickets'])
}

describe('RecentlyDeletedExpensesScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the header', () => {
    it.each(languages)('shows the Recently deleted heading (%s)', async (language) => {
      await renderDeleted({ language })

      expect(
        await screen.findByRole('heading', { name: translated(language, 'recentlyDeleted.title') }),
      ).toBeTruthy()
    })

    it('has a back button to the group', async () => {
      await renderDeleted()

      const back = await screen.findByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.group(testGroupId))
    })

    it('shows no bottom bar', async () => {
      await renderDeleted()
      await showsRows()

      expect(screen.queryByRole('navigation')).toBeNull()
    })

    it('shows the loading spinner while the deleted expenses are being asked', async () => {
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/expenses/deleted', element: <RecentlyDeletedExpensesScreen /> }],
        routes.groupExpensesDeleted(testGroupId),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })
  })

  describe('the rows', () => {
    it('shows the title and the amount of every deleted expense', async () => {
      await renderDeleted()
      await showsRows()

      expect(elementWithAll(['Museum tickets', money(80000)], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Pharmacy', money(35000)], maxRowExtra)).toBeTruthy()
    })

    it('shows the emoji of the category of every deleted expense', async () => {
      await renderDeleted()
      await showsRows()

      expect(within(museumRow()).getByText(funCategory.emoji)).toBeTruthy()
      expect(within(pharmacyRow()).getByText(healthCategory.emoji)).toBeTruthy()
    })

    it('shows the name of the category when the expense had no title', async () => {
      await renderDeleted({ rows: [{ ...museumDeletedRow, title: null }] })

      expect(await screen.findByText(categoryName('en', funCategory), { exact: false })).toBeTruthy()
    })

    it('shows a dash as the title of an expense with neither a title nor a category', async () => {
      await renderDeleted({ rows: [{ ...museumDeletedRow, title: null, categoryId: null }] })

      expect(await screen.findByText(/\u2014/)).toBeTruthy()
    })

    it('shows the amount of an EUR expense in euros', async () => {
      await renderDeleted({ rows: [{ ...museumDeletedRow, currency: 'EUR', amountMinor: 4500 }] })
      await showsRows()

      expect(plainSpaces(museumRow().textContent ?? '')).toContain('€45.00')
    })

    it('shows each expense with its own restore-until date in English', async () => {
      await renderDeleted()
      await showsRows()

      expect(within(museumRow()).getByText('Can be restored until 10 Oct 2026')).toBeTruthy()
      expect(within(pharmacyRow()).getByText('Can be restored until 9 Oct 2026')).toBeTruthy()
    })

    it('shows each expense with its own restore-until date in Macedonian', async () => {
      await renderDeleted({ language: 'mk' })
      await screen.findByText('Museum tickets', { exact: false })

      expect(
        within(museumRow()).getByText(/^Може да се врати до 10 октомври 2026( г\.)?$/),
      ).toBeTruthy()
      expect(
        within(pharmacyRow()).getByText(/^Може да се врати до 9 октомври 2026( г\.)?$/),
      ).toBeTruthy()
    })

    it('keeps the order of the answer, newest deleted first', async () => {
      await renderDeleted()
      await showsRows()

      expect(
        museumRow().compareDocumentPosition(pharmacyRow()) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy()
    })

    it('writes the amounts in Macedonian', async () => {
      await renderDeleted({ language: 'mk' })
      await screen.findByText('Museum tickets', { exact: false })

      expect(plainSpaces(museumRow().textContent ?? '')).toContain(money(80000, 'mk'))
    })
  })

  describe('the Restore button', () => {
    it.each(languages)('is shown only on the expense that the person may restore (%s)', async (language) => {
      await renderDeleted({ language })
      await screen.findByText('Museum tickets', { exact: false })

      expect(restoreButtons(language)).toHaveLength(1)
      expect(
        within(museumRow()).getByRole('button', { name: translated(language, 'recentlyDeleted.restore') }),
      ).toBeTruthy()
      expect(
        within(pharmacyRow()).queryByRole('button', { name: translated(language, 'recentlyDeleted.restore') }),
      ).toBeNull()
    })

    it('is shown on every row when the person may restore all of them', async () => {
      await renderDeleted({ rows: [museumDeletedRow, { ...pharmacyDeletedRow, canEdit: true }] })
      await showsRows()

      expect(restoreButtons()).toHaveLength(2)
    })

    it('is shown on no row when the person may restore none of them', async () => {
      await renderDeleted({
        rows: [
          { ...museumDeletedRow, canEdit: false },
          { ...pharmacyDeletedRow, canEdit: false },
        ],
      })
      await showsRows()

      expect(restoreButtons()).toHaveLength(0)
    })

    it('sends the restore request for the expense whose Restore button is pressed and for no other', async () => {
      const { fetchMock } = await renderDeleted({
        rows: [museumDeletedRow, { ...pharmacyDeletedRow, canEdit: true }],
      })
      await showsRows()

      fireEvent.click(within(pharmacyRow()).getByRole('button', { name: translated('en', 'recentlyDeleted.restore') }))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', pharmacyRestorePath)).toBe(1)
      })
      expect(requestCount(fetchMock, 'POST', museumRestorePath)).toBe(0)
      expect(requestsOf(fetchMock, 'POST', pharmacyRestorePath)[0].body).toBeUndefined()
    })

    it.each(languages)('shows the Expense restored toast after a restore (%s)', async (language) => {
      await renderDeleted({ language })
      await screen.findByText('Museum tickets', { exact: false })

      fireEvent.click(restoreButtons(language)[0])

      await waitFor(() => {
        expect(shownToastTexts()).toContain(translated(language, 'expenses.restored'))
      })
    })

    it('takes the restored expense off the list and keeps the other one', async () => {
      let isRestored = false
      await renderDeleted({
        answers: {
          [`GET ${deletedExpensesPath}`]: () =>
            Response.json({ expenses: isRestored ? [pharmacyDeletedRow] : deletedRows }),
          [`POST ${museumRestorePath}`]: () => {
            isRestored = true
            return new Response(null, { status: 204 })
          },
        },
      })
      await showsRows()

      fireEvent.click(restoreButtons()[0])

      await waitFor(() => {
        expect(screen.queryByText('Museum tickets', { exact: false })).toBeNull()
      })
      expect(screen.getByText('Pharmacy', { exact: false })).toBeTruthy()
    })

    it('shows the empty text after the last deleted expense is restored', async () => {
      let isRestored = false
      await renderDeleted({
        rows: [museumDeletedRow],
        answers: {
          [`GET ${deletedExpensesPath}`]: () =>
            Response.json({ expenses: isRestored ? [] : [museumDeletedRow] }),
          [`POST ${museumRestorePath}`]: () => {
            isRestored = true
            return new Response(null, { status: 204 })
          },
        },
      })
      await showsRows()

      fireEvent.click(restoreButtons()[0])

      expect(await screen.findByText(translated('en', 'expenses.deletedEmpty'))).toBeTruthy()
    })

    it('marks the expenses list and the detail of the restored expense as out of date after a restore', async () => {
      const keys = [
        ['groups', testGroupId, 'expenses'],
        ['groups', testGroupId, 'expenses', museumExpenseId],
      ]
      const { queryClient } = await renderDeleted({
        seedCache: (cache) => {
          for (const key of keys) {
            cache.setQueryData(key, [])
          }
        },
      })
      await showsRows()

      fireEvent.click(restoreButtons()[0])

      await waitFor(() => {
        for (const key of keys) {
          expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
        }
      })
    })

    it('disables the Restore button while the request waits for an answer', async () => {
      await renderDeleted()
      await screen.findByText('Museum tickets', { exact: false })
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      fireEvent.click(restoreButtons()[0])

      await expectDisabledWhilePending(restoreButtons()[0])
    })

    it.each(
      languages.flatMap((language) =>
        (['EXPENSE_RESTORE_EXPIRED', 'EXPENSE_NOT_DELETED', 'EXPENSE_NOT_ALLOWED'] as const).map((code) => [language, code] as const),
      ),
    )('shows the translated text for the answer %s %s, keeps the expense on the list and shows no Expense restored toast', async (language, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderDeleted({
        language,
        answers: { [`POST ${museumRestorePath}`]: problemAnswer(code === 'EXPENSE_NOT_ALLOWED' ? 403 : 400, code) },
      })
      await screen.findByText('Museum tickets', { exact: false })

      fireEvent.click(restoreButtons(language)[0])

      await findErrorText(translated(language, `errors.${code}`))
      expect(screen.getByText('Museum tickets', { exact: false })).toBeTruthy()
      expect(shownToastTexts()).not.toContain(translated(language, 'expenses.restored'))
    })

    it('shows the network message when the restore cannot reach the server', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderDeleted({ answers: { [`POST ${museumRestorePath}`]: networkFailureAnswer() } })
      await screen.findByText('Museum tickets', { exact: false })

      fireEvent.click(restoreButtons()[0])

      await findErrorText(translated('en', 'errors.network'))
    })
  })

  describe('with nothing deleted lately', () => {
    it.each(languages)('shows the empty text and no Restore button (%s)', async (language) => {
      await renderDeleted({ language, rows: [] })

      expect(await screen.findByText(translated(language, 'expenses.deletedEmpty'))).toBeTruthy()
      expect(restoreButtons(language)).toHaveLength(0)
    })

    it('does not show the empty text while there are deleted expenses', async () => {
      await renderDeleted()
      await showsRows()

      expect(screen.queryByText(translated('en', 'expenses.deletedEmpty'))).toBeNull()
    })
  })

  describe('loading and failing', () => {
    it('shows the translated error with a retry button when the list cannot be loaded, and shows the rows after a retry', async () => {
      let tries = 0
      await renderDeleted({
        answers: {
          [`GET ${deletedExpensesPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json({ expenses: deletedRows })
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      await showsRows()
    })

    it('shows the GROUP_NOT_FOUND text with a Go to Groups link and no retry button when the group is not found', async () => {
      await renderDeleted({
        answers: { [`GET ${deletedExpensesPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') }).getAttribute('href')).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it('shows the not-found state for a 404 without an error code', async () => {
      await renderDeleted({
        answers: { [`GET ${deletedExpensesPath}`]: () => new Response(null, { status: 404 }) },
      })

      await screen.findByRole('alert')

      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it.each([
      [404, 'GROUP_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the deleted expenses only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderDeleted({
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${deletedExpensesPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', deletedExpensesPath)).toBe(1)
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderDeleted({ answers: { [`GET ${deletedExpensesPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })
  })

  it('keeps the deleted expenses in the cache under the key groups, the group id, expenses and deleted', async () => {
    const { queryClient } = await renderDeleted()
    await showsRows()

    expect(queryClient.getQueryData(['groups', testGroupId, 'expenses', 'deleted'])).toEqual(deletedRows)
  })

  it('asks only for the deleted expenses, never for the whole expenses list', async () => {
    const { fetchMock } = await renderDeleted()
    await showsRows()

    expect(requestCount(fetchMock, 'GET', deletedExpensesPath)).toBe(1)
    expect(requestCount(fetchMock, 'GET', expensesPath)).toBe(0)
  })
})
