import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { problemResponse, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  activePersonalCategories,
  builtInPersonalCategoriesOnly,
  coffeeCategory,
  personalFoodCategory,
} from '@/test/personalCategoryTestData'
import {
  categoryListAnswer,
  colorGroup,
  emojiPickGroup,
  findSheet,
  personalCategoriesGet,
  personalCategoriesWithArchivedGet,
  personalCategoryCreatePost,
  personalCategoryLabel,
  saveButtonIn,
  sheetField,
  waitForAnyTimers,
} from '@/test/personalCategoryTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { expectSaveOutsideScrollingFields, expectTitleOutsideScrollingBody } from '@/test/sheetScrollTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { translated } from '@/test/translated'
import { PersonalCategoryGrid } from './PersonalCategoryGrid'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const createdTea: PersonalCategory = {
  id: 'd0000000-0000-4000-8000-000000000009',
  key: null,
  name: 'Tea',
  emoji: '\u{1F3F7}\u{FE0F}',
  color: 'orange',
  sortOrder: 103,
  isCustom: true,
  archived: false,
}

const defaultEmoji = '\u{1F3F7}\u{FE0F}'
const coffeeEmoji = '\u{2615}'
const unicornEmoji = '\u{1F984}'

const colorNames = ['orange', 'green', 'yellow', 'blue', 'pink', 'purple', 'slate', 'red', 'teal', 'gray']

interface GridOptions {
  language?: Language
  categories?: () => readonly PersonalCategory[]
  answers?: Record<string, AnswerFactory>
}

async function renderGrid(options: GridOptions = {}) {
  const language = options.language ?? 'en'
  const onPick = vi.fn<(category: PersonalCategory) => void>()
  const fetchMock = stubFetchByRequest({
    [personalCategoriesGet]: categoryListAnswer(options.categories ?? (() => activePersonalCategories)),
    [personalCategoryCreatePost]: jsonAnswer(createdTea),
    ...options.answers,
  })
  await renderElementWithProviders(<PersonalCategoryGrid onPick={onPick} />, '/grid', { language })
  return { onPick, fetchMock, language }
}

async function findTile(language: Language, category: PersonalCategory): Promise<HTMLElement> {
  return screen.findByRole('button', { name: personalCategoryLabel(language, category) })
}

function newTile(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'budget.categories.new') })
}

async function openNewSheet(language: Language = 'en'): Promise<HTMLElement> {
  await findTile(language, personalFoodCategory)
  fireEvent.click(newTile(language))
  return findSheet(language, 'budget.categories.newTitle')
}

function typeName(sheet: HTMLElement, name: string, language: Language = 'en'): void {
  fireEvent.change(sheetField(sheet, language, 'budget.categories.name'), { target: { value: name } })
}

function sentCreates(fetchMock: ReturnType<typeof stubFetchByRequest>) {
  return requestsOf(fetchMock, 'POST', '/api/personal/categories')
}

