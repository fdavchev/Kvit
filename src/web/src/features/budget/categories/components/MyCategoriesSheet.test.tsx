import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import {
  activePersonalCategories,
  archivedGymCategory,
  builtInPersonalCategoriesOnly,
  coffeeCategory,
  personalCategoriesWithArchived,
  personalFoodCategory,
  petCategory,
} from '@/test/personalCategoryTestData'
import {
  categoryListAnswer,
  colorGroup,
  findSheet,
  personalCategoriesGet,
  personalCategoriesWithArchivedGet,
  personalCategoryArchivePost,
  personalCategoryLabel,
  personalCategoryUnarchivePost,
  personalCategoryUpdatePut,
  saveButtonIn,
  sheetField,
  waitForAnyTimers,
} from '@/test/personalCategoryTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import {
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { PersonalCategoryGrid } from './PersonalCategoryGrid'
import { MyCategoriesSheet } from './MyCategoriesSheet'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

interface SheetOptions {
  language?: Language
  categories?: () => readonly PersonalCategory[]
  answers?: Record<string, AnswerFactory>
}

async function renderMine(options: SheetOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [personalCategoriesWithArchivedGet]: categoryListAnswer(options.categories ?? (() => personalCategoriesWithArchived)),
    [personalCategoryUpdatePut(coffeeCategory.id)]: noContentAnswer(),
    [personalCategoryArchivePost(coffeeCategory.id)]: noContentAnswer(),
    [personalCategoryUnarchivePost(coffeeCategory.id)]: noContentAnswer(),
    [personalCategoryUnarchivePost(archivedGymCategory.id)]: noContentAnswer(),
    ...options.answers,
  })
  await renderElementWithProviders(<MyCategoriesSheet onClose={() => {}} />, '/mine', { language })
  const sheet = await findSheet(language, 'budget.categories.mineTitle')
  return { fetchMock, sheet }
}

async function findRow(sheet: HTMLElement, name: string): Promise<HTMLElement> {
  const label = await within(sheet).findByText(name)
  const row = label.closest('li')
  if (row === null) {
    throw new Error(`The category "${name}" is not inside a list row`)
  }
  return row
}

function buttonIn(row: HTMLElement, language: Language, key: string): HTMLElement {
  return within(row).getByRole('button', { name: translated(language, key) })
}

function errorToastTexts(): unknown[] {
  return vi.mocked(toast.error).mock.calls.map(([text]) => text)
}

