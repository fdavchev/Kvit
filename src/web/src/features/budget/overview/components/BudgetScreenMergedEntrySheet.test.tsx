import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { personalEntriesQueryKey } from '@/features/budget/entries/personalEntryCache'
import { personalSummaryQueryKey } from '@/features/budget/summary/personalSummaryCache'
import { languages, type Language } from '@/core/i18n/language'
import {
  duplicateAnswersUrl,
  mergedDinnerOutEntry,
  octoberWithOnlyTheDinnerAnsweredNew,
  octoberWithOnlyTheDinnerMerged,
} from '@/test/budgetDuplicateTestData'
import {
  countedAgainText,
  duplicateAnswersDelete,
  entryRowButton,
  entryRowContaining,
  errorToastTexts,
  mergedLabel,
  monthThatChangesWhenAnswered,
  notTheSameButton,
  queryNotTheSameButton,
  shownToastMessages,
  type AnswerThatChangesTheMonth,
} from '@/test/budgetDuplicateTestHelpers'
import { readableText } from '@/test/budgetLeftCardTestHelpers'
import { hasTapHeightOfAtLeast44 } from '@/test/budgetLimitTestHelpers'
import { budgetNow, findEntryList, personalEntriesGet, renderBudget } from '@/test/budgetTestHelpers'
import { isDisabled, saveButton, sentBody, typeAmount } from '@/test/expenseFormTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { dinnerShare } from '@/test/groupShareTestData'
import { coffeeCategory, personalFoodCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, entryAnswerRow, octoberMonth } from '@/test/personalEntryTestData'
import { findDetailsSheet, waitForEveryDialogToClose } from '@/test/personalEntryTestHelpers'
import {
  jsonAnswer,
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { toastsShownWithOptions } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const monthUrl = '/api/personal/entries?month=2026-10'
const dinnerUrl = `/api/personal/entries/${mergedDinnerOutEntry.id}`
const coffeeUrl = `/api/personal/entries/${coffeeEntry.id}`

interface MergedSheetOptions {
  language?: Language
  answer?: (month: AnswerThatChangesTheMonth) => AnswerFactory
  seedCache?: (queryClient: QueryClient) => void
}

async function openMergedDinnerSheet(options: MergedSheetOptions = {}) {
  const language = options.language ?? 'en'
  const month = monthThatChangesWhenAnswered(octoberWithOnlyTheDinnerMerged, octoberWithOnlyTheDinnerAnsweredNew)
  const rendered = await renderBudget({
    language,
    months: [octoberWithOnlyTheDinnerMerged],
    seedCache: options.seedCache,
    answers: {
      [personalEntriesGet('2026-10')]: month.monthAnswer,
      [duplicateAnswersDelete]: options.answer === undefined ? month.acceptAnswer : options.answer(month),
      [`PUT ${dinnerUrl}`]: jsonAnswer(entryAnswerRow(mergedDinnerOutEntry)),
    },
  })
  const list = await findEntryList(language)
  const rowButton = entryRowButton(entryRowContaining(list, 'Dinner out'))
  rowButton.focus()
  fireEvent.click(rowButton)
  const sheet = await findDetailsSheet(personalFoodCategory, language)
  return { ...rendered, month, sheet }
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

function duplicateAnswerRequestCount(fetchMock: Parameters<typeof requestCount>[0]): number {
  return requestCount(fetchMock, 'PUT', duplicateAnswersUrl) + requestCount(fetchMock, 'DELETE', duplicateAnswersUrl)
}

describe('The edit sheet of a merged entry', () => {
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

  describe('the "Not the same" button', () => {
    it.each(languages)('shows a button "Not the same" under Save (%s)', async (language) => {
      const { sheet } = await openMergedDinnerSheet({ language })

      const button = notTheSameButton(sheet, language)

      expect(button.textContent).toBe(translated(language, 'budget.duplicates.notTheSame'))
      expect(isBefore(saveButton(language), button)).toBe(true)
    })

    it('is at least 44 px tall', async () => {
      const { sheet } = await openMergedDinnerSheet()

      expect(hasTapHeightOfAtLeast44(notTheSameButton(sheet))).toBe(true)
    })

    it('is not on the sheet of an entry that is not merged', async () => {
      await renderBudget({ months: [octoberMonth] })
      const list = await findEntryList()

      fireEvent.click(entryRowButton(entryRowContaining(list, 'Coffee')))
      const sheet = await findDetailsSheet(coffeeCategory)

      expect(queryNotTheSameButton(sheet)).toBeNull()
    })

    it('is not on the sheet of an entry whose merge was undone, once the month is read again', async () => {
      const { sheet } = await openMergedDinnerSheet()
      fireEvent.click(notTheSameButton(sheet))
      await waitForEveryDialogToClose()

      fireEvent.click(entryRowButton(entryRowContaining(await findEntryList(), 'Dinner out')))
      const reopened = await findDetailsSheet(personalFoodCategory)

      expect(queryNotTheSameButton(reopened)).toBeNull()
    })
  })

  describe('tapping "Not the same"', () => {
    it('sends exactly one DELETE /api/personal/duplicate-answers with the entry and the expense it is merged with', async () => {
      const { fetchMock, sheet } = await openMergedDinnerSheet()

      fireEvent.click(notTheSameButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', duplicateAnswersUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'DELETE', duplicateAnswersUrl)).toEqual({
        entryId: mergedDinnerOutEntry.id,
        expenseId: dinnerShare.expenseId,
      })
      expect(requestCount(fetchMock, 'PUT', duplicateAnswersUrl)).toBe(0)
    })

    it('closes the sheet and shows the toast "Counted again in your budget" without an Undo', async () => {
      const { sheet } = await openMergedDinnerSheet()

      fireEvent.click(notTheSameButton(sheet))

      await waitForEveryDialogToClose()
      await waitFor(() => {
        expect(shownToastMessages()).toContain(countedAgainText())
      })
      expect(toastsShownWithOptions().filter((shown) => shown.options.action !== undefined)).toEqual([])
    })

    it.each(languages)('shows the toast in the language of the app (%s)', async (language) => {
      const { sheet } = await openMergedDinnerSheet({ language })

      fireEvent.click(notTheSameButton(sheet, language))

      await waitFor(() => {
        expect(shownToastMessages()).toEqual([translated(language, 'budget.duplicates.countedAgain')])
      })
    })

    it('reads the month again and takes the "Merged with" label off the row', async () => {
      const { fetchMock, sheet } = await openMergedDinnerSheet()

      fireEvent.click(notTheSameButton(sheet))

      await waitFor(() => {
        const list = screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })
        expect(readableText(entryRowContaining(list, 'Dinner out'))).not.toContain(mergedLabel('en', 'Dinner', 'Flat'))
      })
      expect(requestCount(fetchMock, 'GET', monthUrl)).toBe(2)
    })

    it('marks every cached month of entries and every cached summary out of date', async () => {
      const { queryClient, sheet } = await openMergedDinnerSheet({
        seedCache: (client) => {
          client.setQueryData(personalEntriesQueryKey('2026-09'), 'a month of entries')
          client.setQueryData(personalSummaryQueryKey('2026-09'), 'a summary')
        },
      })

      fireEvent.click(notTheSameButton(sheet))
      await waitForEveryDialogToClose()

      expect(queryClient.getQueryState(personalEntriesQueryKey('2026-09'))?.isInvalidated).toBe(true)
      expect(queryClient.getQueryState(personalSummaryQueryKey('2026-09'))?.isInvalidated).toBe(true)
    })

    it('sends one request when it is tapped twice at once', async () => {
      const { fetchMock, sheet } = await openMergedDinnerSheet({ answer: () => neverAnswer() })

      fireEvent.click(notTheSameButton(sheet))
      fireEvent.click(notTheSameButton(sheet))

      await settle()
      expect(requestsOf(fetchMock, 'DELETE', duplicateAnswersUrl)).toHaveLength(1)
    })

    it('disables Save and "Not the same" while the request is on its way', async () => {
      const { sheet } = await openMergedDinnerSheet({ answer: () => neverAnswer() })

      fireEvent.click(notTheSameButton(sheet))

      await waitFor(() => {
        expect(isDisabled(notTheSameButton(sheet))).toBe(true)
      })
      expect(isDisabled(saveButton())).toBe(true)
    })
  })

  describe('when the server refuses', () => {
    const refusals: [string, AnswerFactory, string][] = [
      ['GUEST_NOT_ALLOWED', problemAnswer(403, 'GUEST_NOT_ALLOWED'), 'errors.GUEST_NOT_ALLOWED'],
      ['a network failure', networkFailureAnswer(), 'errors.network'],
    ]

    it.each(languages.flatMap((language) => refusals.map(([name, answer, key]) => [language, name, answer, key] as const)))(
      'shows the translated message as an error toast and keeps the sheet (%s, %s)',
      async (language, _name, answer, key) => {
        const { fetchMock, sheet } = await openMergedDinnerSheet({ language, answer: () => answer })

        fireEvent.click(notTheSameButton(sheet, language))

        await waitFor(() => {
          expect(errorToastTexts()).toEqual([translated(language, key)])
        })
        expect(screen.getByRole('dialog')).toBe(sheet)
        expect(shownToastMessages()).toEqual([])
        expect(requestCount(fetchMock, 'GET', monthUrl)).toBe(1)
      },
    )

    it.each(refusals)('lets "Not the same" be tapped again after %s', async (_name, answer) => {
      const { fetchMock, sheet } = await openMergedDinnerSheet({ answer: () => answer })
      fireEvent.click(notTheSameButton(sheet))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })

      expect(isDisabled(notTheSameButton(sheet))).toBe(false)
      fireEvent.click(notTheSameButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', duplicateAnswersUrl)).toHaveLength(2)
      })
    })
  })

  describe('saving an edit of a merged entry', () => {
    it('still sends the usual PUT of the entry and no request to the duplicate answers', async () => {
      const { fetchMock } = await openMergedDinnerSheet()

      typeAmount('900')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestsOf(fetchMock, 'PUT', dinnerUrl)).toHaveLength(1)
      expect(sentBody(fetchMock, 'PUT', dinnerUrl)).toEqual({
        kind: 'Spending',
        categoryId: personalFoodCategory.id,
        amountMinor: 90000,
        currency: 'MKD',
        entryDate: '2026-10-17',
        note: 'Dinner out',
      })
      expect(duplicateAnswerRequestCount(fetchMock)).toBe(0)
    })

    it('shows "Saved" and does not show "Counted again in your budget"', async () => {
      await openMergedDinnerSheet()

      typeAmount('900')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(shownToastMessages()).toContain(translated('en', 'groupSettings.saved'))
      expect(shownToastMessages()).not.toContain(countedAgainText())
    })
  })

  describe('saving an edit of an entry that is not merged', () => {
    it('sends the usual PUT and no request to the duplicate answers', async () => {
      const { fetchMock } = await renderBudget({
        months: [octoberMonth],
        answers: { [`PUT ${coffeeUrl}`]: jsonAnswer(entryAnswerRow(coffeeEntry)) },
      })
      const list = await findEntryList()
      fireEvent.click(entryRowButton(entryRowContaining(list, 'Coffee')))
      await findDetailsSheet(coffeeCategory)

      typeAmount('150')
      fireEvent.click(saveButton())

      await waitForEveryDialogToClose()
      expect(requestsOf(fetchMock, 'PUT', coffeeUrl)).toHaveLength(1)
      expect(duplicateAnswerRequestCount(fetchMock)).toBe(0)
    })
  })
})
