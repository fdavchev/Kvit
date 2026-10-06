import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { ExpenseDetail } from '@/core/services/expenses/expensesService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { formatMoney } from '@/shared/utils/formatMoney'
import {
  accommodationCategory,
  detailOf,
  groceriesDetail,
  historyDetail,
  hotelDetail,
  hotelExpenseId,
  otherCategory,
  testCategories,
} from '@/test/expenseTestData'
import {
  categoriesPath,
  categoryName,
  elementWithAll,
  expensePathOf,
  expenseRestorePathOf,
  findErrorText,
  freezeTime,
  groupPath,
  macedonianDay,
  membersPath,
  plainSpaces,
  relativeTime,
  unfreezeTime,
  restoreRealRetryPolicy,
} from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import {
  anaMember,
  bojanRemoved,
  filipMember,
  filipPictureUrl,
  grandmaMember,
  markoMember,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
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
import {
  actionOf,
  pressToastAction,
  referenceToastStyling,
  stylingOf,
  toastShownWithText,
  toastsShownWithOptions,
} from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { ExpenseDetailScreen } from './ExpenseDetailScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const detailPath = expensePathOf(hotelExpenseId)
const restorePath = expenseRestorePathOf(hotelExpenseId)

interface RenderOptions {
  language?: Language
  detail?: ExpenseDetail
  members?: GroupMembers
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderDetail(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const detail = options.detail ?? hotelDetail
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: jsonAnswer(options.members ?? ownerViewMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${expensePathOf(detail.id)}`]: jsonAnswer(detail),
    [`DELETE ${expensePathOf(detail.id)}`]: noContentAnswer(),
    [`POST ${expenseRestorePathOf(detail.id)}`]: noContentAnswer(),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/:expenseId', element: <ExpenseDetailScreen /> },
      { path: '/groups/:groupId/expenses/:expenseId/edit', element: <p>edit expense page</p> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpense(testGroupId, detail.id),
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

async function showsDetail(title = 'Hotel'): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: title })
}

function deleteButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'expense.delete') })
}

function editLink(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: translated(language, 'expense.edit') })
}

function money(amountMinor: number, currency: 'MKD' | 'EUR' = 'MKD', language: Language = 'en'): string {
  return plainSpaces(formatMoney(amountMinor, currency, language))
}

const maxRowExtra = 12

describe('ExpenseDetailScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('keeps the detail in the cache under the key groups, the group id, expenses and the expense id', async () => {
    const { queryClient } = await renderDetail()
    await showsDetail()

    expect(queryClient.getQueryData(['groups', testGroupId, 'expenses', hotelExpenseId])).toEqual(hotelDetail)
  })

  describe('the header', () => {
    it('has a back button to the group', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.getByRole('link', { name: translated('en', 'common.back') }).getAttribute('href')).toBe(
        routes.group(testGroupId),
      )
    })

    it('shows no bottom bar', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.queryByRole('navigation')).toBeNull()
    })

    it('shows the loading spinner while the expense is being asked', async () => {
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/expenses/:expenseId', element: <ExpenseDetailScreen /> }],
        routes.groupExpense(testGroupId, hotelExpenseId),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it('shows the title as the heading', async () => {
      await renderDetail()

      expect(await screen.findByRole('heading', { level: 1, name: 'Hotel' })).toBeTruthy()
    })

    it('shows the name of the category as the heading when the expense has no title', async () => {
      await renderDetail({ detail: detailOf({ title: null, categoryId: otherCategory.id }) })

      expect(
        await screen.findByRole('heading', { level: 1, name: categoryName('en', otherCategory) }),
      ).toBeTruthy()
    })

    it('shows the emoji of the category', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.getByText(accommodationCategory.emoji)).toBeTruthy()
    })

    it('shows the amount in the currency of the expense', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.getByText(money(300000), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('shows the amount of an EUR expense in euros', async () => {
      await renderDetail({ detail: groceriesDetail })
      await showsDetail('Groceries')

      expect(screen.getByText('€45.00')).toBeTruthy()
    })

    it.each(languages)('shows who paid and the date (%s)', async (language) => {
      await renderDetail({ language })
      await showsDetail()

      const dateText = language === 'en' ? '3 Oct' : macedonianDay('2026-10-03', false)

      expect(
        screen.getByText(translated(language, 'expense.paidByOn', { name: 'Filip', date: dateText })),
      ).toBeTruthy()
    })

    it('shows the note when the expense has one', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.getByText('Booked for the whole weekend')).toBeTruthy()
    })

    it('shows no empty note line when the expense has no note', async () => {
      await renderDetail({ detail: detailOf({ note: null }) })
      await showsDetail()

      expect(screen.queryByText('Booked for the whole weekend')).toBeNull()
      expect(screen.queryByText('null')).toBeNull()
    })
  })

  describe('the split', () => {
    it('shows every person with their share in the currency of the expense', async () => {
      await renderDetail()
      await showsDetail()

      expect(elementWithAll(['Filip', money(48000)], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Ana', money(48000)], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Marko', money(108000)], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Grandma', money(48000)], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Petar', money(48000)], maxRowExtra)).toBeTruthy()
    })

    it('shows the shares of an EUR expense in euros', async () => {
      await renderDetail({ detail: groceriesDetail })
      await showsDetail('Groceries')

      expect(elementWithAll(['Filip', '€15.00'], maxRowExtra)).toBeTruthy()
      expect(elementWithAll(['Marko', '€15.00'], maxRowExtra)).toBeTruthy()
    })

    it('shows the people in the order of the split', async () => {
      await renderDetail()
      await showsDetail()

      const rows = ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar'].map((name) =>
        elementWithAll([name, money(name === 'Marko' ? 108000 : 48000)], maxRowExtra),
      )
      for (let index = 1; index < rows.length; index += 1) {
        expect(rows[index - 1].compareDocumentPosition(rows[index]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
      }
    })

    it('shows the Google picture of a person who has one and the initial of the others in their row', async () => {
      await renderDetail({
        members: { ...ownerViewMembers, members: [{ ...filipMember, pictureUrl: filipPictureUrl }, anaMember, markoMember, grandmaMember, petarMember] },
      })
      await showsDetail()

      const filipRow = elementWithAll(['Filip', money(48000)], maxRowExtra)
      const anaRow = elementWithAll(['Ana', money(48000)], maxRowExtra)

      expect(filipRow.querySelector('img')?.getAttribute('src')).toBe(filipPictureUrl)
      expect(filipRow.querySelector('img')?.getAttribute('referrerpolicy')).toBe('no-referrer')
      expect(anaRow.querySelector('img')).toBeNull()
    })

    it('gives every person the colour of their place in the joining order', async () => {
      await renderDetail()
      await showsDetail()

      expect(elementWithAll(['Filip', money(48000)], maxRowExtra).innerHTML).toMatch(/--avatar-0(?!\d)/)
      expect(elementWithAll(['Ana', money(48000)], maxRowExtra).innerHTML).toMatch(/--avatar-1(?!\d)/)
      expect(elementWithAll(['Marko', money(108000)], maxRowExtra).innerHTML).toMatch(/--avatar-2(?!\d)/)
    })
  })

  describe('a person who is no longer in the group', () => {
    const removedShare = { memberId: bojanRemoved.id, name: 'Bojan', inputValue: 0, shareMinor: 7000 }

    it('shows the neutral colour with the number 9 and the initial, never a picture, in the row of the person', async () => {
      await renderDetail({
        detail: detailOf({
          shares: [...hotelDetail.shares.slice(0, 2), removedShare],
        }),
      })
      await showsDetail()

      const row = elementWithAll(['Bojan', money(7000)], maxRowExtra)

      expect(row.innerHTML).toMatch(/--avatar-9(?!\d)/)
      expect(row.querySelector('img')).toBeNull()
      expect(row.textContent).toContain('B')
    })

    it('still shows their name and share', async () => {
      await renderDetail({
        detail: detailOf({
          shares: [...hotelDetail.shares.slice(0, 2), removedShare],
        }),
      })
      await showsDetail()

      expect(elementWithAll(['Bojan', money(7000)], maxRowExtra)).toBeTruthy()
    })
  })

  describe('the exchange rate', () => {
    it('shows the saved rate and its date for an EUR expense in English', async () => {
      await renderDetail({ detail: groceriesDetail })
      await showsDetail('Groceries')

      expect(
        screen.getByText('Rate: 1 EUR = 61.5610 MKD · 24 Sep 2026'),
      ).toBeTruthy()
    })

    it('shows the saved rate for an EUR expense in Macedonian with the word «Цена на еврото», a decimal comma and the date the way Intl writes it', async () => {
      await renderDetail({ language: 'mk', detail: groceriesDetail })
      await showsDetail('Groceries')

      expect(
        screen.getByText(
          translated('mk', 'expense.rate', { rate: '61,5610', date: macedonianDay('2026-09-24', true) }),
        ),
      ).toBeTruthy()
    })

    it('writes the rate with four decimals even when the last ones are zeros', async () => {
      await renderDetail({ detail: { ...groceriesDetail, mkdPerEur: 61.5 } })
      await showsDetail('Groceries')

      expect(screen.getByText(/1 EUR = 61\.5000 MKD/)).toBeTruthy()
    })

    it('uses the date of the rate, not the date of the expense', async () => {
      await renderDetail({ detail: { ...groceriesDetail, rateDate: '2026-10-01' } })
      await showsDetail('Groceries')

      expect(screen.getByText(/1 EUR = 61\.5610 MKD · 1 Oct 2026/)).toBeTruthy()
    })

    it('shows no rate line for an MKD expense', async () => {
      await renderDetail()
      await showsDetail()

      expect(screen.queryByText(/1 EUR =/)).toBeNull()
    })
  })

  describe('the history', () => {
    interface Sentences {
      restored: string
      deleted: string
      amount: string
      title: string
      category: string
      note: string
      date: string
      paidBy: string
      currency: string
      split: string
      added: string
      noSplitDetails: RegExp
    }

    function sentencesIn(language: Language): Sentences {
      const dash = '—'
      const day = (date: string): string =>
        language === 'en' ? `${Number(date.slice(8))} Oct` : macedonianDay(date, false)
      return {
        restored: translated(language, 'expense.historyRestored', { name: 'Filip' }),
        deleted: translated(language, 'expense.historyDeleted', { name: 'Filip' }),
        amount: translated(language, 'expense.historyChangedAmount', {
          name: 'Ana',
          old: formatMoney(280000, 'MKD', language),
          new: formatMoney(300000, 'MKD', language),
        }),
        title: translated(language, 'expense.historyChangedTitle', { name: 'Ana', old: dash, new: 'Hotel' }),
        category: translated(language, 'expense.historyChangedCategory', {
          name: 'Ana',
          old: translated(language, 'categories.food'),
          new: translated(language, 'categories.accommodation'),
        }),
        note: translated(language, 'expense.historyChangedNote', { name: 'Filip', old: 'Booked', new: dash }),
        date: translated(language, 'expense.historyChangedDate', {
          name: 'Filip',
          old: day('2026-10-02'),
          new: day('2026-10-03'),
        }),
        paidBy: translated(language, 'expense.historyChangedPaidBy', { name: 'Filip', old: 'Ana', new: 'Filip' }),
        currency: translated(language, 'expense.historyChangedCurrency', { name: 'Filip', old: 'EUR', new: 'MKD' }),
        split: translated(language, 'expense.historyChangedSplit', { name: 'Filip' }),
        added: translated(language, 'expense.historyAdded', { name: 'Bojan' }),
        noSplitDetails: /Equal: Ana/,
      }
    }

    function sentence(text: string): HTMLElement {
      return screen.getByText(plainSpaces(text), { normalizer: plainSpaces })
    }

    it.each(languages)('has a History heading (%s)', async (language) => {
      await renderDetail({ language })
      await showsDetail()

      expect(screen.getByText(translated(language, 'expense.history'))).toBeTruthy()
    })

    it.each(languages)('writes every kind of entry as a sentence with the name of the person (%s)', async (language) => {
      await renderDetail({ language, detail: historyDetail })
      await showsDetail()
      const sentences = sentencesIn(language)

      for (const text of [
        sentences.restored,
        sentences.deleted,
        sentences.amount,
        sentences.title,
        sentences.category,
        sentences.note,
        sentences.date,
        sentences.paidBy,
        sentences.currency,
        sentences.split,
        sentences.added,
      ]) {
        expect(sentence(text)).toBeTruthy()
      }
    })

    it('writes the English sentences the way Filip approved them', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()

      expect(sentence('Bojan added it')).toBeTruthy()
      expect(sentence('Ana changed the amount: 2,800 MKD → 3,000 MKD')).toBeTruthy()
      expect(sentence('Ana changed the title: — → Hotel')).toBeTruthy()
      expect(sentence('Ana changed the category: Food & drinks → Accommodation')).toBeTruthy()
      expect(sentence('Filip changed the note: Booked → —')).toBeTruthy()
      expect(sentence('Filip changed the date: 2 Oct → 3 Oct')).toBeTruthy()
      expect(sentence('Filip changed who paid: Ana → Filip')).toBeTruthy()
      expect(sentence('Filip changed the currency: EUR → MKD')).toBeTruthy()
      expect(sentence('Filip changed the split')).toBeTruthy()
      expect(sentence('Filip deleted it')).toBeTruthy()
      expect(sentence('Filip restored it')).toBeTruthy()
    })

    it('shows the amount of a change through formatMoney in the current currency of the expense', async () => {
      await renderDetail({
        detail: {
          ...historyDetail,
          currency: 'EUR',
          amountMinor: 4500,
          shares: groceriesDetail.shares,
          history: [
            {
              type: 'ExpenseEdited',
              actorName: 'Ana',
              createdAt: '2026-10-03T10:00:00Z',
              changes: [{ field: 'amount', old: '4000', new: '4500' }],
            },
          ],
        },
      })
      await showsDetail()

      expect(
        sentence(translated('en', 'expense.historyChangedAmount', { name: 'Ana', old: '€40.00', new: '€45.00' })),
      ).toBeTruthy()
    })

    it('shows an empty old or new value as a dash', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()

      expect(sentence(sentencesIn('en').title).textContent).toContain('—')
      expect(sentence(sentencesIn('en').note).textContent).toContain('—')
    })

    it('shows a category through its translated name, never the key', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()

      expect(screen.queryByText(/changed the category: food/)).toBeNull()
      expect(sentence(sentencesIn('en').category)).toBeTruthy()
    })

    it('shows the split change as a sentence only, without the old and new text of the server', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()

      expect(sentence(sentencesIn('en').split).textContent).not.toContain('→')
      expect(screen.queryByText(sentencesIn('en').noSplitDetails)).toBeNull()
    })

    it('shows only the fields that changed in an entry', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()

      expect(screen.queryByText(/Ana changed the note/)).toBeNull()
      expect(screen.queryByText(/Ana changed the date/)).toBeNull()
      expect(screen.queryByText(/Filip changed the amount/)).toBeNull()
    })

    it.each(languages)('writes how long ago a recent entry was with Intl.RelativeTimeFormat in minutes, hours and days (%s)', async (language) => {
      await renderDetail({ language, detail: historyDetail })
      await showsDetail()
      const sentences = sentencesIn(language)

      expect(elementWithAll([sentences.restored, relativeTime(language, -5, 'minute')], 40)).toBeTruthy()
      expect(elementWithAll([sentences.deleted, relativeTime(language, -2, 'hour')], 40)).toBeTruthy()
      expect(elementWithAll([sentences.amount, relativeTime(language, -2, 'day')], 40)).toBeTruthy()
    })

    it.each(languages)('writes the date instead of a relative time for an entry older than seven days (%s)', async (language) => {
      await renderDetail({ language, detail: historyDetail })
      await showsDetail()
      const sentences = sentencesIn(language)
      const olderDay = language === 'en' ? '28 Sep' : macedonianDay('2026-09-28', false)
      const oldestDay = language === 'en' ? '20 Sep' : macedonianDay('2026-09-20', false)

      expect(elementWithAll([sentences.paidBy, olderDay], 40)).toBeTruthy()
      expect(elementWithAll([sentences.added, oldestDay], 40)).toBeTruthy()
      expect(screen.queryByText(relativeTime(language, -8, 'day'))).toBeNull()
    })

    it('shows the newest entry first and the first entry last', async () => {
      await renderDetail({ detail: historyDetail })
      await showsDetail()
      const sentences = sentencesIn('en')
      const order = [sentences.restored, sentences.deleted, sentences.amount, sentences.paidBy, sentences.added].map(
        (text) => sentence(text),
      )

      for (let index = 1; index < order.length; index += 1) {
        expect(
          order[index - 1].compareDocumentPosition(order[index]) & Node.DOCUMENT_POSITION_FOLLOWING,
        ).toBeTruthy()
      }
    })

    it('shows an expense that was never changed with only its first entry', async () => {
      await renderDetail({ detail: groceriesDetail })
      await showsDetail('Groceries')

      expect(screen.getByText(translated('en', 'expense.history'))).toBeTruthy()
      expect(sentence(translated('en', 'expense.historyAdded', { name: 'Filip' }))).toBeTruthy()
    })
  })

  describe('the Edit and Delete buttons', () => {
    it.each(languages)('shows Edit as a link to the Edit screen and Delete for a person who may edit (%s)', async (language) => {
      await renderDetail({ language })
      await showsDetail()

      expect(editLink(language).getAttribute('href')).toBe(routes.groupExpenseEdit(testGroupId, hotelExpenseId))
      expect(deleteButton(language)).toBeTruthy()
    })

    it('shows neither Edit nor Delete when the person may not edit the expense', async () => {
      await renderDetail({ detail: detailOf({ canEdit: false }) })
      await showsDetail()

      expect(screen.queryByRole('link', { name: translated('en', 'expense.edit') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'expense.delete') })).toBeNull()
    })

    it('opens the Edit screen when Edit is pressed', async () => {
      const { router } = await renderDetail()
      await showsDetail()

      fireEvent.click(editLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpenseEdit(testGroupId, hotelExpenseId))
      })
    })
  })

  describe('deleting', () => {
    it('sends the delete request for the expense with no body', async () => {
      const { fetchMock } = await renderDetail()
      await showsDetail()

      fireEvent.click(deleteButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'DELETE', detailPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'DELETE', detailPath)[0].body).toBeUndefined()
    })

    it('asks no "are you sure" question before deleting', async () => {
      await renderDetail()
      await showsDetail()

      fireEvent.click(deleteButton())

      expect(screen.queryByRole('dialog')).toBeNull()
      expect(screen.queryByRole('alertdialog')).toBeNull()
    })

    it('goes back to the group after the expense is deleted', async () => {
      const { router } = await renderDetail()
      await showsDetail()

      fireEvent.click(deleteButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(screen.getByText('group page')).toBeTruthy()
    })

    it.each(languages)('shows the Expense deleted toast with an Undo button for 4 seconds (%s)', async (language) => {
      await renderDetail({ language })
      await showsDetail()

      fireEvent.click(deleteButton(language))

      const shown = await waitFor(() => toastShownWithText(translated(language, 'expenses.deleted')))
      expect(shown.options.duration).toBe(4000)
      expect(actionOf(shown).label).toBe(translated(language, 'common.undo'))
    })

    it('shows the Expense deleted toast in the wine-red danger style', async () => {
      const dangerStyling = referenceToastStyling(true)
      await renderDetail()
      await showsDetail()

      fireEvent.click(deleteButton())

      const shown = await waitFor(() => toastShownWithText(translated('en', 'expenses.deleted')))
      expect(stylingOf(shown)).toEqual(dangerStyling)
    })

    it('restores the expense when Undo is pressed on the toast', async () => {
      const { fetchMock } = await renderDetail()
      await showsDetail()
      fireEvent.click(deleteButton())
      const shown = await waitFor(() => toastShownWithText(translated('en', 'expenses.deleted')))

      pressToastAction(shown)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', restorePath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', restorePath)[0].body).toBeUndefined()
    })

    it('does not restore the expense before Undo is pressed', async () => {
      const { fetchMock } = await renderDetail()
      await showsDetail()

      fireEvent.click(deleteButton())
      await waitFor(() => {
        expect(requestCount(fetchMock, 'DELETE', detailPath)).toBe(1)
      })

      expect(requestCount(fetchMock, 'POST', restorePath)).toBe(0)
    })

    it('shows the translated error when the restore after Undo fails', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderDetail({
        answers: { [`POST ${restorePath}`]: problemAnswer(400, 'EXPENSE_RESTORE_EXPIRED') },
      })
      await showsDetail()
      fireEvent.click(deleteButton())
      const shown = await waitFor(() => toastShownWithText(translated('en', 'expenses.deleted')))

      pressToastAction(shown)

      await findErrorText(translated('en', 'errors.EXPENSE_RESTORE_EXPIRED'))
    })

    it('marks every expense query of the group as out of date after the delete', async () => {
      const keys = [
        ['groups', testGroupId, 'expenses'],
        ['groups', testGroupId, 'expenses', 'deleted'],
      ]
      const { queryClient } = await renderDetail({
        seedCache: (cache) => {
          for (const key of keys) {
            cache.setQueryData(key, [])
          }
        },
      })
      await showsDetail()

      fireEvent.click(deleteButton())

      await waitFor(() => {
        for (const key of keys) {
          expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
        }
      })
    })

    it('marks the activity of the group as out of date after the delete, so the deletion shows up in it', async () => {
      const key = ['groups', testGroupId, 'activity']
      const { queryClient } = await renderDetail({
        seedCache: (cache) => {
          cache.setQueryData(key, [])
        },
      })
      await showsDetail()

      fireEvent.click(deleteButton())

      await waitFor(() => {
        expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
      })
    })

    it('marks the activity of the group as out of date after Undo restored the expense, so the restore shows up in it', async () => {
      const key = ['groups', testGroupId, 'activity']
      const { queryClient, fetchMock } = await renderDetail({
        seedCache: (cache) => {
          cache.setQueryData(key, [])
        },
      })
      await showsDetail()
      fireEvent.click(deleteButton())
      const shown = await waitFor(() => toastShownWithText(translated('en', 'expenses.deleted')))
      await waitFor(() => {
        expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
      })
      queryClient.setQueryData(key, [])
      expect(queryClient.getQueryState(key)?.isInvalidated).toBe(false)

      pressToastAction(shown)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', restorePath)).toBe(1)
      })
      await waitFor(() => {
        expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
      })
    })

    it('disables Delete while the request waits for an answer', async () => {
      await renderDetail()
      await showsDetail()
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      fireEvent.click(deleteButton())

      await expectDisabledWhilePending(deleteButton())
    })

    it.each([
      [403, 'EXPENSE_NOT_ALLOWED'],
      [404, 'EXPENSE_NOT_FOUND'],
    ])('shows the translated text for %i %s, stays on the screen and shows no Expense deleted toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router } = await renderDetail({
        answers: { [`DELETE ${detailPath}`]: problemAnswer(status, code) },
      })
      await showsDetail()

      fireEvent.click(deleteButton())

      await findErrorText(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.groupExpense(testGroupId, hotelExpenseId))
      expect(toastsShownWithOptions()).toEqual([])
    })

    it('shows the network message when the delete cannot reach the server', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderDetail({ answers: { [`DELETE ${detailPath}`]: networkFailureAnswer() } })
      await showsDetail()

      fireEvent.click(deleteButton())

      await findErrorText(translated('en', 'errors.network'))
    })
  })

  describe('loading and failing', () => {
    it.each(languages)('shows the EXPENSE_NOT_FOUND text with a Go to Groups link and no retry button (%s)', async (language) => {
      await renderDetail({
        language,
        answers: { [`GET ${detailPath}`]: problemAnswer(404, 'EXPENSE_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.EXPENSE_NOT_FOUND'),
      )
      expect(screen.getByRole('link', { name: translated(language, 'groups.goToGroups') }).getAttribute('href')).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated(language, 'common.retry') })).toBeNull()
    })

    it.each(languages)('shows the EXPENSE_NOT_FOUND text for a 404 without an error code, like an expense id that is not a guid (%s)', async (language) => {
      await renderDetail({
        language,
        answers: { [`GET ${detailPath}`]: () => new Response(null, { status: 404 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.EXPENSE_NOT_FOUND'),
      )
      expect(screen.getByRole('link', { name: translated(language, 'groups.goToGroups') }).getAttribute('href')).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated(language, 'common.retry') })).toBeNull()
    })

    it('shows the GROUP_NOT_FOUND text with a Go to Groups link when the person is not in the group', async () => {
      await renderDetail({
        answers: { [`GET ${detailPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.GROUP_NOT_FOUND'),
      )
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
    })

    it('shows the generic error with a retry button after a server error and loads the expense after a retry', async () => {
      let tries = 0
      await renderDetail({
        answers: {
          [`GET ${detailPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(hotelDetail)
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      await showsDetail()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it.each([
      [404, 'EXPENSE_NOT_FOUND'],
      [429, 'RATE_LIMITED'],
    ])('asks for the expense only once after a %i answer, with the retry policy of the app', async (status, code) => {
      const { fetchMock } = await renderDetail({
        seedCache: restoreRealRetryPolicy,
        answers: { [`GET ${detailPath}`]: problemAnswer(status, code) },
      })

      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'GET', detailPath)).toBe(1)
    })

    it('shows the retry button and no Go to Groups link after a rate limit answer', async () => {
      await renderDetail({ answers: { [`GET ${detailPath}`]: problemAnswer(429, 'RATE_LIMITED') } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.RATE_LIMITED'))
      expect(screen.queryByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeNull()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderDetail({ answers: { [`GET ${detailPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })
  })

  describe('in Macedonian', () => {
    it('writes the amounts, the category name, the buttons and the history heading in Macedonian', async () => {
      await renderDetail({ language: 'mk' })
      await showsDetail()

      expect(screen.getByText(money(300000, 'MKD', 'mk'), { normalizer: plainSpaces })).toBeTruthy()
      expect(screen.getByText(translated('mk', 'expense.history'))).toBeTruthy()
      expect(screen.getByRole('link', { name: translated('mk', 'expense.edit') })).toBeTruthy()
      expect(screen.getByRole('button', { name: translated('mk', 'expense.delete') })).toBeTruthy()
      expect(screen.getByText(accommodationCategory.emoji)).toBeTruthy()
      expect(categoryName('mk', accommodationCategory)).toMatch(/[Ѐ-ӿ]/)
    })
  })
})
