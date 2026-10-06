import type { QueryClient } from '@tanstack/react-query'
import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupMembers } from '@/core/services/groups/membersService'
import type { Me } from '@/core/services/me/meService'
import { formatMoney } from '@/shared/utils/formatMoney'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  accommodationCategory,
  foodCategory,
  hotelDetail,
  hotelExpenseId,
  testCategories,
  testToday,
  testYesterday,
} from '@/test/expenseTestData'
import {
  addDays,
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
  expensesPath,
  freezeTime,
  groupPath,
  isBefore,
  membersPath,
  plainSpaces,
  startsWith,
  unfreezeTime,
  uuidFormat,
} from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroupId } from '@/test/groupTestData'
import {
  anaMember,
  anaUserId,
  anaViewMembers,
  bojanRemoved,
  filipMember,
  filipPictureUrl,
  grandmaMember,
  markoMember,
  membersOf,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { AddExpenseScreen } from './AddExpenseScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const fiveMembers: GroupMembers = ownerViewMembers
const fourMembers: GroupMembers = membersOf({
  members: [filipMember, anaMember, markoMember, grandmaMember],
  removed: [],
  canClaimNames: false,
})
const twoMembers: GroupMembers = membersOf({
  members: [filipMember, anaMember],
  removed: [],
  canClaimNames: false,
})
const hotelMinor = 300000

interface RenderOptions {
  language?: Language
  group?: Group
  members?: GroupMembers
  me?: Partial<Me>
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderAdd(options: RenderOptions = {}) {
  const group = options.group ?? groupOf({ memberCount: 5 })
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    [`GET ${membersPath}`]: jsonAnswer(options.members ?? fiveMembers),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${expensesPath}`]: jsonAnswer(hotelDetail),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupExpenseNew(group.id),
    {
      language,
      seedCache: (queryClient) => {
        seedMe({ ...testMe, language, ...options.me })(queryClient)
        options.seedCache?.(queryClient)
      },
    },
  )
  return { fetchMock, ...rendered }
}

async function showsForm(language: Language = 'en'): Promise<void> {
  await screen.findByRole('button', { name: translated(language, 'groupSettings.save') })
}

async function renderReadyAdd(options: RenderOptions = {}) {
  const rendered = await renderAdd(options)
  await showsForm(options.language)
  return rendered
}

async function savedBody(options: RenderOptions, steps: () => Promise<void> | void) {
  const { fetchMock } = await renderReadyAdd(options)
  await steps()
  fireEvent.click(saveButton())
  await waitFor(() => {
    expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
  })
  return sentBody(fetchMock, 'POST', expensesPath)
}

function sharesOf(body: Record<string, unknown>): { memberId: string; inputValue: number }[] {
  const shares = body.shares
  if (!Array.isArray(shares)) {
    throw new Error(`Expected the body to have a list of shares, found ${JSON.stringify(shares)}`)
  }
  return shares as { memberId: string; inputValue: number }[]
}

function leftToAssign(amountMinor: number, language: Language = 'en'): string {
  return plainSpaces(
    translated(language, 'expense.amountLeft', {
      amount: formatMoney(amountMinor, 'MKD', language),
    }),
  )
}

describe('AddExpenseScreen', () => {
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
    it.each(languages)('shows the Add expense heading (%s)', async (language) => {
      await renderReadyAdd({ language })

      expect(
        screen.getByRole('heading', { level: 1, name: translated(language, 'expense.addTitle') }),
      ).toBeTruthy()
    })

    it('has a back button to the group', async () => {
      await renderReadyAdd()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.group(testGroupId))
    })

    it('shows no bottom bar', async () => {
      await renderReadyAdd()

      expect(screen.queryByRole('navigation')).toBeNull()
    })

    it('puts the amount first, then the rows Paid by, Split, Date and Category, then the title, then Save', async () => {
      await renderReadyAdd()

      const ordered: HTMLElement[] = [
        amountField(),
        formRow('paidBy'),
        formRow('split'),
        formRow('date'),
        formRow('category'),
        titleField(),
        saveButton(),
      ]

      for (let index = 1; index < ordered.length; index += 1) {
        expect(isBefore(ordered[index - 1], ordered[index])).toBe(true)
      }
    })

    it('focuses the amount field when the screen opens', async () => {
      await renderReadyAdd()

      expect(document.activeElement).toBe(amountField())
    })

    it('shows the amount field empty', async () => {
      await renderReadyAdd()

      expect(amountField().value).toBe('')
    })

    it('shows no note field until the Note link is pressed', async () => {
      await renderReadyAdd()

      expect(screen.queryByLabelText(translated('en', 'expense.note'))).toBeNull()
      expect(noteLink()).toBeTruthy()
    })

    it('shows the title field empty with its optional label', async () => {
      await renderReadyAdd()

      expect(titleField().value).toBe('')
    })

    it('shows the loading spinner while the group, the members or the categories are being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/expenses/new', element: <AddExpenseScreen /> }],
        routes.groupExpenseNew(testGroupId),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })).toBeNull()
    })
  })

  describe('the defaults', () => {
    it('pre-fills Paid by with the person who is adding the expense, marked as me', async () => {
      await renderReadyAdd()

      const text = formRow('paidBy').textContent ?? ''

      expect(text).toContain(`Filip (${translated('en', 'expense.me')})`)
    })

    it('pre-fills Split with equally among everyone', async () => {
      await renderReadyAdd()

      const text = formRow('split').textContent ?? ''

      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).toContain(translated('en', 'expense.everyone'))
    })

    it('pre-fills Date with Today', async () => {
      await renderReadyAdd()

      expect(formRow('date').textContent).toContain(translated('en', 'expenses.today'))
    })

    it('leaves Category empty: none of the ten category names is shown', async () => {
      await renderReadyAdd()

      const text = formRow('category').textContent ?? ''

      for (const category of testCategories) {
        expect(text).not.toContain(categoryName('en', category))
      }
    })

    it('uses the currency of the group, MKD', async () => {
      await renderReadyAdd()

      expect(currencyToggle('MKD')).toBeTruthy()
    })

    it('uses the currency of the group, EUR', async () => {
      await renderReadyAdd({ group: groupOf({ memberCount: 5, defaultCurrency: 'EUR' }) })

      expect(currencyToggle('EUR')).toBeTruthy()
    })

    it('uses the person who adds the expense as the payer, whoever that is', async () => {
      const body = await savedBody({ members: anaViewMembers, me: { id: anaUserId } }, () => {
        typeAmount('1200')
      })

      expect(body.paidByMemberId).toBe(anaMember.id)
    })

    it('splits equally among every current member, in the joining order, with no extras', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(body.splitType).toBe('Equal')
      expect(sharesOf(body)).toEqual(
        [filipMember, anaMember, markoMember, grandmaMember, petarMember].map((member) => ({
          memberId: member.id,
          inputValue: 0,
        })),
      )
    })

    it('does not put a removed person in the split', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(sharesOf(body).map((share) => share.memberId)).not.toContain(bojanRemoved.id)
    })

    it('uses today in the time zone of the account', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(body.expenseDate).toBe(testToday)
    })

    it('uses the day it is in the time zone of the account, not in UTC', async () => {
      const body = await savedBody({ me: { timeZone: 'Pacific/Pago_Pago' } }, () => {
        typeAmount('1200')
      })

      expect(body.expenseDate).toBe('2026-10-05')
    })

    it('sends no category, no title and no note by default', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(body.categoryId ?? null).toBeNull()
      expect(body.title ?? null).toBeNull()
      expect(body.note ?? null).toBeNull()
    })

    it('does not ask for the categories again when they are already in the cache for this session', async () => {
      const { fetchMock } = await renderReadyAdd({
        seedCache: (queryClient) => {
          queryClient.setQueryData(['categories'], testCategories)
        },
      })

      expect(requestCount(fetchMock, 'GET', categoriesPath)).toBe(0)
    })
  })

  describe('the amount and the currency', () => {
    it('sends whole denars as deni for MKD', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(body).toMatchObject({ amountMinor: 120000, currency: 'MKD' })
    })

    it('shows a number keypad for MKD and a decimal keypad for EUR', async () => {
      await renderReadyAdd()

      expect(amountField().inputMode).toBe('numeric')

      fireEvent.click(currencyToggle('MKD'))

      expect(amountField().inputMode).toBe('decimal')
    })

    it.each(languages)('switches between MKD and EUR when the currency label is pressed, with the label saying so (%s)', async (language) => {
      await renderReadyAdd({ language })

      fireEvent.click(currencyToggle('MKD', language))
      expect(currencyToggle('EUR', language)).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated(language, 'expense.currencyToggle', { currency: 'MKD' }) })).toBeNull()

      fireEvent.click(currencyToggle('EUR', language))
      expect(currencyToggle('MKD', language)).toBeTruthy()
    })

    it('sends the amount in cents and the currency EUR after the label was switched', async () => {
      const body = await savedBody({}, () => {
        fireEvent.click(currencyToggle('MKD'))
        typeAmount('12.50')
      })

      expect(body).toMatchObject({ amountMinor: 1250, currency: 'EUR' })
    })

    it('accepts a decimal comma for EUR', async () => {
      const body = await savedBody({}, () => {
        fireEvent.click(currencyToggle('MKD'))
        typeAmount('12,5')
      })

      expect(body).toMatchObject({ amountMinor: 1250, currency: 'EUR' })
    })

    it('starts with EUR when the group uses EUR', async () => {
      const body = await savedBody({ group: groupOf({ memberCount: 5, defaultCurrency: 'EUR' }) }, () => {
        typeAmount('45')
      })

      expect(body).toMatchObject({ amountMinor: 4500, currency: 'EUR' })
    })

    it.each([
      ['an empty amount', ''],
      ['zero', '0'],
      ['letters', 'abc'],
      ['a decimal in MKD', '12.5'],
      ['a thousands comma in MKD', '1,200'],
      ['13 digits', '1234567890123'],
    ])('does not send the expense for %s', async (_name, text) => {
      const { fetchMock } = await renderReadyAdd()
      typeAmount(text)

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(isDisabled(saveButton()) || screen.queryByRole('alert') !== null).toBe(true)
      })
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    })

    it('does not send an amount with three decimals in EUR', async () => {
      const { fetchMock } = await renderReadyAdd()
      fireEvent.click(currencyToggle('MKD'))
      typeAmount('12.555')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(isDisabled(saveButton()) || screen.queryByRole('alert') !== null).toBe(true)
      })
      expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(0)
    })
  })

  describe('the Paid by sheet', () => {
    it.each(languages)('opens a sheet with the title Paid by when the row is pressed (%s)', async (language) => {
      await renderReadyAdd({ language })

      const sheet = await openSheet('paidBy', language)

      expect(sheet).toBeTruthy()
    })

    it('lists the current members in the joining order and not the removed ones', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('paidBy')

      const buttons = ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar'].map((name) => personButton(sheet, name))
      for (let index = 1; index < buttons.length; index += 1) {
        expect(isBefore(buttons[index - 1], buttons[index])).toBe(true)
      }
      expect(within(sheet).queryByRole('button', { name: startsWith(bojanRemoved.displayName) })).toBeNull()
    })

    it('marks the person who is adding the expense with me', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('paidBy')

      expect(personButton(sheet, 'Filip').textContent).toContain(`(${translated('en', 'expense.me')})`)
      expect(personButton(sheet, 'Ana').textContent).not.toContain(`(${translated('en', 'expense.me')})`)
    })

    it('shows a tick on the payer only', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('paidBy')

      expect(personButton(sheet, 'Filip').getAttribute('aria-pressed')).toBe('true')
      expect(personButton(sheet, 'Ana').getAttribute('aria-pressed')).toBe('false')
    })

    it('closes with one tap on a person, shows that person in the row and sends them as the payer', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('paidBy')

      fireEvent.click(personButton(sheet, 'Ana'))

      await waitForSheetToClose()
      expect(formRow('paidBy').textContent).toContain('Ana')
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      })
      expect(sentBody(fetchMock, 'POST', expensesPath).paidByMemberId).toBe(anaMember.id)
    })

    it('shows the ticked person after the sheet is opened again', async () => {
      await renderReadyAdd()
      const sheet = await openSheet('paidBy')
      fireEvent.click(personButton(sheet, 'Marko'))
      await waitForSheetToClose()

      const again = await openSheet('paidBy')

      expect(personButton(again, 'Marko').getAttribute('aria-pressed')).toBe('true')
      expect(personButton(again, 'Filip').getAttribute('aria-pressed')).toBe('false')
    })

    it('can pick a plain name as the payer', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('paidBy')
      fireEvent.click(personButton(sheet, 'Grandma'))
      await waitForSheetToClose()
      typeAmount('500')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(sentBody(fetchMock, 'POST', expensesPath).paidByMemberId).toBe(grandmaMember.id)
      })
    })

    it('shows the Google picture of a person who has one and the initial of the others', async () => {
      await renderReadyAdd({
        members: membersOf({ members: [{ ...filipMember, pictureUrl: filipPictureUrl }, anaMember] }),
      })

      const sheet = await openSheet('paidBy')

      const picture = personButton(sheet, 'Filip').querySelector('img')
      expect(picture?.getAttribute('src')).toBe(filipPictureUrl)
      expect(picture?.getAttribute('referrerpolicy')).toBe('no-referrer')
      expect(personButton(sheet, 'Ana').querySelector('img')).toBeNull()
      expect(within(personButton(sheet, 'Ana')).getByText('A')).toBeTruthy()
    })

    it('gives every person the colour of their place in the joining order', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('paidBy')

      expect(personButton(sheet, 'Filip').innerHTML).toMatch(/--avatar-0(?!\d)/)
      expect(personButton(sheet, 'Ana').innerHTML).toMatch(/--avatar-1(?!\d)/)
      expect(personButton(sheet, 'Marko').innerHTML).toMatch(/--avatar-2(?!\d)/)
      expect(personButton(sheet, 'Petar').innerHTML).toMatch(/--avatar-4(?!\d)/)
    })
  })

  describe('the Category sheet', () => {
    it.each(languages)('opens a sheet titled Pick a category with the ten categories (%s)', async (language) => {
      await renderReadyAdd({ language })

      const sheet = await openSheet('category', language)

      for (const category of testCategories) {
        const firstWord = categoryName(language, category).split(' ')[0]
        expect(within(sheet).getByRole('button', { name: new RegExp(firstWord) })).toBeTruthy()
      }
    })

    it('shows the emoji of every category in the grid', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('category')

      for (const category of testCategories) {
        expect(within(sheet).getByText(category.emoji)).toBeTruthy()
      }
    })

    it('closes with one tap, shows the emoji and the name in the row and sends the category', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('category')

      fireEvent.click(within(sheet).getByRole('button', { name: /Food/ }))

      await waitForSheetToClose()
      const rowText = formRow('category').textContent ?? ''
      expect(rowText).toContain(foodCategory.emoji)
      expect(rowText).toContain(categoryName('en', foodCategory))
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      })
      expect(sentBody(fetchMock, 'POST', expensesPath).categoryId).toBe(foodCategory.id)
    })

    it('shows the picked category as pressed when the sheet is opened again', async () => {
      await renderReadyAdd()
      const sheet = await openSheet('category')
      fireEvent.click(within(sheet).getByRole('button', { name: /Accommodation/ }))
      await waitForSheetToClose()

      const again = await openSheet('category')

      expect(within(again).getByRole('button', { name: /Accommodation/ }).getAttribute('aria-pressed')).toBe('true')
      expect(within(again).getByRole('button', { name: /Food/ }).getAttribute('aria-pressed')).toBe('false')
    })

    it('writes the category name in Macedonian in the row after a pick', async () => {
      await renderReadyAdd({ language: 'mk' })
      const sheet = await openSheet('category', 'mk')

      fireEvent.click(within(sheet).getByRole('button', { name: /Сместување/ }))

      await waitForSheetToClose()
      expect(formRow('category', 'mk').textContent).toContain(categoryName('mk', accommodationCategory))
    })
  })

  describe('the Date sheet', () => {
    it.each(languages)('opens a sheet titled Pick a date with Today, Yesterday, a calendar and the hint (%s)', async (language) => {
      await renderReadyAdd({ language })

      const sheet = await openSheet('date', language)

      expect(within(sheet).getByRole('button', { name: translated(language, 'expenses.today') })).toBeTruthy()
      expect(within(sheet).getByRole('button', { name: translated(language, 'expenses.yesterday') })).toBeTruthy()
      expect(within(sheet).getByLabelText(translated(language, 'expense.date'), { selector: 'input' })).toBeTruthy()
      expect(within(sheet).getByText(translated(language, 'expense.dateHint'))).toBeTruthy()
    })

    it('shows Today as the pressed choice at first', async () => {
      await renderReadyAdd()

      const sheet = await openSheet('date')

      expect(within(sheet).getByRole('button', { name: translated('en', 'expenses.today') }).getAttribute('aria-pressed')).toBe('true')
      expect(within(sheet).getByRole('button', { name: translated('en', 'expenses.yesterday') }).getAttribute('aria-pressed')).toBe('false')
    })

    it('closes with one tap on Yesterday, shows Yesterday in the row and sends yesterday', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('date')

      fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'expenses.yesterday') }))

      await waitForSheetToClose()
      expect(formRow('date').textContent).toContain(translated('en', 'expenses.yesterday'))
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      })
      expect(sentBody(fetchMock, 'POST', expensesPath).expenseDate).toBe(testYesterday)
    })

    it('goes back to today when Today is pressed after Yesterday', async () => {
      const { fetchMock } = await renderReadyAdd()
      const first = await openSheet('date')
      fireEvent.click(within(first).getByRole('button', { name: translated('en', 'expenses.yesterday') }))
      await waitForSheetToClose()
      const second = await openSheet('date')

      fireEvent.click(within(second).getByRole('button', { name: translated('en', 'expenses.today') }))

      await waitForSheetToClose()
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(sentBody(fetchMock, 'POST', expensesPath).expenseDate).toBe(testToday)
      })
    })

    it('lets the person pick a date in the future, like a December trip, closes and sends that date', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('date')

      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.date'), { selector: 'input' }), {
        target: { value: '2026-12-12' },
      })

      await waitForSheetToClose()
      expect(formRow('date').textContent).toContain('12')
      expect(formRow('date').textContent).not.toContain(translated('en', 'expenses.today'))
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      })
      expect(sentBody(fetchMock, 'POST', expensesPath).expenseDate).toBe('2026-12-12')
    })

    it('lets the person pick an old date', async () => {
      const { fetchMock } = await renderReadyAdd()
      const sheet = await openSheet('date')

      fireEvent.change(within(sheet).getByLabelText(translated('en', 'expense.date'), { selector: 'input' }), {
        target: { value: '2025-03-01' },
      })

      await waitForSheetToClose()
      typeAmount('1200')
      fireEvent.click(saveButton())
      await waitFor(() => {
        expect(sentBody(fetchMock, 'POST', expensesPath).expenseDate).toBe('2025-03-01')
      })
    })

    it('does not offer a date before the year 2000', async () => {
      await renderReadyAdd()
      const sheet = await openSheet('date')

      const field = within(sheet).getByLabelText<HTMLInputElement>(translated('en', 'expense.date'), { selector: 'input' })

      expect(field.min).toBe('2000-01-01')
    })

    it('does not offer a date beyond today plus 365 days, so a typo like 2062 is not possible', async () => {
      await renderReadyAdd()
      const sheet = await openSheet('date')

      const field = within(sheet).getByLabelText<HTMLInputElement>(translated('en', 'expense.date'), { selector: 'input' })

      expect(field.max).toBe(addDays(testToday, 365))
    })

    it('counts the year ahead from today in the time zone of the account', async () => {
      await renderReadyAdd({ me: { timeZone: 'Pacific/Pago_Pago' } })
      const sheet = await openSheet('date')

      const field = within(sheet).getByLabelText<HTMLInputElement>(translated('en', 'expense.date'), { selector: 'input' })

      expect(field.max).toBe(addDays('2026-10-05', 365))
    })
  })

  describe('the Split sheet', () => {
    it.each(languages)('opens a sheet titled Split with the four tabs Equal, Exact, % and Shares, Equal selected (%s)', async (language) => {
      await renderReadyAdd({ language })

      const sheet = await openSheet('split', language)

      expect(splitTab(sheet, 'splitEqual', language).getAttribute('aria-selected')).toBe('true')
      for (const tab of ['splitExact', 'splitPercentage', 'splitShares'] as const) {
        expect(splitTab(sheet, tab, language).getAttribute('aria-selected')).toBe('false')
      }
    })

    it('shows a selected tab after it is pressed and unselects the others', async () => {
      await renderReadyAdd()
      const sheet = await openSheet('split')

      fireEvent.click(splitTab(sheet, 'splitShares'))

      expect(splitTab(sheet, 'splitShares').getAttribute('aria-selected')).toBe('true')
      expect(splitTab(sheet, 'splitEqual').getAttribute('aria-selected')).toBe('false')
    })

    it('lists every current member of the group in the joining order on the Equal tab, all ticked', async () => {
      await renderReadyAdd()
      typeAmount('3000')

      const sheet = await openSheet('split')

      expect(within(sheet).getAllByRole('group')).toHaveLength(5)
      for (const name of ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar']) {
        const tick = within(personGroup(sheet, name)).getByRole('button', { name: startsWith(name) })
        expect(tick.getAttribute('aria-pressed')).toBe('true')
      }
    })

    it('shows the Google picture of a person in their row', async () => {
      await renderReadyAdd({
        members: membersOf({ members: [{ ...filipMember, pictureUrl: filipPictureUrl }, anaMember] }),
      })

      const sheet = await openSheet('split')

      expect(personGroup(sheet, 'Filip').querySelector('img')?.getAttribute('src')).toBe(filipPictureUrl)
      expect(personGroup(sheet, 'Ana').querySelector('img')).toBeNull()
    })

    it('has a + extra button for every person on the Equal tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')

      const sheet = await openSheet('split')

      for (const name of ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar']) {
        expect(
          within(personGroup(sheet, name)).getByRole('button', { name: translated('en', 'expense.extra') }),
        ).toBeTruthy()
      }
    })

    it('has Done enabled for the default split and closes the sheet when Done is pressed', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')

      expect(isDisabled(doneButton(sheet))).toBe(false)
      fireEvent.click(doneButton(sheet))

      await waitForSheetToClose()
    })

    it('takes a person out of the split when their tick is pressed and shows that in the row', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')

      fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: startsWith('Marko') }))

      expect(within(personGroup(sheet, 'Marko')).getByRole('button', { name: startsWith('Marko') }).getAttribute('aria-pressed')).toBe('false')
      expect(isDisabled(doneButton(sheet))).toBe(false)
      fireEvent.click(doneButton(sheet))
      await waitForSheetToClose()
      const text = formRow('split').textContent ?? ''
      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).not.toContain(translated('en', 'expense.everyone'))
      expect(text).not.toContain('Marko')
      for (const name of ['Filip', 'Ana', 'Grandma', 'Petar']) {
        expect(text).toContain(name)
      }
      expect(text).toContain(', ')
    })

    it('sends only the people who are in the split', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: startsWith('Marko') }))
        fireEvent.click(within(personGroup(sheet, 'Petar')).getByRole('button', { name: startsWith('Petar') }))
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(sharesOf(body).map((share) => share.memberId)).toEqual([filipMember.id, anaMember.id, grandmaMember.id])
    })

    it('keeps Done disabled when nobody is in the split', async () => {
      await renderReadyAdd({ members: twoMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')

      for (const name of ['Filip', 'Ana']) {
        fireEvent.click(within(personGroup(sheet, name)).getByRole('button', { name: startsWith(name) }))
      }

      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('splits the hotel as 3,000 MKD for 5 people with Marko paying 600 more', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: translated('en', 'expense.extra') }))
        typeInPerson(sheet, 'Marko', '600')
        expect(isDisabled(doneButton(sheet))).toBe(false)
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body).toMatchObject({ amountMinor: hotelMinor, splitType: 'Equal' })
      expect(sharesOf(body)).toEqual([
        { memberId: filipMember.id, inputValue: 0 },
        { memberId: anaMember.id, inputValue: 0 },
        { memberId: markoMember.id, inputValue: 60000 },
        { memberId: grandmaMember.id, inputValue: 0 },
        { memberId: petarMember.id, inputValue: 0 },
      ])
    })

    it('keeps Done disabled when the extras are more than the total', async () => {
      await renderReadyAdd()
      typeAmount('1000')
      const sheet = await openSheet('split')

      fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: translated('en', 'expense.extra') }))
      typeInPerson(sheet, 'Marko', '1500')

      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('says how much is left to assign on the Exact tab and keeps Done disabled until it adds up', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))

      typeInPerson(sheet, 'Marko', '1080')

      expect(within(sheet).getByText(leftToAssign(192000), { normalizer: plainSpaces })).toBeTruthy()
      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('has the whole total left to assign when nothing is typed on the Exact tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')

      fireEvent.click(splitTab(sheet, 'splitExact'))

      expect(within(sheet).getByText(leftToAssign(hotelMinor), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('enables Done and sends the typed amounts as deni when the Exact amounts add up', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitExact'))
        typeInPerson(sheet, 'Filip', '480')
        typeInPerson(sheet, 'Ana', '480')
        typeInPerson(sheet, 'Marko', '1080')
        typeInPerson(sheet, 'Grandma', '480')
        typeInPerson(sheet, 'Petar', '480')
        expect(isDisabled(doneButton(sheet))).toBe(false)
        expect(within(sheet).queryByText(leftToAssign(0), { normalizer: plainSpaces })).toBeNull()
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body.splitType).toBe('Exact')
      expect(sharesOf(body).map((share) => share.inputValue)).toEqual([48000, 48000, 108000, 48000, 48000])
    })

    it('says how many percent are left on the % tab and keeps Done disabled until it is 100 %', async () => {
      await renderReadyAdd({ members: fourMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitPercentage'))

      typeInPerson(sheet, 'Filip', '25')
      typeInPerson(sheet, 'Ana', '25')
      typeInPerson(sheet, 'Marko', '25')

      expect(within(sheet).getByText(translated('en', 'expense.percentLeft', { percent: '25' }))).toBeTruthy()
      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('shows 12.5 % left in English and 12,5 % in Macedonian', async () => {
      await renderReadyAdd({ members: fourMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitPercentage'))

      typeInPerson(sheet, 'Filip', '25')
      typeInPerson(sheet, 'Ana', '25')
      typeInPerson(sheet, 'Marko', '25')
      typeInPerson(sheet, 'Grandma', '12.5')

      expect(within(sheet).getByText(translated('en', 'expense.percentLeft', { percent: '12.5' }))).toBeTruthy()
    })

    it('shows the percent left in Macedonian with a decimal comma', async () => {
      await renderReadyAdd({ language: 'mk', members: fourMembers })
      typeAmount('3000', 'mk')
      const sheet = await openSheet('split', 'mk')
      fireEvent.click(splitTab(sheet, 'splitPercentage', 'mk'))

      typeInPerson(sheet, 'Filip', '25')
      typeInPerson(sheet, 'Ana', '25')
      typeInPerson(sheet, 'Marko', '25')
      typeInPerson(sheet, 'Grandma', '12.5')

      expect(within(sheet).getByText(translated('mk', 'expense.percentLeft', { percent: '12,5' }))).toBeTruthy()
    })

    it('sends percentages in hundredths of a percent when they add up to 100 %', async () => {
      const body = await savedBody({ members: fourMembers }, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitPercentage'))
        typeInPerson(sheet, 'Filip', '50')
        typeInPerson(sheet, 'Ana', '25')
        typeInPerson(sheet, 'Marko', '12.5')
        typeInPerson(sheet, 'Grandma', '12.5')
        expect(isDisabled(doneButton(sheet))).toBe(false)
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body.splitType).toBe('Percentage')
      expect(sharesOf(body).map((share) => share.inputValue)).toEqual([5000, 2500, 1250, 1250])
    })

    it('has a minus and a plus button for every person on the Shares tab and sends whole shares', async () => {
      const body = await savedBody({ members: twoMembers }, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitShares'))
        const anaButtons = within(personGroup(sheet, 'Ana')).getAllByRole('button')
        expect(anaButtons).toHaveLength(2)
        const before = Number(within(personGroup(sheet, 'Ana')).getByText(/^\d+$/).textContent)
        fireEvent.click(anaButtons[1])
        expect(Number(within(personGroup(sheet, 'Ana')).getByText(/^\d+$/).textContent)).toBe(before + 1)
        fireEvent.click(anaButtons[1])
        expect(Number(within(personGroup(sheet, 'Ana')).getByText(/^\d+$/).textContent)).toBe(before + 2)
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body.splitType).toBe('Shares')
      const shares = sharesOf(body)
      expect(shares[1].inputValue - shares[0].inputValue).toBe(2)
    })

    it('keeps Done disabled when everybody is down to 0 shares', async () => {
      await renderReadyAdd({ members: twoMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitShares'))

      for (const name of ['Filip', 'Ana']) {
        const minus = within(personGroup(sheet, name)).getAllByRole('button')[0]
        for (let press = 0; press < 5; press += 1) {
          fireEvent.click(minus)
        }
      }

      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('counts an EUR split in cents on the Exact tab', async () => {
      const body = await savedBody({ members: twoMembers, group: groupOf({ memberCount: 2, defaultCurrency: 'EUR' }) }, async () => {
        typeAmount('10.50')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitExact'))
        typeInPerson(sheet, 'Filip', '4.50')
        typeInPerson(sheet, 'Ana', '6')
        expect(isDisabled(doneButton(sheet))).toBe(false)
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body).toMatchObject({ amountMinor: 1050, currency: 'EUR', splitType: 'Exact' })
      expect(sharesOf(body).map((share) => share.inputValue)).toEqual([450, 600])
    })

    it('shows the exact amount left in euros on the Exact tab of an EUR expense', async () => {
      await renderReadyAdd({ members: twoMembers, group: groupOf({ memberCount: 2, defaultCurrency: 'EUR' }) })
      typeAmount('10.50')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))

      typeInPerson(sheet, 'Filip', '4.50')

      expect(
        within(sheet).getByText(
          plainSpaces(translated('en', 'expense.amountLeft', { amount: formatMoney(600, 'EUR', 'en') })),
          { normalizer: plainSpaces },
        ),
      ).toBeTruthy()
    })
  })

  describe('the Split sheet edits', () => {
    it.each([
      ['splitExact', 'expense.splitExact'],
      ['splitPercentage', 'expense.splitPercentage'],
      ['splitShares', 'expense.splitShares'],
    ] as const)('shows the type name in the Split row instead of equally and everyone after the %s tab was used', async (tab, key) => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')

      fireEvent.click(splitTab(sheet, tab))
      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      const text = formRow('split').textContent ?? ''
      expect(text).toContain(translated('en', key))
      expect(text).not.toContain(translated('en', 'expense.everyone'))
      expect(text).not.toContain(translated('en', 'expense.equally'))
    })

    it('says equally and everyone again when everybody is back in on the Equal tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(personButton(sheet, 'Marko'))
      fireEvent.click(personButton(sheet, 'Marko'))
      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      const text = formRow('split').textContent ?? ''

      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).toContain(translated('en', 'expense.everyone'))
    })

    it('clears the typed extra of the Equal tab when another tab is opened and Equal is opened again', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: translated('en', 'expense.extra') }))
      typeInPerson(sheet, 'Marko', '600')

      fireEvent.click(splitTab(sheet, 'splitExact'))
      fireEvent.click(splitTab(sheet, 'splitEqual'))

      const box = within(personGroup(sheet, 'Marko')).queryByRole<HTMLInputElement>('textbox')
      expect(box === null || box.value === '').toBe(true)
    })

    it('clears the typed Exact amounts when another tab is opened and Exact is opened again', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Filip', '480')

      fireEvent.click(splitTab(sheet, 'splitPercentage'))
      expect(within(personGroup(sheet, 'Filip')).getByRole<HTMLInputElement>('textbox').value).toBe('')
      fireEvent.click(splitTab(sheet, 'splitExact'))

      expect(within(personGroup(sheet, 'Filip')).getByRole<HTMLInputElement>('textbox').value).toBe('')
    })

    it('starts every person who is in with 1 share on the Shares tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')

      fireEvent.click(splitTab(sheet, 'splitShares'))

      for (const name of ['Filip', 'Ana', 'Marko', 'Grandma', 'Petar']) {
        expect(within(personGroup(sheet, name)).getByText(/^\d+$/).textContent).toBe('1')
      }
      expect(isDisabled(doneButton(sheet))).toBe(false)
    })

    it('starts the people who stay in with 1 share when somebody was taken out on the Equal tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(personButton(sheet, 'Marko'))

      fireEvent.click(splitTab(sheet, 'splitShares'))

      for (const name of ['Filip', 'Ana']) {
        expect(within(personGroup(sheet, name)).getByText(/^\d+$/).textContent).toBe('1')
      }
    })

    it('writes the amount left to assign with a minus when the Exact amounts add up to more than the total', async () => {
      await renderReadyAdd({ members: fourMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))

      typeInPerson(sheet, 'Filip', '1000')
      typeInPerson(sheet, 'Ana', '1000')
      typeInPerson(sheet, 'Marko', '1000')
      typeInPerson(sheet, 'Grandma', '500')

      expect(within(sheet).getByText(leftToAssign(-50000), { normalizer: plainSpaces })).toBeTruthy()
      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('writes the percent left with a minus when the percentages add up to more than 100', async () => {
      await renderReadyAdd({ members: fourMembers })
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitPercentage'))

      typeInPerson(sheet, 'Filip', '60')
      typeInPerson(sheet, 'Ana', '60')

      expect(within(sheet).getByText(translated('en', 'expense.percentLeft', { percent: '-20' }))).toBeTruthy()
      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('applies a change at once: Escape closes the sheet and keeps a person who was taken out', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(personButton(sheet, 'Marko'))

      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      expect(formRow('split').textContent).not.toContain('Marko')
      const again = await openSheet('split')
      expect(personButton(again, 'Marko').getAttribute('aria-pressed')).toBe('false')
    })

    it('closes with Escape even while Done is disabled, and keeps the half-finished Exact split in the draft', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Filip', '480')
      expect(isDisabled(doneButton(sheet))).toBe(true)

      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      const again = await openSheet('split')
      expect(splitTab(again, 'splitExact').getAttribute('aria-selected')).toBe('true')
      expect(within(personGroup(again, 'Filip')).getByRole<HTMLInputElement>('textbox').value).toBe('480')
    })
  })

  describe('switching the currency', () => {
    it('keeps the typed amount text', async () => {
      await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(currencyToggle('MKD'))

      expect(amountField().value).toBe('1200')
      expect(currencyToggle('EUR')).toBeTruthy()
    })

    it('clears the extras that were typed on the Equal tab', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const first = await openSheet('split')
      fireEvent.click(within(personGroup(first, 'Marko')).getByRole('button', { name: translated('en', 'expense.extra') }))
      typeInPerson(first, 'Marko', '600')
      fireEvent.keyDown(first, { key: 'Escape' })
      await waitForSheetToClose()

      fireEvent.click(currencyToggle('MKD'))

      const second = await openSheet('split')
      const box = within(personGroup(second, 'Marko')).queryByRole<HTMLInputElement>('textbox')
      expect(box === null || box.value === '').toBe(true)
    })

    it('clears the typed Exact amounts', async () => {
      await renderReadyAdd()
      typeAmount('3000')
      const first = await openSheet('split')
      fireEvent.click(splitTab(first, 'splitExact'))
      typeInPerson(first, 'Filip', '480')
      fireEvent.keyDown(first, { key: 'Escape' })
      await waitForSheetToClose()

      fireEvent.click(currencyToggle('MKD'))

      const second = await openSheet('split')
      expect(within(personGroup(second, 'Filip')).getByRole<HTMLInputElement>('textbox').value).toBe('')
    })

    it('sends the kept amount in the new currency with no extras', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('3000')
        const sheet = await openSheet('split')
        fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: translated('en', 'expense.extra') }))
        typeInPerson(sheet, 'Marko', '600')
        fireEvent.keyDown(sheet, { key: 'Escape' })
        await waitForSheetToClose()
        fireEvent.click(currencyToggle('MKD'))
      })

      expect(body).toMatchObject({ amountMinor: 300000, currency: 'EUR', splitType: 'Equal' })
      expect(sharesOf(body).every((share) => share.inputValue === 0)).toBe(true)
    })
  })

  describe('the title and the note', () => {
    it('sends the typed title', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
        typeTitle('Hotel')
      })

      expect(body.title).toBe('Hotel')
    })

    it('shows the note field when the Note link is pressed and sends the typed note', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
        fireEvent.click(noteLink())
        fireEvent.change(noteField(), { target: { value: 'Booked for the weekend' } })
      })

      expect(body.note).toBe('Booked for the weekend')
    })

    it('keeps the typed title and amount while the note field is opened', async () => {
      await renderReadyAdd()
      typeAmount('1200')
      typeTitle('Hotel')

      fireEvent.click(noteLink())

      expect(amountField().value).toBe('1200')
      expect(titleField().value).toBe('Hotel')
    })
  })

  describe('saving', () => {
    it('sends exactly the fields of the add request as JSON to the expenses of this group', async () => {
      const { fetchMock } = await renderReadyAdd()
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', expensesPath)[0]
      expect(request.contentType).toBe('application/json')
      const allowed = [
        'clientRequestId',
        'title',
        'note',
        'amountMinor',
        'currency',
        'expenseDate',
        'categoryId',
        'paidByMemberId',
        'splitType',
        'shares',
      ]
      expect(Object.keys(sentBody(fetchMock, 'POST', expensesPath)).every((key) => allowed.includes(key))).toBe(true)
      expect(sentBody(fetchMock, 'POST', expensesPath)).toMatchObject({
        amountMinor: 120000,
        currency: 'MKD',
        expenseDate: testToday,
        paidByMemberId: filipMember.id,
        splitType: 'Equal',
      })
    })

    it('sends a clientRequestId that is a new id', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(String(body.clientRequestId)).toMatch(uuidFormat)
    })

    it('uses another clientRequestId every time the screen is opened', async () => {
      const first = await savedBody({}, () => {
        typeAmount('1200')
      })
      cleanup()

      const second = await savedBody({}, () => {
        typeAmount('1200')
      })

      expect(second.clientRequestId).not.toBe(first.clientRequestId)
    })

    it('shows the Expense saved toast, goes to the group and shows the saved toast only after the answer', async () => {
      const { router } = await renderReadyAdd()
      typeAmount('1200')
      expect(shownToastTexts()).not.toContain(translated('en', 'expenses.saved'))

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(shownToastTexts()).toContain(translated('en', 'expenses.saved'))
      expect(screen.getByText('group page')).toBeTruthy()
    })

    it.each(languages)('shows the Expense saved toast in the language of the person (%s)', async (language) => {
      await renderReadyAdd({ language })
      typeAmount('1200', language)

      fireEvent.click(saveButton(language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(translated(language, 'expenses.saved'))
      })
    })

    it('marks every expense query of the group as out of date, so the list, the detail and the deleted list are asked again', async () => {
      const keys = [
        ['groups', testGroupId, 'expenses'],
        ['groups', testGroupId, 'expenses', hotelExpenseId],
        ['groups', testGroupId, 'expenses', 'deleted'],
      ]
      const { queryClient } = await renderReadyAdd({
        seedCache: (cache) => {
          for (const key of keys) {
            cache.setQueryData(key, [])
          }
        },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        for (const key of keys) {
          expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
        }
      })
    })

    it('marks the activity of the group as out of date after it was saved, so the new expense shows up in it', async () => {
      const key = ['groups', testGroupId, 'activity']
      const { queryClient } = await renderReadyAdd({
        seedCache: (cache) => {
          cache.setQueryData(key, [])
        },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true)
      })
    })

    it('does not mark the activity of the group as out of date when the save failed', async () => {
      const key = ['groups', testGroupId, 'activity']
      const { queryClient } = await renderReadyAdd({
        answers: { [`POST ${expensesPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE') },
        seedCache: (cache) => {
          cache.setQueryData(key, [])
        },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(queryClient.getQueryState(key)?.isInvalidated).toBe(false)
    })

    it('disables Save while the request waits for an answer', async () => {
      await renderReadyAdd()
      typeAmount('1200')
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      fireEvent.click(saveButton())

      await expectDisabledWhilePending(saveButton())
    })

    it('sends the same clientRequestId with every request of a double tap, so the server saves the expense once', async () => {
      await renderReadyAdd()
      typeAmount('1200')
      const neverAnswering = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
      vi.stubGlobal('fetch', neverAnswering)

      fireEvent.click(saveButton())
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(neverAnswering).toHaveBeenCalled()
      })
      const clientRequestIds = neverAnswering.mock.calls.map(([, init]) => {
        const body: unknown = JSON.parse(String(init?.body))
        return typeof body === 'object' && body !== null ? Reflect.get(body, 'clientRequestId') : undefined
      })
      expect(new Set(clientRequestIds).size).toBe(1)
      expect(String(clientRequestIds[0])).toMatch(uuidFormat)
    })

    it('keeps the same clientRequestId when Save is pressed again after a failed try, so a retry never saves twice', async () => {
      let tries = 0
      const { fetchMock } = await renderReadyAdd({
        answers: {
          [`POST ${expensesPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(hotelDetail)
          },
        },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())
      await screen.findByRole('alert')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(2)
      })
      const first = sentBody(fetchMock, 'POST', expensesPath, 0)
      const second = sentBody(fetchMock, 'POST', expensesPath, 1)
      expect(second.clientRequestId).toBe(first.clientRequestId)
      expect(second).toEqual(first)
    })

    it('keeps the same clientRequestId after a network failure', async () => {
      let tries = 0
      const { fetchMock } = await renderReadyAdd({
        answers: {
          [`POST ${expensesPath}`]: () => {
            tries += 1
            return tries === 1 ? new TypeError('Failed to fetch') : Response.json(hotelDetail)
          },
        },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())
      await screen.findByRole('alert')
      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', expensesPath)).toBe(2)
      })
      expect(sentBody(fetchMock, 'POST', expensesPath, 1).clientRequestId).toBe(
        sentBody(fetchMock, 'POST', expensesPath, 0).clientRequestId,
      )
    })

    it.each([
      [400, 'EXPENSE_AMOUNT_TOO_LARGE'],
      [400, 'EXPENSE_SPLIT_DOES_NOT_ADD_UP'],
      [400, 'EXPENSE_CLIENT_REQUEST_ID_USED'],
      [400, 'EXPENSE_DATE_INVALID'],
      [404, 'MEMBER_NOT_FOUND'],
      [404, 'GROUP_NOT_FOUND'],
    ])('shows the translated text for %i %s, stays on the screen and does not say Expense saved', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router } = await renderReadyAdd({
        answers: { [`POST ${expensesPath}`]: problemAnswer(status, code) },
      })
      typeAmount('1200')

      fireEvent.click(saveButton())

      const alert = await screen.findByRole('alert')
      expect(alert.textContent).toContain(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.groupExpenseNew(testGroupId))
      expect(shownToastTexts()).not.toContain(translated('en', 'expenses.saved'))
    })

    it.each(languages)('shows the translated text of an error code in the language of the person (%s)', async (language) => {
      await renderReadyAdd({
        language,
        answers: { [`POST ${expensesPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE') },
      })
      typeAmount('1200', language)

      fireEvent.click(saveButton(language))

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.EXPENSE_AMOUNT_TOO_LARGE'),
      )
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderReadyAdd({ answers: { [`POST ${expensesPath}`]: networkFailureAnswer() } })
      typeAmount('1200')

      fireEvent.click(saveButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('keeps everything the person typed and chose after a failed save', async () => {
      await renderReadyAdd({
        answers: { [`POST ${expensesPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE') },
      })
      typeAmount('1200')
      typeTitle('Hotel')
      const paidBy = await openSheet('paidBy')
      fireEvent.click(personButton(paidBy, 'Ana'))
      await waitForSheetToClose()
      const category = await openSheet('category')
      fireEvent.click(within(category).getByRole('button', { name: /Accommodation/ }))
      await waitForSheetToClose()

      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      expect(amountField().value).toBe('1200')
      expect(titleField().value).toBe('Hotel')
      expect(formRow('paidBy').textContent).toContain('Ana')
      expect(formRow('category').textContent).toContain(categoryName('en', accommodationCategory))
    })

    it('saves after a failed try when the problem is gone, with the values that were typed', async () => {
      let tries = 0
      const { router } = await renderReadyAdd({
        answers: {
          [`POST ${expensesPath}`]: () => {
            tries += 1
            return tries === 1 ? problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE')() : Response.json(hotelDetail)
          },
        },
      })
      typeAmount('1200')
      fireEvent.click(saveButton())
      await screen.findByRole('alert')

      fireEvent.click(saveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
    })
  })

  describe('loading and failing', () => {
    it('shows the GROUP_NOT_FOUND text with a Go to Groups link and no retry button when the group is not found', async () => {
      await renderAdd({ answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') }).getAttribute('href')).toBe(routes.groups)
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it('shows the not-found state with a Go to Groups link when the members answer 404', async () => {
      await renderAdd({ answers: { [`GET ${membersPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') } })

      await screen.findByRole('alert')

      expect(screen.getByRole('link', { name: translated('en', 'groups.goToGroups') })).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'common.retry') })).toBeNull()
    })

    it('shows the generic error with a retry button when the members cannot be loaded, and loads the form after a retry', async () => {
      let tries = 0
      await renderAdd({
        answers: {
          [`GET ${membersPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(fiveMembers)
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      await showsForm()
    })

    it('shows the error with a retry button when the categories cannot be loaded', async () => {
      await renderAdd({ answers: { [`GET ${categoriesPath}`]: () => new Response(null, { status: 500 }) } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderAdd({ answers: { [`GET ${groupPath}`]: networkFailureAnswer() } })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })
  })

  describe('in Macedonian', () => {
    it('writes the rows, the title field and the Save button in Macedonian', async () => {
      await renderReadyAdd({ language: 'mk' })

      expect(formRow('paidBy', 'mk')).toBeTruthy()
      expect(formRow('split', 'mk')).toBeTruthy()
      expect(formRow('date', 'mk')).toBeTruthy()
      expect(formRow('category', 'mk')).toBeTruthy()
      expect(titleField('mk')).toBeTruthy()
      expect(saveButton('mk')).toBeTruthy()
    })

    it('shows Today in Macedonian in the Date row and me in Macedonian next to the payer', async () => {
      await renderReadyAdd({ language: 'mk' })

      expect(formRow('date', 'mk').textContent).toContain(translated('mk', 'expenses.today'))
      expect(formRow('paidBy', 'mk').textContent).toContain(`Filip (${translated('mk', 'expense.me')})`)
    })
  })
})
