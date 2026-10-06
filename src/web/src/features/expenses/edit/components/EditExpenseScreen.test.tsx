import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { ExpenseDetail } from '@/core/services/expenses/expensesService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import {
  accommodationCategory,
  detailOf,
  exactSplitDetail,
  groceriesDetail,
  hotelDetail,
  hotelExpenseId,
  testCategories,
} from '@/test/expenseTestData'
import {
  amountField,
  currencyToggle,
  doneButton,
  formRow,
  isDisabled,
  noteField,
  noteLink,
  openSheet,
  personButton,
  personGroup,
  saveButton,
  sentBody,
  splitTab,
  titleField,
  typeAmount,
  typeInPerson,
  typeTitle,
  waitForSheetToClose,
} from '@/test/expenseFormTestHelpers'
import {
  categoriesPath,
  categoryName,
  expensePathOf,
  expensesPath,
  freezeTime,
  groupPath,
  membersPath,
  unfreezeTime,
} from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import {
  anaMember,
  filipMember,
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
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { EditExpenseScreen } from './EditExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const detailPath = expensePathOf(hotelExpenseId)
const editRoute = routes.groupExpenseEdit(testGroupId, hotelExpenseId)
const detailRoute = routes.groupExpense(testGroupId, hotelExpenseId)

interface RenderOptions {
  language?: Language
  detail?: ExpenseDetail
  members?: GroupMembers
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderEdit(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const detail = options.detail ?? hotelDetail
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(groupOf({ memberCount: 5 })),
    [`GET ${membersPath}`]: jsonAnswer(options.members ?? ownerViewMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`GET ${expensePathOf(detail.id)}`]: jsonAnswer(detail),
    [`PUT ${expensePathOf(detail.id)}`]: noContentAnswer(),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/:expenseId/edit', element: <EditExpenseScreen /> },
      { path: '/groups/:groupId/expenses/:expenseId', element: <p>expense detail page</p> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpenseEdit(testGroupId, detail.id),
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

async function renderReadyEdit(options: RenderOptions = {}) {
  const rendered = await renderEdit(options)
  await screen.findByRole('button', { name: translated(options.language ?? 'en', 'groupSettings.save') })
  return rendered
}

function sharesOf(body: Record<string, unknown>): { memberId: string; inputValue: number }[] {
  const shares = body.shares
  if (!Array.isArray(shares)) {
    throw new Error(`Expected the body to have a list of shares, found ${JSON.stringify(shares)}`)
  }
  return shares as { memberId: string; inputValue: number }[]
}

function revealedNoteField(): HTMLInputElement | HTMLTextAreaElement {
  const shown = screen.queryByLabelText<HTMLInputElement | HTMLTextAreaElement>(
    translated('en', 'expense.note'),
  )
  if (shown !== null) {
    return shown
  }
  fireEvent.click(noteLink())
  return noteField()
}

describe('EditExpenseScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('the layout', () => {
    it.each(languages)('shows the Edit expense heading (%s)', async (language) => {
      await renderReadyEdit({ language })

      expect(
        screen.getByRole('heading', { level: 1, name: translated(language, 'expense.editTitle') }),
      ).toBeTruthy()
    })

    it('has a back button to the detail of the expense, not to the group', async () => {
      await renderReadyEdit()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(detailRoute)
    })

    it('shows no bottom bar', async () => {
      await renderReadyEdit()

      expect(screen.queryByRole('navigation')).toBeNull()
    })

    it('shows the loading spinner while the expense is being asked', async () => {
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/expenses/:expenseId/edit', element: <EditExpenseScreen /> }],
        editRoute,
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })
  })

  describe('prefilled from the detail', () => {
    it('shows the amount in whole denars for an MKD expense', async () => {
      await renderReadyEdit()

      expect(parseMoneyInput(amountField().value, 'MKD')).toBe(300000)
      expect(currencyToggle('MKD')).toBeTruthy()
    })

    it('shows the amount of an EUR expense so that it reads back as the same cents', async () => {
      await renderReadyEdit({ detail: groceriesDetail })

      expect(parseMoneyInput(amountField().value, 'EUR')).toBe(4500)
      expect(currencyToggle('EUR')).toBeTruthy()
    })

    it('shows the title', async () => {
      await renderReadyEdit()

      expect(titleField().value).toBe('Hotel')
    })

    it('shows an empty title field for an expense without a title', async () => {
      await renderReadyEdit({ detail: detailOf({ title: null }) })

      expect(titleField().value).toBe('')
    })

    it('shows the note in the note field', async () => {
      await renderReadyEdit()

      expect(revealedNoteField().value).toBe('Booked for the whole weekend')
    })

    it('shows the payer in the Paid by row', async () => {
      await renderReadyEdit({ detail: detailOf({ paidByMemberId: anaMember.id, paidByName: 'Ana' }) })

      expect(formRow('paidBy').textContent).toContain('Ana')
    })

    it('shows the category with its emoji and name in the Category row', async () => {
      await renderReadyEdit()

      const text = formRow('category').textContent ?? ''

      expect(text).toContain(accommodationCategory.emoji)
      expect(text).toContain(categoryName('en', accommodationCategory))
    })

    it('shows the date of the expense in the Date row, not Today', async () => {
      await renderReadyEdit()

      const text = formRow('date').textContent ?? ''

      expect(text).toContain('3')
      expect(text).not.toContain(translated('en', 'expenses.today'))
    })

    it('shows the Equal tab with everybody ticked and the extra of Marko in the Split sheet', async () => {
      await renderReadyEdit()

      const sheet = await openSheet('split')

      expect(splitTab(sheet, 'splitEqual').getAttribute('aria-selected')).toBe('true')
      for (const name of ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar']) {
        expect(personButton(sheet, name).getAttribute('aria-pressed')).toBe('true')
      }
      const extra = within(personGroup(sheet, 'Marko')).getByRole<HTMLInputElement>('textbox')
      expect(parseMoneyInput(extra.value, 'MKD')).toBe(60000)
    })

    it('shows the Exact tab with the typed amounts of an Exact split', async () => {
      await renderReadyEdit({ detail: exactSplitDetail })

      const sheet = await openSheet('split')

      expect(splitTab(sheet, 'splitExact').getAttribute('aria-selected')).toBe('true')
      for (const name of ['Filip', 'Ana', 'Marko']) {
        const field = within(personGroup(sheet, name)).getByRole<HTMLInputElement>('textbox')
        expect(parseMoneyInput(field.value, 'MKD')).toBe(100000)
      }
    })

    it('leaves a member who was not in the split unticked', async () => {
      const detail = detailOf({
        splitType: 'Equal',
        amountMinor: 100000,
        shares: [
          { memberId: filipMember.id, name: 'Filip', inputValue: 0, shareMinor: 50000 },
          { memberId: anaMember.id, name: 'Ana', inputValue: 0, shareMinor: 50000 },
        ],
      })
      await renderReadyEdit({ detail })

      const sheet = await openSheet('split')

      expect(personButton(sheet, 'Filip').getAttribute('aria-pressed')).toBe('true')
      expect(personButton(sheet, 'Marko').getAttribute('aria-pressed')).toBe('false')
    })
  })

  describe('saving', () => {
    it('sends PUT to the expense with the unchanged values and no clientRequestId', async () => {
      const { fetchMock } = await renderReadyEdit()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'PUT', detailPath)[0]
      expect(request.contentType).toBe('application/json')
      const body = sentBody(fetchMock, 'PUT', detailPath)
      expect(body).not.toHaveProperty('clientRequestId')
      expect(body).toMatchObject({
        title: 'Hotel',
        note: 'Booked for the whole weekend',
        amountMinor: 300000,
        currency: 'MKD',
        expenseDate: '2026-10-03',
        categoryId: accommodationCategory.id,
        paidByMemberId: filipMember.id,
        splitType: 'Equal',
      })
      expect(sharesOf(body)).toEqual(
        hotelDetail.shares.map((share) => ({ memberId: share.memberId, inputValue: share.inputValue })),
      )
    })

    it('never creates an expense', async () => {
      const { fetchMock } = await renderReadyEdit()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
      })
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    })

    it('sends the new amount after it was changed', async () => {
      const { fetchMock } = await renderReadyEdit()
      typeAmount('3500')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'PUT', detailPath).amountMinor).toBe(350000)
      })
    })

    it('sends the new title and note after they were changed', async () => {
      const { fetchMock } = await renderReadyEdit()
      typeTitle('Hotel by the sea')
      fireEvent.change(revealedNoteField(), { target: { value: 'Paid in cash' } })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'PUT', detailPath)).toMatchObject({ title: 'Hotel by the sea', note: 'Paid in cash' })
      })
    })

    it('sends an empty title as null or as an empty text after the title was cleared', async () => {
      const { fetchMock } = await renderReadyEdit()
      typeTitle('')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
      })
      expect(sentBody(fetchMock, 'PUT', detailPath).title ?? '').toBe('')
    })

    it('sends the new payer after another person was picked', async () => {
      const { fetchMock } = await renderReadyEdit()
      const sheet = await openSheet('paidBy')
      expect(personButton(sheet, 'Filip').getAttribute('aria-pressed')).toBe('true')
      fireEvent.click(personButton(sheet, 'Ana'))
      await waitForSheetToClose()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'PUT', detailPath).paidByMemberId).toBe(anaMember.id)
      })
    })

    it('sends the new currency and amount after the currency was switched from EUR to MKD', async () => {
      const { fetchMock } = await renderReadyEdit({ detail: groceriesDetail })
      fireEvent.click(currencyToggle('EUR'))
      typeAmount('2400')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'PUT', expensePathOf(groceriesDetail.id))).toMatchObject({
          amountMinor: 240000,
          currency: 'MKD',
        })
      })
    })

    it('sends the new amount in cents after the currency was switched from MKD to EUR, with the extras cleared', async () => {
      const { fetchMock } = await renderReadyEdit()
      fireEvent.click(currencyToggle('MKD'))
      typeAmount('45.50')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'PUT', detailPath)).toMatchObject({ amountMinor: 4550, currency: 'EUR' })
      })
      expect(sharesOf(sentBody(fetchMock, 'PUT', detailPath)).every((share) => share.inputValue === 0)).toBe(true)
    })

    it('keeps the typed amount text when the currency is switched', async () => {
      await renderReadyEdit()

      fireEvent.click(currencyToggle('MKD'))

      expect(amountField().value).toBe('3000')
    })

    it('sends the changed split: Marko without the extra and Ana out of the split', async () => {
      const { fetchMock } = await renderReadyEdit()
      const sheet = await openSheet('split')
      typeInPerson(sheet, 'Marko', '0')
      fireEvent.click(personButton(sheet, 'Ana'))
      expect(isDisabled(doneButton(sheet))).toBe(false)
      fireEvent.click(doneButton(sheet))
      await waitForSheetToClose()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
      })
      expect(sharesOf(sentBody(fetchMock, 'PUT', detailPath))).toEqual([
        { memberId: filipMember.id, inputValue: 0 },
        { memberId: markoMember.id, inputValue: 0 },
        { memberId: grandmaMember.id, inputValue: 0 },
        { memberId: petarMember.id, inputValue: 0 },
      ])
    })

    it('sends the typed amounts of an Exact split unchanged', async () => {
      const { fetchMock } = await renderReadyEdit({ detail: exactSplitDetail })

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(1)
      })
      const body = sentBody(fetchMock, 'PUT', detailPath)
      expect(body.splitType).toBe('Exact')
      expect(sharesOf(body).map((share) => share.inputValue)).toEqual([100000, 100000, 100000])
    })

    it('does not send an empty amount', async () => {
      const { fetchMock } = await renderReadyEdit()
      typeAmount('')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(isDisabled(saveButton()) || screen.queryByRole('alert') !== null).toBe(true)
      })
      expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(0)
    })

    it('goes back to the detail of the expense after it was saved', async () => {
      const { router } = await renderReadyEdit()

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(detailRoute)
      })
      expect(screen.getByText('expense detail page')).toBeTruthy()
    })

    it.each(languages)('shows the plain Expense saved toast after the edit was saved (%s)', async (language) => {
      await renderReadyEdit({ language })

      fireEvent.click(saveButton(language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(translated(language, 'expenses.saved'))
      })
    })

    it('shows no Expense saved toast when the save failed', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderReadyEdit({ answers: { [`PUT ${detailPath}`]: problemAnswer(403, 'EXPENSE_NOT_ALLOWED') } })

      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(shownToastTexts()).not.toContain(translated('en', 'expenses.saved'))
    })

    it('marks the expenses list and the deleted list of the group as out of date after it was saved', async () => {
      const keys = [
        ['groups', testGroupId, 'expenses'],
        ['groups', testGroupId, 'expenses', 'deleted'],
      ]
      const { queryClient } = await renderReadyEdit({
        seedCache: (cache) => {
          for (const key of keys) {
            cache.setQueryData(key, [])
          }
        },
      })

      fireEvent.click(saveButton())

      await waitFor(() => {
        for (const key of keys) {
          expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
        }
      })
    })

    it('disables Save while the request waits for an answer', async () => {
      await renderReadyEdit()
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      fireEvent.click(saveButton())

      await expectDisabledWhilePending(saveButton())
    })

    it.each([
      [403, 'EXPENSE_NOT_ALLOWED'],
      [404, 'EXPENSE_NOT_FOUND'],
      [400, 'EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL'],
      [400, 'EXPENSE_AMOUNT_TOO_LARGE'],
    ])('shows the translated text for %i %s, stays on the screen and keeps the typed values', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router } = await renderReadyEdit({
        answers: { [`PUT ${detailPath}`]: problemAnswer(status, code) },
      })
      typeAmount('3500')
      typeTitle('Hotel by the sea')

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(editRoute)
      expect(amountField().value).toBe('3500')
      expect(titleField().value).toBe('Hotel by the sea')
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderReadyEdit({ answers: { [`PUT ${detailPath}`]: networkFailureAnswer() } })

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })
  })

  describe('without the right to edit', () => {
    it('goes to the detail of the expense instead of showing the form', async () => {
      const { router, fetchMock } = await renderEdit({ detail: detailOf({ canEdit: false }) })

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(detailRoute)
      })
      expect(screen.getByText('expense detail page')).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })).toBeNull()
      expect(requestCount(fetchMock, 'PUT', detailPath)).toBe(0)
    })
  })

  describe('loading and failing', () => {
    it.each(languages)('shows the EXPENSE_NOT_FOUND text with a Go to Groups link and no retry button (%s)', async (language) => {
      await renderEdit({
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
      await renderEdit({
        language,
        answers: { [`GET ${detailPath}`]: () => new Response(null, { status: 404 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.EXPENSE_NOT_FOUND'),
      )

      expect(screen.getByRole('link', { name: translated(language, 'groups.goToGroups') }).getAttribute('href')).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated(language, 'common.retry') })).toBeNull()
    })

    it('shows the generic error with a retry button when the expense cannot be loaded, and loads the form after a retry', async () => {
      let tries = 0
      await renderEdit({
        answers: {
          [`GET ${detailPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(hotelDetail)
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
    })

    it('shows the not-found state when the group is not found', async () => {
      await renderEdit({ answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderEdit({ answers: { [`GET ${detailPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })
  })

  describe('in Macedonian', () => {
    it('writes the rows and the Save button in Macedonian and the category name in Macedonian', async () => {
      await renderReadyEdit({ language: 'mk' })

      expect(formRow('paidBy', 'mk')).toBeTruthy()
      expect(formRow('split', 'mk')).toBeTruthy()
      expect(saveButton('mk')).toBeTruthy()
      expect(formRow('category', 'mk').textContent).toContain(categoryName('mk', accommodationCategory))
    })
  })
})
