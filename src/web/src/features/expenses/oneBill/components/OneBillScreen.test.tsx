import type { QueryClient } from '@tanstack/react-query'
import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  accommodationCategory,
  hotelExpenseId,
  testCategories,
  testToday,
  testYesterday,
} from '@/test/expenseTestData'
import {
  amountField,
  amountLeftText,
  currencyToggle,
  doneButton,
  formRow,
  isDisabled,
  openSheet,
  personBoxValue,
  personButton,
  personGroup,
  sentBody,
  sheetOf,
  splitTab,
  titleField,
  typeAmount,
  typeDate,
  typeInPerson,
  typeTitle,
  waitForSheetToClose,
  type SplitTabKey,
} from '@/test/expenseFormTestHelpers'
import {
  categoriesPath,
  categoryName,
  freezeTime,
  isBefore,
  plainSpaces,
  startsWith,
  unfreezeTime,
  uuidFormat,
} from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending, typeInto } from '@/test/formTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { filipPictureUrl } from '@/test/memberTestData'
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
import { expectTapTarget } from '@/test/tapTargetTestHelpers'
import { translated } from '@/test/translated'
import { OneBillScreen } from './OneBillScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const oneBillPath = '/api/groups/one-bill'
const createdBill = { groupId: testGroupId, expenseId: hotelExpenseId }
const nameMaxLength = 60
const titleMaxLength = 60

interface RenderOptions {
  language?: Language
  me?: Partial<Me>
  answers?: Record<string, AnswerFactory>
  seedCache?: (queryClient: QueryClient) => void
}