describe('MyCategoriesSheet', () => {
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  describe('the list', () => {
    it.each(languages)('lists only the own active categories, each with Edit and Archive (%s)', async (language) => {
      const { sheet } = await renderMine({ language })

      for (const category of [coffeeCategory, petCategory]) {
        const row = await findRow(sheet, category.name)
        expect(buttonIn(row, language, 'expense.edit')).toBeTruthy()
        expect(buttonIn(row, language, 'budget.categories.archive')).toBeTruthy()
      }
      expect(within(sheet).queryByText(personalCategoryLabel(language, personalFoodCategory))).toBeNull()
    })

    it('asks for the categories with the archived ones', async () => {
      const { fetchMock, sheet } = await renderMine()
      await findRow(sheet, coffeeCategory.name)

      expect(requestCount(fetchMock, 'GET', '/api/personal/categories?includeArchived=true')).toBe(1)
    })

    it.each(languages)('lists the archived ones under "Archived (N)", each with "Bring back" (%s)', async (language) => {
      const { sheet } = await renderMine({ language })

      const heading = await within(sheet).findByRole('heading', {
        name: translated(language, 'budget.categories.archivedHeading', { count: '1' }),
      })
      const row = await findRow(sheet, archivedGymCategory.name)
      expect(buttonIn(row, language, 'budget.categories.bringBack')).toBeTruthy()
      expect(within(row).queryByRole('button', { name: translated(language, 'budget.categories.archive') })).toBeNull()
      expect(heading.compareDocumentPosition(row) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    })

    it('shows no Archived section when nothing is archived', async () => {
      const { sheet } = await renderMine({ categories: () => activePersonalCategories })
      await findRow(sheet, coffeeCategory.name)

      expect(within(sheet).queryByRole('heading', { name: /Archived/ })).toBeNull()
    })

    it.each(languages)('points to "+ New" when there are no own categories (%s)', async (language) => {
      const { sheet } = await renderMine({ language, categories: () => builtInPersonalCategoriesOnly })

      expect(await within(sheet).findByText(translated(language, 'budget.categories.emptyMine'))).toBeTruthy()
      expect(within(sheet).queryByRole('listitem')).toBeNull()
    })

    it('shows the empty text and the Archived section when every own category is archived', async () => {
      const { sheet } = await renderMine({ categories: () => [...builtInPersonalCategoriesOnly, archivedGymCategory] })

      expect(await within(sheet).findByText(translated('en', 'budget.categories.emptyMine'))).toBeTruthy()
      expect(await findRow(sheet, archivedGymCategory.name)).toBeTruthy()
    })

    it('shows skeleton rows and no spinner while loading', async () => {
      stubFetchThatNeverAnswers()
      await renderElementWithProviders(<MyCategoriesSheet onClose={() => {}} />, '/mine')
      const sheet = await findSheet('en', 'budget.categories.mineTitle')

      expect(within(sheet).getByRole('status')).toBeTruthy()
      const look = loadingLook()
      expect(look.skeletonCount).toBeGreaterThanOrEqual(3)
      expect(look.hasSpinner).toBe(false)
    })

    it('shows the translated error when the list cannot load', async () => {
      const { sheet } = await renderMine({ answers: { [personalCategoriesWithArchivedGet]: problemAnswer(403, 'GUEST_NOT_ALLOWED') } })

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toContain(translated('en', 'errors.GUEST_NOT_ALLOWED'))
    })

    it('gives Edit, Archive and Bring back a 44 px tap area', async () => {
      const { sheet } = await renderMine()

      const row = await findRow(sheet, coffeeCategory.name)
      const archivedRow = await findRow(sheet, archivedGymCategory.name)
      for (const button of [buttonIn(row, 'en', 'expense.edit'), buttonIn(row, 'en', 'budget.categories.archive'), buttonIn(archivedRow, 'en', 'budget.categories.bringBack')]) {
        expect(Array.from(button.classList)).toEqual(expect.arrayContaining(['min-h-11', 'min-w-11']))
      }
    })
  })

  describe('editing', () => {
    it.each(languages)('opens the same sheet filled in with the name, the emoji and the colour (%s)', async (language) => {
      const { sheet } = await renderMine({ language })

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), language, 'expense.edit'))

      const editSheet = await findSheet(language, 'budget.categories.editTitle')
      expect(sheetField(editSheet, language, 'budget.categories.name').value).toBe('Coffee')
      expect(sheetField(editSheet, language, 'budget.categories.emoji').value).toBe(coffeeCategory.emoji)
      const pressed = within(colorGroup(editSheet, language)).getAllByRole('button', { pressed: true })
      expect(pressed.map((button) => button.getAttribute('aria-label'))).toEqual([translated(language, 'budget.categories.colors.teal')])
    })

    it('sends PUT with the changed name and the kept emoji and colour, then closes', async () => {
      const { fetchMock, sheet } = await renderMine()

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'expense.edit'))
      const editSheet = await findSheet('en', 'budget.categories.editTitle')
      fireEvent.change(sheetField(editSheet, 'en', 'budget.categories.name'), { target: { value: 'Espresso' } })
      fireEvent.click(saveButtonIn(editSheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog', { name: translated('en', 'budget.categories.editTitle') })).toBeNull()
      })
      const sent = requestsOf(fetchMock, 'PUT', `/api/personal/categories/${coffeeCategory.id}`)
      expect(sent).toHaveLength(1)
      expect(sent[0].body).toEqual({ name: 'Espresso', emoji: coffeeCategory.emoji, color: 'teal' })
      expect(requestCount(fetchMock, 'GET', '/api/personal/categories?includeArchived=true')).toBe(2)
    })

    it.each([
      ['CATEGORY_NAME_TAKEN', 400, 'name'],
      ['CATEGORY_EMOJI_INVALID', 400, 'emoji'],
      ['CATEGORY_NOT_FOUND', 404, 'none'],
    ] as const)('shows the %s text and marks the right field red', async (code, status, field) => {
      const { sheet } = await renderMine({ answers: { [personalCategoryUpdatePut(coffeeCategory.id)]: problemAnswer(status, code) } })

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'expense.edit'))
      const editSheet = await findSheet('en', 'budget.categories.editTitle')
      fireEvent.click(saveButtonIn(editSheet))

      const alert = await within(editSheet).findByRole('alert')
      expect(alert.textContent).toBe(translated('en', `errors.${code}`))
      expect(sheetField(editSheet, 'en', 'budget.categories.name').getAttribute('aria-invalid')).toBe(String(field === 'name'))
      expect(sheetField(editSheet, 'en', 'budget.categories.emoji').getAttribute('aria-invalid')).toBe(String(field === 'emoji'))
    })

    it('sends one PUT when Save is tapped twice at once', async () => {
      const { sheet } = await renderMine()
      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'expense.edit'))
      const editSheet = await findSheet('en', 'budget.categories.editTitle')
      stubFetchThatNeverAnswers()

      fireEvent.click(saveButtonIn(editSheet))
      fireEvent.click(saveButtonIn(editSheet))

      await waitForAnyTimers()
      expect(vi.mocked(fetch)).toHaveBeenCalledOnce()
    })
  })

  describe('archiving', () => {
    it('archives with one tap, no question, and shows "Archived" with Undo', async () => {
      const { fetchMock, sheet } = await renderMine()

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'budget.categories.archive'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', `/api/personal/categories/${coffeeCategory.id}/archive`)).toBe(1)
      })
      const shown = await waitFor(() => toastShownWithText(translated('en', 'budget.categories.archivedToast', { name: 'Coffee' })))
      expect(shown.options.action).toMatchObject({ label: translated('en', 'common.undo') })
      expect(screen.queryByRole('alertdialog')).toBeNull()
    })

    it('brings the category back when Undo is pressed', async () => {
      const { fetchMock, sheet } = await renderMine()

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'budget.categories.archive'))
      const shown = await waitFor(() => toastShownWithText(translated('en', 'budget.categories.archivedToast', { name: 'Coffee' })))
      pressToastAction(shown)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', `/api/personal/categories/${coffeeCategory.id}/unarchive`)).toBe(1)
      })
    })

    it('writes the Archived toast in Macedonian', async () => {
      const { sheet } = await renderMine({ language: 'mk' })

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'mk', 'budget.categories.archive'))

      const shown = await waitFor(() => toastShownWithText(translated('mk', 'budget.categories.archivedToast', { name: 'Coffee' })))
      expect(shown.options.action).toMatchObject({ label: translated('mk', 'common.undo') })
    })

    it('shows a failed archive as an error toast', async () => {
      const { sheet } = await renderMine({ answers: { [personalCategoryArchivePost(coffeeCategory.id)]: problemAnswer(404, 'CATEGORY_NOT_FOUND') } })

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'budget.categories.archive'))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated('en', 'errors.CATEGORY_NOT_FOUND')])
      })
    })

    it('takes an archived category out of the grid and puts it under Archived', async () => {
      let archived = false
      const currentWithArchived = (): PersonalCategory[] =>
        personalCategoriesWithArchived.map((category) => (category.id === coffeeCategory.id ? { ...category, archived } : category))
      stubFetchByRequest({
        [personalCategoriesGet]: categoryListAnswer(() => currentWithArchived().filter((category) => !category.archived)),
        [personalCategoriesWithArchivedGet]: categoryListAnswer(currentWithArchived),
        [personalCategoryArchivePost(coffeeCategory.id)]: () => {
          archived = true
          return new Response(null, { status: 204 })
        },
      })
      await renderElementWithProviders(<PersonalCategoryGrid onPick={() => {}} />, '/grid')
      await screen.findByRole('button', { name: 'Coffee' })
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'budget.categories.editMine') }))
      const sheet = await findSheet('en', 'budget.categories.mineTitle')

      fireEvent.click(buttonIn(await findRow(sheet, coffeeCategory.name), 'en', 'budget.categories.archive'))

      await within(sheet).findByRole('heading', { name: translated('en', 'budget.categories.archivedHeading', { count: '2' }) })
      expect(within(await findRow(sheet, coffeeCategory.name)).getByRole('button', { name: translated('en', 'budget.categories.bringBack') })).toBeTruthy()
      await waitFor(() => {
        expect(screen.queryByRole('button', { name: 'Coffee' })).toBeNull()
      })
    })
  })

  describe('bringing back', () => {
    it('brings an archived category back with one tap', async () => {
      const { fetchMock, sheet } = await renderMine()

      fireEvent.click(buttonIn(await findRow(sheet, archivedGymCategory.name), 'en', 'budget.categories.bringBack'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', `/api/personal/categories/${archivedGymCategory.id}/unarchive`)).toBe(1)
      })
      expect(requestCount(fetchMock, 'GET', '/api/personal/categories?includeArchived=true')).toBe(2)
    })

    it.each(languages)('shows the name-taken text as an error toast when an active category has the name now (%s)', async (language) => {
      const { sheet } = await renderMine({
        language,
        answers: { [personalCategoryUnarchivePost(archivedGymCategory.id)]: problemAnswer(400, 'CATEGORY_NAME_TAKEN') },
      })

      fireEvent.click(buttonIn(await findRow(sheet, archivedGymCategory.name), language, 'budget.categories.bringBack'))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated(language, 'errors.CATEGORY_NAME_TAKEN')])
      })
    })
  })
})