describe('PersonalCategoryGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('the tiles', () => {
    it.each(languages)('shows the built-ins first with their translated names, then the own ones, then "+ New" last (%s)', async (language) => {
      await renderGrid({ language })
      await findTile(language, personalFoodCategory)

      const expectedOrder = [
        ...activePersonalCategories.map((category) => personalCategoryLabel(language, category)),
        translated(language, 'budget.categories.new'),
      ]
      const tiles = screen.getAllByRole('button').slice(0, expectedOrder.length)

      expectedOrder.forEach((name, index) => {
        expect(tiles[index]).toBe(screen.getByRole('button', { name }))
      })
    })

    it('shows each built-in under its translated name, not its key', async () => {
      await renderGrid({ language: 'mk' })

      expect(await screen.findByRole('button', { name: translated('mk', 'categories.food') })).toBeTruthy()
      expect(screen.queryByText('food')).toBeNull()
    })

    it('lays the tiles out in 4 columns with a 44 px tile in each button', async () => {
      await renderGrid()
      const tile = await findTile('en', coffeeCategory)

      expect(Array.from(tile.parentElement?.classList ?? [])).toContain('grid-cols-4')
      expect(tile.querySelector('.size-11')).not.toBeNull()
      expect(newTile().querySelector('.size-11.border-dashed')).not.toBeNull()
    })

    it('paints the own category tile in its colour token', async () => {
      await renderGrid()
      const tile = await findTile('en', coffeeCategory)

      expect(tile.querySelector<HTMLElement>('.size-11')?.style.backgroundColor).toBe('var(--category-teal)')
    })

    it('picks a category with one tap and nothing else', async () => {
      const { onPick, fetchMock } = await renderGrid()

      fireEvent.click(await findTile('en', coffeeCategory))

      expect(onPick).toHaveBeenCalledOnce()
      expect(onPick).toHaveBeenCalledWith(coffeeCategory)
      expect(fetchMock).toHaveBeenCalledOnce()
    })

    it('asks only for the active categories, so archived ones never reach the grid', async () => {
      const { fetchMock } = await renderGrid()
      await findTile('en', coffeeCategory)

      expect(requestCount(fetchMock, 'GET', '/api/personal/categories')).toBe(1)
      expect(requestCount(fetchMock, 'GET', '/api/personal/categories?includeArchived=true')).toBe(0)
      expect(screen.queryByRole('button', { name: 'Gym' })).toBeNull()
    })

    it('shows the "+ New" tile and the "Edit my categories" link even with no own categories', async () => {
      await renderGrid({ categories: () => builtInPersonalCategoriesOnly })
      await findTile('en', personalFoodCategory)

      expect(newTile()).toBeTruthy()
      expect(screen.getByRole('button', { name: translated('en', 'budget.categories.editMine') })).toBeTruthy()
    })

    it('gives the "Edit my categories" link a 44 px tap height', async () => {
      await renderGrid()
      await findTile('en', personalFoodCategory)

      const link = screen.getByRole('button', { name: translated('en', 'budget.categories.editMine') })
      expect(Array.from(link.classList)).toContain('min-h-11')
    })

    it('opens "My categories" from the "Edit my categories" link', async () => {
      await renderGrid({ answers: { [personalCategoriesWithArchivedGet]: categoryListAnswer(() => activePersonalCategories) } })
      await findTile('en', personalFoodCategory)

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'budget.categories.editMine') }))

      expect(await findSheet('en', 'budget.categories.mineTitle')).toBeTruthy()
    })
  })

  describe('while loading and after a failure', () => {
    it('shows skeleton tiles and no spinner while the categories load', async () => {
      stubFetchThatNeverAnswers()
      await renderElementWithProviders(<PersonalCategoryGrid onPick={() => {}} />, '/grid')

      const look = loadingLook()
      expect(look.skeletonCount).toBeGreaterThanOrEqual(8)
      expect(look.hasSpinner).toBe(false)
    })

    it.each(languages)('shows the translated error with a retry that asks again (%s)', async (language) => {
      let failing = true
      const fetchMock = stubFetchByRequest({
        [personalCategoriesGet]: () =>
          failing ? new Response(null, { status: 500 }) : Response.json({ categories: activePersonalCategories }),
      })
      await renderElementWithProviders(<PersonalCategoryGrid onPick={() => {}} />, '/grid', { language })

      const alert = await screen.findByRole('alert')
      expect(alert.textContent).toContain(translated(language, 'errors.generic'))
      failing = false
      fireEvent.click(within(alert).getByRole('button', { name: translated(language, 'common.retry') }))

      expect(await findTile(language, coffeeCategory)).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/categories')).toBe(2)
    })
  })

  describe('the new-category sheet', () => {
    it.each(languages)('opens at once with one tap on "+ New" and puts the cursor in the name (%s)', async (language) => {
      await renderGrid({ language })

      const sheet = await openNewSheet(language)

      const name = sheetField(sheet, language, 'budget.categories.name')
      await waitFor(() => {
        expect(document.activeElement).toBe(name)
      })
      expect(name.value).toBe('')
      expect(name.maxLength).toBe(30)
    })

    it('keeps the title and Save in view on a short window, scrolling only the fields', async () => {
      await renderGrid()

      const sheet = await openNewSheet()

      expectTitleOutsideScrollingBody(sheet, translated('en', 'budget.categories.newTitle'))
      expectSaveOutsideScrollingFields(sheet, sheetField(sheet, 'en', 'budget.categories.name'), saveButtonIn(sheet))
    })

    it('creates a category with 1 field and 2 taps: "+ New", type the name, Save, using the default emoji and colour', async () => {
      const { fetchMock } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toEqual({ name: 'Tea', emoji: defaultEmoji, color: 'orange' })
    })

    it('closes after a successful save and picks the new category straight away', async () => {
      const { onPick } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(onPick).toHaveBeenCalledWith(createdTea)
      })
      expect(onPick).toHaveBeenCalledOnce()
      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.categories.newTitle') })).toBeNull()
      })
    })

    it('asks for the list again after a save, so the new tile appears in the grid', async () => {
      let categories: readonly PersonalCategory[] = activePersonalCategories
      const { fetchMock } = await renderGrid({
        categories: () => categories,
        answers: {
          [personalCategoryCreatePost]: () => {
            categories = [...activePersonalCategories, createdTea]
            return Response.json(createdTea)
          },
        },
      })

      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      fireEvent.click(saveButtonIn(sheet))

      expect(await findTile('en', createdTea)).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', '/api/personal/categories')).toBe(2)
    })

    it('shows the default emoji in the emoji field, limited to 16 characters, and the first colour selected', async () => {
      await renderGrid()

      const sheet = await openNewSheet()

      const emoji = sheetField(sheet, 'en', 'budget.categories.emoji')
      expect(emoji.value).toBe(defaultEmoji)
      expect(emoji.maxLength).toBe(16)
      const pressed = within(colorGroup(sheet, 'en')).getAllByRole('button', { pressed: true })
      expect(pressed.map((button) => button.getAttribute('aria-label'))).toEqual([translated('en', 'budget.categories.colors.orange')])
    })

    it('uses a font of at least 16 px in both fields, so phones do not zoom the page', async () => {
      await renderGrid()

      const sheet = await openNewSheet()

      for (const labelKey of ['budget.categories.name', 'budget.categories.emoji']) {
        expect(Array.from(sheetField(sheet, 'en', labelKey).classList)).toContain('text-[1.0625rem]')
      }
    })

    it.each(languages)('offers the ten app colours, each named in words (%s)', async (language) => {
      await renderGrid({ language })

      const sheet = await openNewSheet(language)

      const swatches = within(colorGroup(sheet, language)).getAllByRole('button')
      expect(swatches.map((swatch) => swatch.getAttribute('aria-label'))).toEqual(
        colorNames.map((color) => translated(language, `budget.categories.colors.${color}`)),
      )
      expect(swatches.map((swatch) => swatch.querySelector<HTMLElement>('[aria-hidden="true"]')?.style.backgroundColor)).toEqual(
        colorNames.map((color) => `var(--category-${color})`),
      )
    })

    it('marks the chosen colour with a tick as well, not with colour alone', async () => {
      await renderGrid()

      const sheet = await openNewSheet()
      const blue = within(colorGroup(sheet, 'en')).getByRole('button', { name: translated('en', 'budget.categories.colors.blue') })
      fireEvent.click(blue)

      expect(blue.getAttribute('aria-pressed')).toBe('true')
      expect(blue.textContent).toBe('✓')
      const others = within(colorGroup(sheet, 'en')).getAllByRole('button').filter((swatch) => swatch !== blue)
      for (const swatch of others) {
        expect(swatch.getAttribute('aria-pressed')).toBe('false')
        expect(swatch.textContent).toBe('')
      }
    })

    it('gives every colour a 44 px tap area', async () => {
      await renderGrid()

      const sheet = await openNewSheet()

      for (const swatch of within(colorGroup(sheet, 'en')).getAllByRole('button')) {
        expect(Array.from(swatch.classList)).toEqual(expect.arrayContaining(['size-11']))
      }
    })

    it('sends the colour that was tapped', async () => {
      const { fetchMock } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      fireEvent.click(within(colorGroup(sheet, 'en')).getByRole('button', { name: translated('en', 'budget.categories.colors.blue') }))
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toMatchObject({ color: 'blue' })
    })

    it('offers 20 quick emoji picks with the default one chosen, each at least 44 px', async () => {
      await renderGrid()

      const sheet = await openNewSheet()

      const picks = within(emojiPickGroup(sheet, 'en')).getAllByRole('button')
      expect(picks).toHaveLength(20)
      expect(new Set(picks.map((pick) => pick.textContent)).size).toBe(20)
      expect(picks[0].textContent).toBe(defaultEmoji)
      expect(picks.filter((pick) => pick.getAttribute('aria-pressed') === 'true')).toEqual([picks[0]])
      for (const pick of picks) {
        expect(Array.from(pick.classList)).toEqual(expect.arrayContaining(['min-h-11', 'min-w-12']))
      }
    })

    it('hides the scrollbar of the quick picks and scrolls them sideways with an up-and-down mouse wheel', async () => {
      await renderGrid()
      const sheet = await openNewSheet()
      const row = within(emojiPickGroup(sheet, 'en')).getAllByRole('button')[0].parentElement
      if (row === null) {
        throw new Error('The quick picks have no row around them')
      }
      Object.defineProperty(row, 'scrollWidth', { configurable: true, value: 1200 })
      Object.defineProperty(row, 'clientWidth', { configurable: true, value: 320 })
      Object.defineProperty(row, 'scrollLeft', { configurable: true, writable: true, value: 0 })

      fireEvent.wheel(row, { deltaY: 100 })

      expect(row.className.split(/\s+/)).toContain('sideways-scroll')
      expect(row.scrollLeft).toBe(100)
    })

    it('puts a tapped quick pick into the emoji field and sends it', async () => {
      const { fetchMock } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Coffee')
      fireEvent.click(within(emojiPickGroup(sheet, 'en')).getByRole('button', { name: coffeeEmoji }))

      expect(sheetField(sheet, 'en', 'budget.categories.emoji').value).toBe(coffeeEmoji)
      fireEvent.click(saveButtonIn(sheet))
      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toMatchObject({ emoji: coffeeEmoji })
    })

    it('sends a typed emoji without the spaces around it', async () => {
      const { fetchMock } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Magic')
      fireEvent.change(sheetField(sheet, 'en', 'budget.categories.emoji'), { target: { value: `  ${unicornEmoji} ` } })
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toMatchObject({ emoji: unicornEmoji })
    })

    it('sends an emoji typed into the field', async () => {
      const { fetchMock } = await renderGrid()

      const sheet = await openNewSheet()
      typeName(sheet, 'Magic')
      fireEvent.change(sheetField(sheet, 'en', 'budget.categories.emoji'), { target: { value: unicornEmoji } })
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toMatchObject({ emoji: unicornEmoji })
    })

    it('sends the request even with an empty name, because the server does the checking', async () => {
      const { fetchMock } = await renderGrid({
        answers: { [personalCategoryCreatePost]: problemAnswer(400, 'CATEGORY_NAME_INVALID') },
      })

      const sheet = await openNewSheet()
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(sentCreates(fetchMock)).toHaveLength(1)
      })
      expect(sentCreates(fetchMock)[0].body).toMatchObject({ name: '' })
    })
  })

  describe('a refused new category', () => {
    const refusals = [
      ['CATEGORY_NAME_INVALID', 'name'],
      ['CATEGORY_NAME_TAKEN', 'name'],
      ['CATEGORY_EMOJI_INVALID', 'emoji'],
      ['CATEGORY_COLOR_INVALID', 'color'],
    ] as const

    const refusalsInEachLanguage = languages.flatMap((language) =>
      refusals.map(([code]) => [language, code] as const),
    )

    function invalidMarks(sheet: HTMLElement, language: Language) {
      return {
        name: sheetField(sheet, language, 'budget.categories.name').getAttribute('aria-invalid'),
        emoji: sheetField(sheet, language, 'budget.categories.emoji').getAttribute('aria-invalid'),
        color: colorGroup(sheet, language).getAttribute('aria-invalid'),
      }
    }

    it.each(refusalsInEachLanguage)('shows the %s text for %s above Save (%s)', async (language, code) => {
      await renderGrid({ language, answers: { [personalCategoryCreatePost]: problemAnswer(400, code) } })

      const sheet = await openNewSheet(language)
      typeName(sheet, 'Tea', language)
      fireEvent.click(saveButtonIn(sheet, language))

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toBe(translated(language, `errors.${code}`))
    })

    it.each(refusals)('marks the right field red for %s and keeps what was typed', async (code, field) => {
      await renderGrid({ answers: { [personalCategoryCreatePost]: problemAnswer(400, code) } })

      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      fireEvent.click(within(colorGroup(sheet, 'en')).getByRole('button', { name: translated('en', 'budget.categories.colors.blue') }))
      fireEvent.click(saveButtonIn(sheet))
      await within(sheet).findByRole('alert')

      const marks = invalidMarks(sheet, 'en')
      expect(marks).toEqual({
        name: String(field === 'name'),
        emoji: String(field === 'emoji'),
        color: field === 'color' ? 'true' : 'false',
      })
      expect(sheetField(sheet, 'en', 'budget.categories.name').value).toBe('Tea')
      expect(within(colorGroup(sheet, 'en')).getByRole('button', { name: translated('en', 'budget.categories.colors.blue') }).getAttribute('aria-pressed')).toBe('true')
    })

    it('draws the red edge of the colours with the same class as the fields', async () => {
      await renderGrid({ answers: { [personalCategoryCreatePost]: problemAnswer(400, 'CATEGORY_COLOR_INVALID') } })

      const sheet = await openNewSheet()
      fireEvent.click(saveButtonIn(sheet))
      await within(sheet).findByRole('alert')

      expect(Array.from(colorGroup(sheet, 'en').classList)).toContain('aria-invalid:border-destructive')
      expect(Array.from(sheetField(sheet, 'en', 'budget.categories.name').classList)).toContain('aria-invalid:border-destructive')
    })

    it('takes the red edge off the name once it is changed', async () => {
      await renderGrid({ answers: { [personalCategoryCreatePost]: problemAnswer(400, 'CATEGORY_NAME_TAKEN') } })

      const sheet = await openNewSheet()
      typeName(sheet, 'Coffee')
      fireEvent.click(saveButtonIn(sheet))
      await within(sheet).findByRole('alert')
      typeName(sheet, 'Coffee 2')

      expect(sheetField(sheet, 'en', 'budget.categories.name').getAttribute('aria-invalid')).toBe('false')
    })

    it.each(languages)('shows the network text when the server cannot be reached, with no field marked (%s)', async (language) => {
      await renderGrid({ language, answers: { [personalCategoryCreatePost]: networkFailureAnswer() } })

      const sheet = await openNewSheet(language)
      typeName(sheet, 'Tea', language)
      fireEvent.click(saveButtonIn(sheet, language))

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toBe(translated(language, 'errors.network'))
      expect(invalidMarks(sheet, language)).toEqual({ name: 'false', emoji: 'false', color: 'false' })
    })

    it('stays open and does not pick anything after a refusal', async () => {
      const { onPick } = await renderGrid({ answers: { [personalCategoryCreatePost]: problemAnswer(400, 'CATEGORY_NAME_TAKEN') } })

      const sheet = await openNewSheet()
      typeName(sheet, 'Coffee')
      fireEvent.click(saveButtonIn(sheet))
      await within(sheet).findByRole('alert')

      expect(onPick).not.toHaveBeenCalled()
      expect(screen.getByRole('dialog', { name: translated('en', 'budget.categories.newTitle') })).toBe(sheet)
    })

    it('lets the person try again after a refusal', async () => {
      let refuse = true
      const { onPick, fetchMock } = await renderGrid({
        answers: {
          [personalCategoryCreatePost]: () => (refuse ? problemResponse(400, 'CATEGORY_NAME_TAKEN') : Response.json(createdTea)),
        },
      })

      const sheet = await openNewSheet()
      typeName(sheet, 'Coffee')
      fireEvent.click(saveButtonIn(sheet))
      await within(sheet).findByRole('alert')
      refuse = false
      typeName(sheet, 'Tea')
      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(onPick).toHaveBeenCalledWith(createdTea)
      })
      expect(sentCreates(fetchMock)).toHaveLength(2)
    })
  })

  describe('a double tap on Save', () => {
    it('sends one request when Save is tapped twice at once', async () => {
      await renderGrid()
      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      stubFetchThatNeverAnswers()

      fireEvent.click(saveButtonIn(sheet))
      fireEvent.click(saveButtonIn(sheet))

      await waitForAnyTimers()
      expect(vi.mocked(fetch)).toHaveBeenCalledOnce()
    })

    it('disables Save while the request waits for an answer', async () => {
      await renderGrid()
      const sheet = await openNewSheet()
      typeName(sheet, 'Tea')
      stubFetchThatNeverAnswers()

      fireEvent.click(saveButtonIn(sheet))

      await waitFor(() => {
        expect(saveButtonIn(sheet).matches(':disabled')).toBe(true)
      })
    })
  })
})