async function renderBill(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${oneBillPath}`]: jsonAnswer(createdBill),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.newBill, element: <OneBillScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.newGroup, element: <p>new group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.newBill,
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

function saveBillButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'oneBill.save') })
}

async function renderReadyBill(options: RenderOptions = {}) {
  const rendered = await renderBill(options)
  await screen.findByRole('button', { name: translated(options.language ?? 'en', 'oneBill.save') })
  return rendered
}

function plusCircle(language: Language = 'en'): HTMLElement {
  const label = screen.getByText(translated(language, 'oneBill.nameLabel'))
  let node: HTMLElement | null = label
  while (node !== null) {
    const button: HTMLElement | null = node.matches('button') ? node : node.querySelector('button')
    if (button !== null) {
      return button
    }
    node = node.parentElement
  }
  throw new Error('Found no button around the label of the + circle')
}

function removeButton(name: string, language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'oneBill.removeName', { name }) })
}

function nameDialog(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, 'addName.title') })
}

function sheetAddButton(language: Language = 'en'): HTMLElement {
  return within(nameDialog(language)).getByRole('button', { name: translated(language, 'addName.submit') })
}

async function openNameSheet(language: Language = 'en'): Promise<void> {
  fireEvent.click(plusCircle(language))
  await screen.findByRole('dialog', { name: translated(language, 'addName.title') })
}

function typeName(name: string, language: Language = 'en'): void {
  typeInto(translated(language, 'addName.label'), name)
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return within(nameDialog(language)).getByLabelText<HTMLInputElement>(translated(language, 'addName.label'))
}

async function addName(name: string, language: Language = 'en'): Promise<void> {
  await openNameSheet(language)
  typeName(name, language)
  fireEvent.click(sheetAddButton(language))
  await waitForSheetToClose()
}

async function addNames(...names: string[]): Promise<void> {
  for (const name of names) {
    await addName(name)
  }
}

function avatarsInOrder(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('[style*="--avatar-"], img'))
}

function colourIndexOf(avatar: HTMLElement): number {
  const match = /--avatar-(\d+)/.exec(avatar.getAttribute('style') ?? '')
  if (match === null) {
    throw new Error(`Found no avatar colour on the element: ${avatar.outerHTML}`)
  }
  return Number(match[1])
}

async function savedBody(options: RenderOptions, steps: () => Promise<void> | void) {
  const { fetchMock } = await renderReadyBill(options)
  await steps()
  fireEvent.click(saveBillButton())
  await waitFor(() => {
    expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
  })
  return sentBody(fetchMock, 'POST', oneBillPath)
}

function sharesOf(body: Record<string, unknown>): { personIndex: number; inputValue: number }[] {
  const shares = body.shares
  if (!Array.isArray(shares)) {
    throw new Error(`Expected the body to have a list of shares, found ${JSON.stringify(shares)}`)
  }
  return shares as { personIndex: number; inputValue: number }[]
}

function expectedBody(changes: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    clientRequestId: expect.stringMatching(uuidFormat),
    title: null,
    names: [],
    note: null,
    amountMinor: 180000,
    currency: 'MKD',
    expenseDate: testToday,
    categoryId: null,
    paidByPersonIndex: 0,
    splitType: 'Equal',
    shares: [{ personIndex: 0, inputValue: 0 }],
    ...changes,
  }
}

describe('OneBillScreen', () => {
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
    it.each(languages)('shows the One bill heading (%s)', async (language) => {
      await renderReadyBill({ language })

      expect(
        screen.getByRole('heading', { level: 1, name: translated(language, 'newGroup.oneBill') }),
      ).toBeTruthy()
    })

    it('has a back button to the New group choice', async () => {
      await renderReadyBill()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.newGroup)
    })

    it('shows no bottom bar', async () => {
      await renderReadyBill()

      expect(screen.queryByRole('navigation')).toBeNull()
    })

    it('puts the amount first, then the people, then the rows Paid by, Split, Date and Category, then the title, then Save bill', async () => {
      await renderReadyBill()

      const ordered: HTMLElement[] = [
        amountField(),
        screen.getByText(translated('en', 'oneBill.people')),
        screen.getByText(translated('en', 'expense.me')),
        plusCircle(),
        formRow('paidBy'),
        formRow('split'),
        formRow('date'),
        formRow('category'),
        titleField(),
        saveBillButton(),
      ]

      for (let index = 1; index < ordered.length; index += 1) {
        expect(isBefore(ordered[index - 1], ordered[index])).toBe(true)
      }
    })

    it.each(languages)('writes the people heading, the label of the + circle and the Save bill button (%s)', async (language) => {
      await renderReadyBill({ language })

      expect(screen.getByText(translated(language, 'oneBill.people'))).toBeTruthy()
      expect(screen.getByText(translated(language, 'oneBill.nameLabel'))).toBeTruthy()
      expect(saveBillButton(language)).toBeTruthy()
    })

    it('focuses the amount field when the screen opens and shows it empty', async () => {
      await renderReadyBill()

      expect(document.activeElement).toBe(amountField())
      expect(amountField().value).toBe('')
    })

    it('shows the title field empty with its optional label', async () => {
      await renderReadyBill()

      expect(titleField().value).toBe('')
    })

    it('asks only for the categories: it needs no group, no members and no expenses', async () => {
      const { fetchMock } = await renderReadyBill()

      expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual([categoriesPath])
    })

    it('shows the loading spinner and no Save bill button while the categories are being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: routes.newBill, element: <OneBillScreen /> }],
        routes.newBill,
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'oneBill.save') })).toBeNull()
    })

    it('shows the error with a retry button when the categories cannot be loaded, and the form after a retry', async () => {
      let tries = 0
      await renderBill({
        answers: {
          [`GET ${categoriesPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json({ categories: testCategories })
          },
        },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await screen.findByRole('button', { name: translated('en', 'oneBill.save') })).toBeTruthy()
    })
  })

  describe('the amount and the currency', () => {
    it('sends whole denars as deni for MKD, which is the currency at first', async () => {
      const body = await savedBody({}, () => {
        expect(currencyToggle('MKD')).toBeTruthy()
        typeAmount('1800')
      })

      expect(body).toMatchObject({ amountMinor: 180000, currency: 'MKD' })
    })

    it('shows a number keypad for MKD and a decimal keypad for EUR', async () => {
      await renderReadyBill()

      expect(amountField().inputMode).toBe('numeric')

      fireEvent.click(currencyToggle('MKD'))

      expect(amountField().inputMode).toBe('decimal')
    })

    it.each(languages)('switches between MKD and EUR when the currency label is pressed, with the label saying so (%s)', async (language) => {
      await renderReadyBill({ language })

      fireEvent.click(currencyToggle('MKD', language))
      expect(currencyToggle('EUR', language)).toBeTruthy()

      fireEvent.click(currencyToggle('EUR', language))
      expect(currencyToggle('MKD', language)).toBeTruthy()
    })

    it('sends the amount in cents and the currency EUR after the label was switched', async () => {
      const body = await savedBody({}, () => {
        fireEvent.click(currencyToggle('MKD'))
        typeAmount('12,50')
      })

      expect(body).toMatchObject({ amountMinor: 1250, currency: 'EUR' })
    })

    it('keeps the typed amount and clears the typed Exact amounts when the currency is switched', async () => {
      await renderReadyBill()
      typeAmount('3000')
      await addNames('Marko')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Marko', '480')
      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      fireEvent.click(currencyToggle('MKD'))

      expect(amountField().value).toBe('3000')
      const again = await openSheet('split')
      expect(within(personGroup(again, 'Marko')).getByRole<HTMLInputElement>('textbox').value).toBe('')
    })

    it.each([
      ['an empty amount', ''],
      ['zero', '0'],
      ['letters', 'abc'],
      ['a decimal in MKD', '12.5'],
      ['a thousands comma in MKD', '1,200'],
      ['13 digits', '1234567890123'],
    ])('keeps Save bill disabled and sends nothing for %s', async (_name, text) => {
      const { fetchMock } = await renderReadyBill()
      typeAmount(text)

      fireEvent.click(saveBillButton())

      expect(isDisabled(saveBillButton())).toBe(true)
      expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(0)
    })

    it('keeps Save bill disabled for an amount with three decimals in EUR', async () => {
      await renderReadyBill()
      fireEvent.click(currencyToggle('MKD'))

      typeAmount('12.555')

      expect(isDisabled(saveBillButton())).toBe(true)
    })

    it('enables Save bill when the amount is valid', async () => {
      await renderReadyBill()

      typeAmount('1800')

      expect(isDisabled(saveBillButton())).toBe(false)
    })
  })

  describe('the people', () => {
    it('starts with my circle only, labelled me, and the + circle labelled name', async () => {
      await renderReadyBill()

      expect(screen.getByText(translated('en', 'expense.me'))).toBeTruthy()
      expect(plusCircle()).toBeTruthy()
      expect(avatarsInOrder()).toHaveLength(1)
      expect(screen.queryByRole('button', { name: startsWith('Remove') })).toBeNull()
    })

    it('shows my picture in my circle when the account has one, without a referrer', async () => {
      await renderReadyBill({ me: { pictureUrl: filipPictureUrl } })

      const [mine] = avatarsInOrder()

      expect(mine.tagName).toBe('IMG')
      expect(mine.getAttribute('src')).toBe(filipPictureUrl)
      expect(mine.getAttribute('referrerpolicy')).toBe('no-referrer')
    })

    it('shows my initial on the colour 0 when the account has no picture', async () => {
      await renderReadyBill({ me: { pictureUrl: null } })

      const [mine] = avatarsInOrder()

      expect(mine.textContent).toBe('F')
      expect(colourIndexOf(mine)).toBe(0)
    })

    it('has no remove button for me', async () => {
      await renderReadyBill()
      await addNames('Marko')

      expect(screen.queryByRole('button', { name: translated('en', 'oneBill.removeName', { name: 'Filip' }) })).toBeNull()
      expect(removeButton('Marko')).toBeTruthy()
    })

    it('opens a sheet titled Add a name with the name field, its hint and the Add button when the + circle is pressed', async () => {
      await renderReadyBill()

      await openNameSheet()

      const sheet = nameDialog()
      expect(within(sheet).getByLabelText(translated('en', 'addName.label'))).toBeTruthy()
      expect(within(sheet).getByText(translated('en', 'addName.hint'))).toBeTruthy()
      expect(sheetAddButton()).toBeTruthy()
    })

    it('shows no sheet before the + circle is pressed', async () => {
      await renderReadyBill()

      expect(screen.queryByRole('dialog')).toBeNull()
    })

    it.each(languages)('adds a name, closes the sheet and shows its circle with a remove button (%s)', async (language) => {
      await renderReadyBill({ language })

      await addName('Marko', language)

      expect(screen.getByText('Marko')).toBeTruthy()
      expect(removeButton('Marko', language)).toBeTruthy()
      expect(avatarsInOrder()).toHaveLength(2)
    })

    it('adds a name when the form of the sheet is submitted with the Enter key', async () => {
      await renderReadyBill()
      await openNameSheet()
      typeName('Marko')
      const form = within(nameDialog()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).form
      if (form === null) {
        throw new Error('The name field is not inside a form')
      }

      fireEvent.submit(form)

      await waitForSheetToClose()
      expect(removeButton('Marko')).toBeTruthy()
    })

    it('shows the circles in the order the names were added, after mine, with the + circle last', async () => {
      await renderReadyBill()

      await addNames('Marko', 'Ana')

      const me = screen.getByText(translated('en', 'expense.me'))
      expect(isBefore(me, removeButton('Marko'))).toBe(true)
      expect(isBefore(removeButton('Marko'), removeButton('Ana'))).toBe(true)
      expect(isBefore(removeButton('Ana'), plusCircle())).toBe(true)
    })

    it('shows the initial of every name on the colour of its place in the order, and no picture', async () => {
      await renderReadyBill()

      await addNames('Marko', 'Ana', 'Борис')

      const avatars = avatarsInOrder()
      expect(avatars.map((avatar) => avatar.textContent)).toEqual(['F', 'M', 'A', 'Б'])
      expect(avatars.map(colourIndexOf)).toEqual([0, 1, 2, 3])
    })

    it('gives the circles after a removed one the colour of their new place', async () => {
      await renderReadyBill()
      await addNames('Marko', 'Ana')

      fireEvent.click(removeButton('Marko'))

      expect(screen.queryByRole('button', { name: translated('en', 'oneBill.removeName', { name: 'Marko' }) })).toBeNull()
      const avatars = avatarsInOrder()
      expect(avatars.map((avatar) => avatar.textContent)).toEqual(['F', 'A'])
      expect(avatars.map(colourIndexOf)).toEqual([0, 1])
    })

    it('removes a circle with one tap on its remove button', async () => {
      await renderReadyBill()
      await addNames('Marko')

      fireEvent.click(removeButton('Marko'))

      expect(avatarsInOrder()).toHaveLength(1)
      expect(screen.queryByText('Marko')).toBeNull()
    })

    it('lets the same name be added again after it was removed', async () => {
      await renderReadyBill()
      await addNames('Marko')
      fireEvent.click(removeButton('Marko'))

      await addName('Marko')

      expect(removeButton('Marko')).toBeTruthy()
    })

    it('sends no request while names are added and removed', async () => {
      const { fetchMock } = await renderReadyBill()

      await addNames('Marko', 'Ana')
      fireEvent.click(removeButton('Marko'))

      expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual([categoriesPath])
    })

    it('closes the sheet without adding a name when Escape is pressed', async () => {
      await renderReadyBill()
      await openNameSheet()
      typeName('Marko')

      fireEvent.keyDown(nameDialog(), { key: 'Escape' })

      await waitForSheetToClose()
      expect(avatarsInOrder()).toHaveLength(1)
    })

    it('closes the sheet without adding a name when its Close button is pressed', async () => {
      await renderReadyBill()
      await openNameSheet()

      fireEvent.click(within(nameDialog()).getByRole('button', { name: translated('en', 'common.close') }))

      await waitForSheetToClose()
      expect(avatarsInOrder()).toHaveLength(1)
    })

    it('shows the field empty and without an error when the sheet is opened again', async () => {
      await renderReadyBill()
      await openNameSheet()
      fireEvent.click(sheetAddButton())
      await within(nameDialog()).findByRole('alert')
      fireEvent.keyDown(nameDialog(), { key: 'Escape' })
      await waitForSheetToClose()

      await openNameSheet()

      expect(within(nameDialog()).getByLabelText<HTMLInputElement>(translated('en', 'addName.label')).value).toBe('')
      expect(within(nameDialog()).queryByRole('alert')).toBeNull()
    })

    describe('the names that are refused', () => {
      it.each(languages)('refuses an empty name with the MEMBER_NAME_INVALID text inside the sheet, keeps the sheet open and adds no circle (%s)', async (language) => {
        await renderReadyBill({ language })
        await openNameSheet(language)

        fireEvent.click(sheetAddButton(language))

        expect((await within(nameDialog(language)).findByRole('alert')).textContent).toContain(
          translated(language, 'errors.MEMBER_NAME_INVALID'),
        )
        expect(avatarsInOrder()).toHaveLength(1)
      })

      it('refuses a name of only spaces like an empty one', async () => {
        await renderReadyBill()
        await openNameSheet()
        typeName('   ')

        fireEvent.click(sheetAddButton())

        expect((await within(nameDialog()).findByRole('alert')).textContent).toContain(
          translated('en', 'errors.MEMBER_NAME_INVALID'),
        )
        expect(avatarsInOrder()).toHaveLength(1)
      })

      it.each(languages)('refuses a name of more than 60 characters with the MEMBER_NAME_INVALID text and keeps the typed name (%s)', async (language) => {
        await renderReadyBill({ language })
        await openNameSheet(language)
        const tooLong = 'a'.repeat(nameMaxLength + 1)
        typeName(tooLong, language)

        fireEvent.click(sheetAddButton(language))

        expect((await within(nameDialog(language)).findByRole('alert')).textContent).toContain(
          translated(language, 'errors.MEMBER_NAME_INVALID'),
        )
        expect(
          within(nameDialog(language)).getByLabelText<HTMLInputElement>(translated(language, 'addName.label')).value,
        ).toBe(tooLong)
        expect(avatarsInOrder()).toHaveLength(1)
      })

      it('accepts a name of exactly 60 characters', async () => {
        await renderReadyBill()

        await addName('a'.repeat(nameMaxLength))

        expect(avatarsInOrder()).toHaveLength(2)
      })

      it.each(languages)('refuses a name that repeats another one in other letter case with the MEMBER_NAME_TAKEN text and the typed name (%s)', async (language) => {
        await renderReadyBill({ language })
        await addName('Marko', language)
        await openNameSheet(language)
        typeName('marko', language)

        fireEvent.click(sheetAddButton(language))

        expect((await within(nameDialog(language)).findByRole('alert')).textContent).toBe(
          translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'marko' }),
        )
        expect(avatarsInOrder()).toHaveLength(2)
      })

      it.each(languages)('refuses the account name of the person who is making the bill, in any letter case, with the MEMBER_NAME_TAKEN text (%s)', async (language) => {
        await renderReadyBill({ language })
        await openNameSheet(language)
        typeName('FILIP', language)

        fireEvent.click(sheetAddButton(language))

        expect((await within(nameDialog(language)).findByRole('alert')).textContent).toBe(
          translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'FILIP' }),
        )
        expect(avatarsInOrder()).toHaveLength(1)
      })

      it('words the name-taken message exactly with the typed name between the quotes', async () => {
        await renderReadyBill()
        await addName('Marko')
        await openNameSheet()
        typeName('MARKO')

        fireEvent.click(sheetAddButton())

        expect((await within(nameDialog()).findByRole('alert')).textContent).toBe(
          'Someone called “MARKO” is already in this group.',
        )
      })

      it.each(languages)('does not mark the name field as invalid before a name was refused (%s)', async (language) => {
        await renderReadyBill({ language })

        await openNameSheet(language)

        expect(nameField(language).getAttribute('aria-invalid')).not.toBe('true')
      })

      it.each(languages)('marks the name field with aria-invalid="true" when an empty name was refused (%s)', async (language) => {
        await renderReadyBill({ language })
        await openNameSheet(language)

        fireEvent.click(sheetAddButton(language))

        await within(nameDialog(language)).findByRole('alert')
        expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
      })

      it.each(languages)('marks the name field with aria-invalid="true" when a name of more than 60 characters was refused (%s)', async (language) => {
        await renderReadyBill({ language })
        await openNameSheet(language)
        typeName('a'.repeat(nameMaxLength + 1), language)

        fireEvent.click(sheetAddButton(language))

        await within(nameDialog(language)).findByRole('alert')
        expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
      })

      it.each(languages)('marks the name field with aria-invalid="true" when a name that is taken was refused (%s)', async (language) => {
        await renderReadyBill({ language })
        await addName('Marko', language)
        await openNameSheet(language)
        typeName('marko', language)

        fireEvent.click(sheetAddButton(language))

        await within(nameDialog(language)).findByRole('alert')
        expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
      })

      it('clears the aria-invalid mark of the name field as soon as the typed text changes', async () => {
        await renderReadyBill()
        await addName('Marko')
        await openNameSheet()
        typeName('marko')
        fireEvent.click(sheetAddButton())
        await within(nameDialog()).findByRole('alert')

        typeName('marko P')

        expect(nameField().getAttribute('aria-invalid')).not.toBe('true')
      })

      it('marks the name field again when the corrected name is refused too', async () => {
        await renderReadyBill()
        await addName('Marko')
        await openNameSheet()
        typeName('marko')
        fireEvent.click(sheetAddButton())
        await within(nameDialog()).findByRole('alert')
        typeName('MARKO')

        fireEvent.click(sheetAddButton())

        await waitFor(() => {
          expect(nameField().getAttribute('aria-invalid')).toBe('true')
        })
      })

      it('shows a new empty sheet without the mark after a refused name was corrected and added', async () => {
        await renderReadyBill()
        await openNameSheet()
        fireEvent.click(sheetAddButton())
        await within(nameDialog()).findByRole('alert')
        typeName('Marko')
        fireEvent.click(sheetAddButton())
        await waitForSheetToClose()

        await openNameSheet()

        expect(nameField().getAttribute('aria-invalid')).not.toBe('true')
      })

      it('lets a name that was refused be corrected and added', async () => {
        await renderReadyBill()
        await addName('Marko')
        await openNameSheet()
        typeName('marko')
        fireEvent.click(sheetAddButton())
        await within(nameDialog()).findByRole('alert')

        typeName('Marko Petrov')
        fireEvent.click(sheetAddButton())

        await waitForSheetToClose()
        expect(removeButton('Marko Petrov')).toBeTruthy()
      })
    })
  })

  describe('the tap targets of the people circles, each at least 44 px: the class min-h-11 and the class min-w-11 on the tapped element', () => {
    it('gives the Remove button of a name a tap target', async () => {
      await renderReadyBill()
      await addName('Marko')

      expectTapTarget(removeButton('Marko'), 'the Remove button of Marko')
    })

    it('gives the Remove button of every name a tap target', async () => {
      await renderReadyBill()
      await addNames('Marko', 'Ana')

      expectTapTarget(removeButton('Marko'), 'the Remove button of Marko')
      expectTapTarget(removeButton('Ana'), 'the Remove button of Ana')
    })

    it('gives the + circle with its name label a tap target', async () => {
      await renderReadyBill()

      expectTapTarget(plusCircle(), 'the button of the + circle')
    })
  })

  describe('the Paid by sheet', () => {
    it('is pre-filled with me', async () => {
      await renderReadyBill()

      expect(formRow('paidBy').textContent).toContain(`Filip (${translated('en', 'expense.me')})`)
    })

    it('lists me and the names in the order of the circles, with a tick on the payer only', async () => {
      await renderReadyBill()
      await addNames('Marko', 'Ana')

      const sheet = await openSheet('paidBy')

      const buttons = ['Filip', 'Marko', 'Ana'].map((name) => personButton(sheet, name))
      expect(isBefore(buttons[0], buttons[1])).toBe(true)
      expect(isBefore(buttons[1], buttons[2])).toBe(true)
      expect(personButton(sheet, 'Filip').textContent).toContain(`(${translated('en', 'expense.me')})`)
      expect(personButton(sheet, 'Filip').getAttribute('aria-pressed')).toBe('true')
      expect(personButton(sheet, 'Marko').getAttribute('aria-pressed')).toBe('false')
    })

    it('shows my picture and the colours of the circles in the sheet', async () => {
      await renderReadyBill({ me: { pictureUrl: filipPictureUrl } })
      await addNames('Marko')

      const sheet = await openSheet('paidBy')

      expect(personButton(sheet, 'Filip').querySelector('img')?.getAttribute('src')).toBe(filipPictureUrl)
      expect(personButton(sheet, 'Marko').innerHTML).toMatch(/--avatar-1(?!\d)/)
    })

    it('closes with one tap on a name, shows that name in the row and sends the index of that name', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        const sheet = await openSheet('paidBy')
        fireEvent.click(personButton(sheet, 'Ana'))
        await waitForSheetToClose()
        expect(formRow('paidBy').textContent).toContain('Ana')
      })

      expect(body.paidByPersonIndex).toBe(2)
      expect(body.names).toEqual(['Marko', 'Ana'])
    })

    it('sends the payer 0 when nobody else is picked', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko')
      })

      expect(body.paidByPersonIndex).toBe(0)
    })

    it('moves the payer back to me and sends 0 when the payer is removed from the circles', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        const sheet = await openSheet('paidBy')
        fireEvent.click(personButton(sheet, 'Marko'))
        await waitForSheetToClose()

        fireEvent.click(removeButton('Marko'))

        expect(formRow('paidBy').textContent).toContain(`Filip (${translated('en', 'expense.me')})`)
      })

      expect(body.paidByPersonIndex).toBe(0)
      expect(body.names).toEqual(['Ana'])
    })

    it('keeps the payer when another person is removed, and points at the new index', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        const sheet = await openSheet('paidBy')
        fireEvent.click(personButton(sheet, 'Ana'))
        await waitForSheetToClose()

        fireEvent.click(removeButton('Marko'))
      })

      expect(body.paidByPersonIndex).toBe(1)
      expect(body.names).toEqual(['Ana'])
    })
  })

  describe('the Split sheet', () => {
    it('is pre-filled with equally, everyone', async () => {
      await renderReadyBill()
      await addNames('Marko')

      const text = formRow('split').textContent ?? ''

      expect(text).toContain(translated('en', 'expense.equally'))
      expect(text).toContain(translated('en', 'expense.everyone'))
    })

    it.each(languages)('opens a sheet titled Split with the four tabs, Equal selected (%s)', async (language) => {
      await renderReadyBill({ language })

      const sheet = await openSheet('split', language)

      expect(splitTab(sheet, 'splitEqual', language).getAttribute('aria-selected')).toBe('true')
      for (const tab of ['splitExact', 'splitPercentage', 'splitShares'] as const) {
        expect(splitTab(sheet, tab, language).getAttribute('aria-selected')).toBe('false')
      }
    })

    it('lists me and every name in the order of the circles, all in the split', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko', 'Ana')

      const sheet = await openSheet('split')

      expect(within(sheet).getAllByRole('group')).toHaveLength(3)
      for (const name of ['Filip', 'Marko', 'Ana']) {
        expect(
          within(personGroup(sheet, name)).getByRole('button', { name: startsWith(name) }).getAttribute('aria-pressed'),
        ).toBe('true')
      }
    })

    it('splits equally among me and the names, in the order of the circles, with no extras', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
      })

      expect(body.splitType).toBe('Equal')
      expect(sharesOf(body)).toEqual([
        { personIndex: 0, inputValue: 0 },
        { personIndex: 1, inputValue: 0 },
        { personIndex: 2, inputValue: 0 },
      ])
    })

    it('sends only the people who are in the split, by index', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        const sheet = await openSheet('split')
        fireEvent.click(within(personGroup(sheet, 'Marko')).getByRole('button', { name: startsWith('Marko') }))
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(sharesOf(body)).toEqual([
        { personIndex: 0, inputValue: 0 },
        { personIndex: 2, inputValue: 0 },
      ])
    })

    it('sends the typed amounts by index when an Exact split adds up', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitExact'))
        typeInPerson(sheet, 'Filip', '600')
        typeInPerson(sheet, 'Marko', '300')
        typeInPerson(sheet, 'Ana', '900')
        expect(isDisabled(doneButton(sheet))).toBe(false)
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body.splitType).toBe('Exact')
      expect(sharesOf(body)).toEqual([
        { personIndex: 0, inputValue: 60000 },
        { personIndex: 1, inputValue: 30000 },
        { personIndex: 2, inputValue: 90000 },
      ])
    })

    it('does not list a person who was removed from the circles', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko', 'Ana')
      fireEvent.click(removeButton('Marko'))

      const sheet = await openSheet('split')

      expect(within(sheet).getAllByRole('group')).toHaveLength(2)
      expect(within(sheet).queryByRole('group', { name: startsWith('Marko') })).toBeNull()
    })

    it('does not send a person who was removed from the circles and numbers the rest again', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko', 'Ana')
        fireEvent.click(removeButton('Marko'))
      })

      expect(body.names).toEqual(['Ana'])
      expect(sharesOf(body)).toEqual([
        { personIndex: 0, inputValue: 0 },
        { personIndex: 1, inputValue: 0 },
      ])
    })

    it('keeps Save bill disabled when a removed person leaves an Exact split that no longer adds up, and enables it again when it does', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko', 'Ana')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Filip', '600')
      typeInPerson(sheet, 'Marko', '600')
      typeInPerson(sheet, 'Ana', '600')
      fireEvent.click(doneButton(sheet))
      await waitForSheetToClose()
      expect(isDisabled(saveBillButton())).toBe(false)

      fireEvent.click(removeButton('Marko'))

      expect(isDisabled(saveBillButton())).toBe(true)
      const again = await openSheet('split')
      typeInPerson(again, 'Ana', '1200')
      fireEvent.click(doneButton(again))
      await waitForSheetToClose()
      expect(isDisabled(saveBillButton())).toBe(false)
    })

    it('keeps Save bill disabled while the split does not add up, even with a valid amount', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko', 'Ana')
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Filip', '600')

      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      expect(isDisabled(saveBillButton())).toBe(true)
    })

    it('keeps Save bill disabled when nobody is in the split', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko')
      const sheet = await openSheet('split')
      for (const name of ['Filip', 'Marko']) {
        fireEvent.click(within(personGroup(sheet, name)).getByRole('button', { name: startsWith(name) }))
      }

      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      expect(isDisabled(saveBillButton())).toBe(true)
    })
  })

  describe('the auto-fill of the Split sheet with two people', () => {
    async function openSheetOnTab(tab: SplitTabKey, ...names: string[]): Promise<HTMLElement> {
      await renderReadyBill()
      typeAmount('1800')
      await addNames(...names)
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, tab))
      return sheet
    }

    it('fills the name with the rest of the amount when I type an amount on the Exact tab', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')

      typeInPerson(sheet, 'Filip', '600')

      expect(personBoxValue(sheet, 'Filip')).toBe('600')
      expect(personBoxValue(sheet, 'Marko')).toBe('1200')
      expect(within(sheet).queryByText(amountLeftText(0, 'MKD'), { normalizer: plainSpaces })).toBeNull()
      expect(isDisabled(doneButton(sheet))).toBe(false)
    })

    it('fills me with the rest of the amount when an amount is typed for the name', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')

      typeInPerson(sheet, 'Marko', '500')

      expect(personBoxValue(sheet, 'Filip')).toBe('1300')
      expect(personBoxValue(sheet, 'Marko')).toBe('500')
    })

    it.each([
      ['30', '70'],
      ['33.33', '66.67'],
    ])('fills the name with the rest of 100 percent when I type %s on the percentage tab: %s', async (typed, rest) => {
      const sheet = await openSheetOnTab('splitPercentage', 'Marko')

      typeInPerson(sheet, 'Filip', typed)

      expect(personBoxValue(sheet, 'Marko')).toBe(rest)
      expect(isDisabled(doneButton(sheet))).toBe(false)
    })

    it('keeps the amount of the first box and shows what is left when the second box is typed in too', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')
      typeInPerson(sheet, 'Filip', '600')

      typeInPerson(sheet, 'Marko', '500')

      expect(personBoxValue(sheet, 'Filip')).toBe('600')
      expect(personBoxValue(sheet, 'Marko')).toBe('500')
      expect(within(sheet).getByText(amountLeftText(70000, 'MKD'), { normalizer: plainSpaces })).toBeTruthy()
      expect(isDisabled(doneButton(sheet))).toBe(true)
    })

    it('leaves the other box empty and shows the amount left with a minus when the typed amount is above the total', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')

      typeInPerson(sheet, 'Filip', '2000')

      expect(personBoxValue(sheet, 'Marko')).toBe('')
      expect(within(sheet).getByText(amountLeftText(-20000, 'MKD'), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('enables Save bill after the sheet is closed when the filled split adds up', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')
      typeInPerson(sheet, 'Filip', '600')

      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      expect(isDisabled(saveBillButton())).toBe(false)
    })

    it('sends both amounts by index when the filled Exact split is saved', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        await addNames('Marko')
        const sheet = await openSheet('split')
        fireEvent.click(splitTab(sheet, 'splitExact'))
        typeInPerson(sheet, 'Filip', '600')
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
      })

      expect(body.splitType).toBe('Exact')
      expect(sharesOf(body)).toEqual([
        { personIndex: 0, inputValue: 60000 },
        { personIndex: 1, inputValue: 120000 },
      ])
    })

    it('fills the box nobody typed in again when the amount is changed after the sheet was closed', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')
      typeInPerson(sheet, 'Filip', '600')
      fireEvent.keyDown(sheet, { key: 'Escape' })
      await waitForSheetToClose()

      typeAmount('2000')

      const again = await openSheet('split')
      expect(personBoxValue(again, 'Filip')).toBe('600')
      expect(personBoxValue(again, 'Marko')).toBe('1400')
      expect(isDisabled(doneButton(again))).toBe(false)
    })

    it('fills nobody with two names and me on the Exact tab', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko', 'Ana')

      typeInPerson(sheet, 'Filip', '600')

      expect(personBoxValue(sheet, 'Filip')).toBe('600')
      expect(personBoxValue(sheet, 'Marko')).toBe('')
      expect(personBoxValue(sheet, 'Ana')).toBe('')
    })

    it('fills the one person who is left when a name was removed from the circles and the Exact tab is opened', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko', 'Ana')
      fireEvent.click(removeButton('Marko'))
      const sheet = await openSheet('split')
      fireEvent.click(splitTab(sheet, 'splitExact'))

      typeInPerson(sheet, 'Filip', '600')

      expect(personBoxValue(sheet, 'Ana')).toBe('1200')
    })

    it('fills nobody when I am the only person', async () => {
      const sheet = await openSheetOnTab('splitExact')

      typeInPerson(sheet, 'Filip', '600')

      expect(personBoxValue(sheet, 'Filip')).toBe('600')
      expect(within(sheet).getByText(amountLeftText(120000, 'MKD'), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('fills nobody on the Equal tab, where a typed amount is an extra', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko')
      const sheet = await openSheet('split')

      fireEvent.click(within(personGroup(sheet, 'Filip')).getByRole('button', { name: translated('en', 'expense.extra') }))
      typeInPerson(sheet, 'Filip', '300')

      expect(personBoxValue(sheet, 'Filip')).toBe('300')
      expect(within(personGroup(sheet, 'Marko')).queryByRole('textbox')).toBeNull()
    })

    it('forgets what was typed and filled when another tab is opened, and fills again from nothing', async () => {
      const sheet = await openSheetOnTab('splitExact', 'Marko')
      typeInPerson(sheet, 'Filip', '600')

      fireEvent.click(splitTab(sheet, 'splitPercentage'))
      expect(personBoxValue(sheet, 'Filip')).toBe('')
      expect(personBoxValue(sheet, 'Marko')).toBe('')
      fireEvent.click(splitTab(sheet, 'splitExact'))
      typeInPerson(sheet, 'Marko', '500')

      expect(personBoxValue(sheet, 'Filip')).toBe('1300')
    })
  })

  describe('the Date and Category sheets', () => {
    it('is pre-filled with Today and leaves the category empty', async () => {
      await renderReadyBill()

      expect(formRow('date').textContent).toContain(translated('en', 'expenses.today'))
      for (const category of testCategories) {
        expect(formRow('category').textContent).not.toContain(categoryName('en', category))
      }
    })

    it('sends today in the time zone of the account', async () => {
      const body = await savedBody({ me: { timeZone: 'Pacific/Pago_Pago' } }, () => {
        typeAmount('1800')
      })

      expect(body.expenseDate).toBe('2026-10-05')
    })

    it('closes with one tap on Yesterday, shows Yesterday in the row and sends yesterday', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        const sheet = await openSheet('date')
        fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'expenses.yesterday') }))
        await waitForSheetToClose()
        expect(formRow('date').textContent).toContain(translated('en', 'expenses.yesterday'))
      })

      expect(body.expenseDate).toBe(testYesterday)
    })

    it('applies a typed date with Done in the Date sheet, closes it and sends that date', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        const sheet = await openSheet('date')
        typeDate(sheet, '2026-12-12')
        expect(sheetOf('date')).toBeTruthy()
        fireEvent.click(doneButton(sheet))
        await waitForSheetToClose()
        expect(formRow('date').textContent).toContain('12 Dec')
      })

      expect(body.expenseDate).toBe('2026-12-12')
    })

    it('keeps Done disabled in the Date sheet while the typed date is empty or out of range', async () => {
      await renderReadyBill()
      const sheet = await openSheet('date')

      typeDate(sheet, '')
      expect(isDisabled(doneButton(sheet))).toBe(true)
      typeDate(sheet, '2062-10-06')
      expect(isDisabled(doneButton(sheet))).toBe(true)
      typeDate(sheet, '2026-12-12')
      expect(isDisabled(doneButton(sheet))).toBe(false)
    })

    it('closes with one tap on a category, shows its emoji and name in the row and sends its id', async () => {
      const body = await savedBody({}, async () => {
        typeAmount('1800')
        const sheet = await openSheet('category')
        fireEvent.click(within(sheet).getByRole('button', { name: /Accommodation/ }))
        await waitForSheetToClose()
        expect(formRow('category').textContent).toContain(accommodationCategory.emoji)
        expect(formRow('category').textContent).toContain(categoryName('en', accommodationCategory))
      })

      expect(body.categoryId).toBe(accommodationCategory.id)
    })
  })

  describe('the title', () => {
    it('sends the typed title', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1800')
        typeTitle('Dinner at Mario')
      })

      expect(body.title).toBe('Dinner at Mario')
    })

    it('sends null when no title is typed', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1800')
      })

      expect(body.title).toBeNull()
    })

    it('sends a title of exactly 60 characters', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1800')
        typeTitle('a'.repeat(titleMaxLength))
      })

      expect(body.title).toBe('a'.repeat(titleMaxLength))
    })

    it.each(languages)('shows the oneBill.titleTooLong text inline, not the server text, and sends nothing when the title is longer than 60 characters (%s)', async (language) => {
      const { fetchMock } = await renderReadyBill({ language })
      typeAmount('1800', language)
      typeTitle('a'.repeat(titleMaxLength + 1), language)

      fireEvent.click(saveBillButton(language))

      const alert = await screen.findByRole('alert')
      expect(alert.textContent).toContain(translated(language, 'oneBill.titleTooLong'))
      expect(alert.textContent).not.toContain(translated(language, 'errors.EXPENSE_TITLE_INVALID'))
      expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(0)
    })

    it('keeps everything that was typed after the title error', async () => {
      await renderReadyBill()
      typeAmount('1800')
      await addNames('Marko')
      const tooLong = 'a'.repeat(titleMaxLength + 1)
      typeTitle(tooLong)

      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')

      expect(titleField().value).toBe(tooLong)
      expect(amountField().value).toBe('1800')
      expect(removeButton('Marko')).toBeTruthy()
    })

    it('sends the bill once the title is shortened after the title error', async () => {
      const { fetchMock } = await renderReadyBill()
      typeAmount('1800')
      typeTitle('a'.repeat(titleMaxLength + 1))
      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')

      typeTitle('a'.repeat(titleMaxLength))
      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
      })
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('saving', () => {
    it('sends exactly the fields of the One bill request as JSON, with no emoji', async () => {
      const { fetchMock } = await renderReadyBill()
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', oneBillPath)[0].contentType).toBe('application/json')
      expect(sentBody(fetchMock, 'POST', oneBillPath)).toEqual(expectedBody())
      expect(sentBody(fetchMock, 'POST', oneBillPath)).not.toHaveProperty('emoji')
    })

    it('sends a whole bill: amount, currency, title, names, payer, split, date and category', async () => {
      const body = await savedBody({}, async () => {
        fireEvent.click(currencyToggle('MKD'))
        typeAmount('45.50')
        typeTitle('Taxi to the airport')
        await addNames('Marko', 'Ana')
        const paidBy = await openSheet('paidBy')
        fireEvent.click(personButton(paidBy, 'Marko'))
        await waitForSheetToClose()
        const date = await openSheet('date')
        fireEvent.click(within(date).getByRole('button', { name: translated('en', 'expenses.yesterday') }))
        await waitForSheetToClose()
        const category = await openSheet('category')
        fireEvent.click(within(category).getByRole('button', { name: /Accommodation/ }))
        await waitForSheetToClose()
      })

      expect(body).toEqual(
        expectedBody({
          title: 'Taxi to the airport',
          names: ['Marko', 'Ana'],
          amountMinor: 4550,
          currency: 'EUR',
          expenseDate: testYesterday,
          categoryId: accommodationCategory.id,
          paidByPersonIndex: 1,
          shares: [
            { personIndex: 0, inputValue: 0 },
            { personIndex: 1, inputValue: 0 },
            { personIndex: 2, inputValue: 0 },
          ],
        }),
      )
    })

    it('sends a bill for one person when no name is added', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1800')
      })

      expect(body.names).toEqual([])
      expect(sharesOf(body)).toEqual([{ personIndex: 0, inputValue: 0 }])
    })

    it('sends a clientRequestId that is a new id', async () => {
      const body = await savedBody({}, () => {
        typeAmount('1800')
      })

      expect(String(body.clientRequestId)).toMatch(uuidFormat)
    })

    it('uses another clientRequestId every time the screen is opened', async () => {
      const first = await savedBody({}, () => {
        typeAmount('1800')
      })
      cleanup()

      const second = await savedBody({}, () => {
        typeAmount('1800')
      })

      expect(second.clientRequestId).not.toBe(first.clientRequestId)
    })

    it.each(languages)('shows the Expense saved toast and goes to the new group, replacing the form in the history (%s)', async (language) => {
      const { router } = await renderReadyBill({ language })
      typeAmount('1800', language)
      expect(shownToastTexts()).not.toContain(translated(language, 'expenses.saved'))

      fireEvent.click(saveBillButton(language))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(shownToastTexts()).toContain(translated(language, 'expenses.saved'))
      expect(screen.getByText('group page')).toBeTruthy()
    })

    it('goes to the group of the answer, whatever its id is', async () => {
      const { router } = await renderReadyBill({
        answers: {
          [`POST ${oneBillPath}`]: jsonAnswer({ groupId: '9c8b7a65-4321-4fed-8cba-0987654321ab', expenseId: hotelExpenseId }),
        },
      })
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe('/groups/9c8b7a65-4321-4fed-8cba-0987654321ab')
      })
    })

    it('marks the group list as out of date after the bill was saved, so the new group shows up in it', async () => {
      const { queryClient } = await renderReadyBill({
        seedCache: (cache) => {
          cache.setQueryData(['groups'], { groups: [], finishedGroups: [], recentlyDeleted: [] })
        },
      })
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(queryClient.getQueryState(['groups'])?.isInvalidated).toBe(true)
      })
    })

    it('disables Save bill while the request waits for an answer', async () => {
      await renderReadyBill()
      typeAmount('1800')
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

      fireEvent.click(saveBillButton())

      await expectDisabledWhilePending(saveBillButton())
    })

    it('sends the same clientRequestId with every request of a double tap, so the server saves the bill once', async () => {
      await renderReadyBill()
      typeAmount('1800')
      const neverAnswering = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
      vi.stubGlobal('fetch', neverAnswering)

      fireEvent.click(saveBillButton())
      fireEvent.click(saveBillButton())

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

    it('keeps the same clientRequestId and the same body when Save bill is pressed again after a failed try, so a retry never saves twice', async () => {
      let tries = 0
      const { fetchMock } = await renderReadyBill({
        answers: {
          [`POST ${oneBillPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(createdBill)
          },
        },
      })
      typeAmount('1800')
      await addNames('Marko')

      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')
      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(2)
      })
      const first = sentBody(fetchMock, 'POST', oneBillPath, 0)
      const second = sentBody(fetchMock, 'POST', oneBillPath, 1)
      expect(second.clientRequestId).toBe(first.clientRequestId)
      expect(second).toEqual(first)
    })

    it('keeps the same clientRequestId after a network failure', async () => {
      let tries = 0
      const { fetchMock } = await renderReadyBill({
        answers: {
          [`POST ${oneBillPath}`]: () => {
            tries += 1
            return tries === 1 ? new TypeError('Failed to fetch') : Response.json(createdBill)
          },
        },
      })
      typeAmount('1800')

      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')
      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(2)
      })
      expect(sentBody(fetchMock, 'POST', oneBillPath, 1).clientRequestId).toBe(
        sentBody(fetchMock, 'POST', oneBillPath, 0).clientRequestId,
      )
    })

    it.each([
      [400, 'EXPENSE_TITLE_INVALID'],
      [400, 'EXPENSE_AMOUNT_TOO_LARGE'],
      [400, 'EXPENSE_SPLIT_DOES_NOT_ADD_UP'],
      [400, 'EXPENSE_DATE_INVALID'],
      [400, 'EXPENSE_CLIENT_REQUEST_ID_USED'],
      [404, 'MEMBER_NOT_FOUND'],
    ])('shows the translated text for %i %s inline, stays on the screen and does not say Expense saved', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router } = await renderReadyBill({
        answers: { [`POST ${oneBillPath}`]: problemAnswer(status, code) },
      })
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.newBill)
      expect(shownToastTexts()).not.toContain(translated('en', 'expenses.saved'))
    })

    it.each(languages)('shows the translated text of an error code in the language of the person (%s)', async (language) => {
      await renderReadyBill({
        language,
        answers: { [`POST ${oneBillPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE') },
      })
      typeAmount('1800', language)

      fireEvent.click(saveBillButton(language))

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.EXPENSE_AMOUNT_TOO_LARGE'),
      )
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderReadyBill({ answers: { [`POST ${oneBillPath}`]: networkFailureAnswer() } })
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('shows the too-many-tries text and asks the server only once for a 429, never retrying', async () => {
      const { fetchMock } = await renderReadyBill({
        answers: { [`POST ${oneBillPath}`]: problemAnswer(429, 'RATE_LIMITED') },
      })
      typeAmount('1800')

      fireEvent.click(saveBillButton())

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.RATE_LIMITED'))
      expect(requestCount(fetchMock, 'POST', oneBillPath)).toBe(1)
    })

    it('keeps everything the person typed and chose after a failed save', async () => {
      await renderReadyBill({
        answers: { [`POST ${oneBillPath}`]: problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE') },
      })
      typeAmount('1800')
      typeTitle('Hotel')
      await addNames('Marko', 'Ana')
      const paidBy = await openSheet('paidBy')
      fireEvent.click(personButton(paidBy, 'Ana'))
      await waitForSheetToClose()
      const category = await openSheet('category')
      fireEvent.click(within(category).getByRole('button', { name: /Accommodation/ }))
      await waitForSheetToClose()

      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')

      expect(amountField().value).toBe('1800')
      expect(titleField().value).toBe('Hotel')
      expect(removeButton('Marko')).toBeTruthy()
      expect(removeButton('Ana')).toBeTruthy()
      expect(formRow('paidBy').textContent).toContain('Ana')
      expect(formRow('category').textContent).toContain(categoryName('en', accommodationCategory))
    })

    it('saves after a failed try when the problem is gone, with the values that were typed', async () => {
      let tries = 0
      const { router } = await renderReadyBill({
        answers: {
          [`POST ${oneBillPath}`]: () => {
            tries += 1
            return tries === 1 ? problemAnswer(400, 'EXPENSE_AMOUNT_TOO_LARGE')() : Response.json(createdBill)
          },
        },
      })
      typeAmount('1800')
      fireEvent.click(saveBillButton())
      await screen.findByRole('alert')

      fireEvent.click(saveBillButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
    })
  })

  describe('in Macedonian', () => {
    it('writes the people, the rows, the title field and the Save bill button in Macedonian', async () => {
      await renderReadyBill({ language: 'mk' })

      expect(screen.getByText(translated('mk', 'oneBill.people'))).toBeTruthy()
      expect(screen.getByText(translated('mk', 'expense.me'))).toBeTruthy()
      expect(formRow('paidBy', 'mk')).toBeTruthy()
      expect(formRow('split', 'mk')).toBeTruthy()
      expect(titleField('mk')).toBeTruthy()
      expect(saveBillButton('mk')).toBeTruthy()
    })

    it('writes the remove button of a circle in Macedonian with the name between the quotes', async () => {
      await renderReadyBill({ language: 'mk' })

      await addName('Марко', 'mk')

      expect(removeButton('Марко', 'mk')).toBeTruthy()
    })
  })
})
