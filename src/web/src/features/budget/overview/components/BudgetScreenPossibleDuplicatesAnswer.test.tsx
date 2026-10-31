import type { QueryClient } from '@tanstack/react-query'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import {
  dinnerPair,
  duplicateAnswersUrl,
  mergedDinnerOutEntry,
  octoberWithOnlyTheDinnerAnsweredNew,
  octoberWithOnlyTheDinnerMerged,
  octoberWithOnlyTheDinnerPair,
  octoberWithTwoPairs,
  octoberWithTwoPairsAfterDinnerIsNew,
  octoberWithTwoPairsAfterDinnerMerged,
} from '@/test/budgetDuplicateTestData'
import {
  cardText,
  dinnerSentence,
  duplicateAnswersPut,
  entryRowContaining,
  errorToastTexts,
  findDuplicateCard,
  mergedLabel,
  monthThatChangesWhenAnswered,
  newButton,
  queryDuplicateCard,
  sameButton,
  taxiSentence,
  type AnswerThatChangesTheMonth,
} from '@/test/budgetDuplicateTestHelpers'
import { readableText } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow, personalEntriesGet, renderBudget } from '@/test/budgetTestHelpers'
import { isDisabled, sentBody } from '@/test/expenseFormTestHelpers'
import { dinnerShare } from '@/test/groupShareTestData'
import { personalEntriesQueryKey } from '@/features/budget/entries/personalEntryCache'
import { personalSummaryQueryKey } from '@/features/budget/summary/personalSummaryCache'
import { problemResponse } from '@/test/apiTestHelpers'
import {
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

interface TwoPairsOptions {
  language?: Language
  after?: Parameters<typeof monthThatChangesWhenAnswered>[1]
  answer?: (month: AnswerThatChangesTheMonth) => AnswerFactory
  seedCache?: (queryClient: QueryClient) => void
}

async function renderTwoPairs(options: TwoPairsOptions = {}) {
  const language = options.language ?? 'en'
  const month = monthThatChangesWhenAnswered(octoberWithTwoPairs, options.after ?? octoberWithTwoPairsAfterDinnerMerged)
  const rendered = await renderBudget({
    language,
    months: [octoberWithTwoPairs],
    seedCache: options.seedCache,
    answers: {
      [personalEntriesGet('2026-10')]: month.monthAnswer,
      [duplicateAnswersPut]: options.answer === undefined ? month.acceptAnswer : options.answer(month),
    },
  })
  const card = await findDuplicateCard(language)
  return { ...rendered, month, card }
}

async function renderLastPair() {
  const month = monthThatChangesWhenAnswered(octoberWithOnlyTheDinnerPair, octoberWithOnlyTheDinnerMerged)
  const rendered = await renderBudget({
    months: [octoberWithOnlyTheDinnerPair],
    answers: {
      [personalEntriesGet('2026-10')]: month.monthAnswer,
      [duplicateAnswersPut]: month.acceptAnswer,
    },
  })
  const card = await findDuplicateCard()
  return { ...rendered, month, card }
}

async function waitForCardToSay(sentence: string): Promise<void> {
  await waitFor(() => {
    expect(cardText(queryDuplicateCard() ?? document.body)).toContain(sentence)
  })
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('BudgetScreen, answering the "Is this the same?" card', () => {
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

  describe('the request', () => {
    it('sends PUT /api/personal/duplicate-answers with the entry, the expense and "Same" for "Same, merge"', async () => {
      const { fetchMock, card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', duplicateAnswersUrl)).toEqual({
        entryId: dinnerPair.entryId,
        expenseId: dinnerPair.expenseId,
        answer: 'Same',
      })
    })

    it('sends PUT /api/personal/duplicate-answers with the entry, the expense and "New" for "No, it\'s new"', async () => {
      const { fetchMock, card } = await renderTwoPairs({ after: octoberWithTwoPairsAfterDinnerIsNew })

      fireEvent.click(newButton(card))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(1)
      })
      expect(sentBody(fetchMock, 'PUT', duplicateAnswersUrl)).toEqual({
        entryId: dinnerPair.entryId,
        expenseId: dinnerPair.expenseId,
        answer: 'New',
      })
    })

    it('sends no other request to the duplicate answers and none for the second pair', async () => {
      const { fetchMock, card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))
      await waitForCardToSay(taxiSentence())

      expect(requestCount(fetchMock, 'PUT', duplicateAnswersUrl)).toBe(1)
      expect(requestCount(fetchMock, 'DELETE', duplicateAnswersUrl)).toBe(0)
    })

    it('sends one request when "Same, merge" is tapped twice at once', async () => {
      const { fetchMock, card } = await renderTwoPairs({ answer: () => neverAnswer() })

      fireEvent.click(sameButton(card))
      fireEvent.click(sameButton(card))

      await settle()
      expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(1)
    })

    it('sends one request when "Same, merge" and then "No, it\'s new" are tapped at once', async () => {
      const { fetchMock, card } = await renderTwoPairs({ answer: () => neverAnswer() })

      fireEvent.click(sameButton(card))
      fireEvent.click(newButton(card))

      await settle()
      expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(1)
      expect(sentBody(fetchMock, 'PUT', duplicateAnswersUrl).answer).toBe('Same')
    })

    it('disables both buttons while the answer is on its way', async () => {
      const { card } = await renderTwoPairs({ answer: () => neverAnswer() })

      fireEvent.click(newButton(card))

      await waitFor(() => {
        expect(isDisabled(sameButton(card))).toBe(true)
      })
      expect(isDisabled(newButton(card))).toBe(true)
    })

    it('keeps the card and its sentence on screen while the answer is on its way', async () => {
      const { card } = await renderTwoPairs({ answer: () => neverAnswer() })

      fireEvent.click(sameButton(card))
      await settle()

      expect(queryDuplicateCard()).toBe(card)
      expect(cardText(card)).toContain(dinnerSentence())
    })
  })

  describe('after the server accepted the answer', () => {
    it('reads the month again and shows the second pair in place of the first', async () => {
      const { fetchMock, card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))

      await waitForCardToSay(taxiSentence())
      expect(requestCount(fetchMock, 'GET', monthUrl)).toBe(2)
      expect(cardText(queryDuplicateCard() ?? document.body)).not.toContain('Dinner')
    })

    it.each(languages)('shows the second pair after "No, it\'s new" too, and lets both buttons be tapped again (%s)', async (language) => {
      const { card } = await renderTwoPairs({ language, after: octoberWithTwoPairsAfterDinnerIsNew })

      fireEvent.click(newButton(card, language))

      await waitForCardToSay(taxiSentence(language))
      const next = await findDuplicateCard(language)
      expect(isDisabled(sameButton(next, language))).toBe(false)
      expect(isDisabled(newButton(next, language))).toBe(false)
    })

    it('answers the second pair with its own entry and expense', async () => {
      const { fetchMock, card } = await renderTwoPairs()
      fireEvent.click(sameButton(card))
      await waitForCardToSay(taxiSentence())

      fireEvent.click(newButton(await findDuplicateCard()))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(2)
      })
      expect(sentBody(fetchMock, 'PUT', duplicateAnswersUrl, 1)).toEqual({
        entryId: octoberWithTwoPairs.possibleDuplicates[1]?.entryId,
        expenseId: octoberWithTwoPairs.possibleDuplicates[1]?.expenseId,
        answer: 'New',
      })
    })

    it('removes the card when that was the last pair', async () => {
      const { card } = await renderLastPair()

      fireEvent.click(sameButton(card))

      await waitFor(() => {
        expect(queryDuplicateCard()).toBeNull()
      })
    })

    it('marks every cached month of entries and every cached summary out of date', async () => {
      const { queryClient, card } = await renderTwoPairs({
        seedCache: (client) => {
          client.setQueryData(personalEntriesQueryKey('2026-09'), 'a month of entries')
          client.setQueryData(personalSummaryQueryKey('2026-09'), 'a summary')
          client.setQueryData(personalSummaryQueryKey('2026-10'), 'a summary')
        },
      })

      fireEvent.click(sameButton(card))
      await waitForCardToSay(taxiSentence())

      expect(queryClient.getQueryState(personalEntriesQueryKey('2026-09'))?.isInvalidated).toBe(true)
      expect(queryClient.getQueryState(personalSummaryQueryKey('2026-09'))?.isInvalidated).toBe(true)
      expect(queryClient.getQueryState(personalSummaryQueryKey('2026-10'))?.isInvalidated).toBe(true)
    })

    it('shows the merged entry in the list as merged once the month is read again', async () => {
      const { card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))

      await waitFor(() => {
        const list = screen.getByRole('list', { name: translated('en', 'budget.screen.entries') })
        expect(readableText(entryRowContaining(list, mergedDinnerOutEntry.note ?? ''))).toContain(
          mergedLabel('en', dinnerShare.title ?? '', dinnerShare.groupName),
        )
      })
    })

    it('shows no Undo toast, because the answers are changed from the entry afterwards', async () => {
      const { card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))
      await waitForCardToSay(taxiSentence())

      expect(toastsShownWithOptions().filter((shown) => shown.options.action !== undefined)).toEqual([])
      expect(errorToastTexts()).toEqual([])
    })

    it('sends no further request to the duplicate answers on its own', async () => {
      const { fetchMock, card } = await renderTwoPairs()

      fireEvent.click(sameButton(card))
      await waitForCardToSay(taxiSentence())
      await settle()

      expect(requestCount(fetchMock, 'PUT', duplicateAnswersUrl)).toBe(1)
      expect(requestCount(fetchMock, 'DELETE', duplicateAnswersUrl)).toBe(0)
    })
  })

  describe('the focus after an answer', () => {
    it('moves to "Same, merge" of the next card, whichever button was pressed', async () => {
      const { card } = await renderTwoPairs({ after: octoberWithTwoPairsAfterDinnerIsNew })
      const pressed = newButton(card)
      pressed.focus()

      fireEvent.click(pressed)

      await waitForCardToSay(taxiSentence())
      await waitFor(() => {
        expect(document.activeElement).toBe(sameButton(screen.getByRole('region', { name: translated('en', 'budget.duplicates.title') })))
      })
    })

    it('moves to the heading "Budget" of the screen when no card is left, so it does not fall back to the page', async () => {
      const { card } = await renderLastPair()
      const pressed = sameButton(card)
      pressed.focus()

      fireEvent.click(pressed)

      await waitFor(() => {
        expect(queryDuplicateCard()).toBeNull()
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(
          screen.getByRole('heading', { level: 1, name: translated('en', 'nav.budget') }),
        )
      })
    })

    it('stays on the pressed button when the answer is refused', async () => {
      const { card } = await renderTwoPairs({ answer: () => problemAnswer(400, 'DUPLICATE_ANSWER_INVALID') })
      const pressed = sameButton(card)
      pressed.focus()

      fireEvent.click(pressed)

      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })
      expect(document.activeElement).toBe(pressed)
    })
  })

  describe('when the server refuses the answer', () => {
    const refusals: [string, AnswerFactory, string][] = [
      ['DUPLICATE_ANSWER_INVALID', problemAnswer(400, 'DUPLICATE_ANSWER_INVALID'), 'errors.DUPLICATE_ANSWER_INVALID'],
      ['GUEST_NOT_ALLOWED', problemAnswer(403, 'GUEST_NOT_ALLOWED'), 'errors.GUEST_NOT_ALLOWED'],
      ['a network failure', networkFailureAnswer(), 'errors.network'],
    ]

    it.each(languages.flatMap((language) => refusals.map(([name, answer, key]) => [language, name, answer, key] as const)))(
      'shows the translated message as an error toast and keeps the card (%s, %s)',
      async (language, _name, answer, key) => {
        const { card } = await renderTwoPairs({ language, answer: () => answer })

        fireEvent.click(sameButton(card, language))

        await waitFor(() => {
          expect(errorToastTexts()).toEqual([translated(language, key)])
        })
        expect(queryDuplicateCard(language)).toBe(card)
        expect(cardText(card)).toContain(dinnerSentence(language))
      },
    )

    it.each(refusals)('lets both buttons be tapped again after %s', async (_name, answer) => {
      const { fetchMock, card } = await renderTwoPairs({ answer: () => answer })
      fireEvent.click(sameButton(card))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })

      expect(isDisabled(sameButton(card))).toBe(false)
      expect(isDisabled(newButton(card))).toBe(false)
      fireEvent.click(newButton(card))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', duplicateAnswersUrl)).toHaveLength(2)
      })
      expect(sentBody(fetchMock, 'PUT', duplicateAnswersUrl, 1).answer).toBe('New')
    })

    it.each(refusals)('does not read the month again after %s, because nothing is out of date', async (_name, answer) => {
      const { fetchMock, card } = await renderTwoPairs({ answer: () => answer })

      fireEvent.click(sameButton(card))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })
      await settle()

      expect(requestCount(fetchMock, 'GET', monthUrl)).toBe(1)
    })

    it('shows no Undo toast and no success toast for a refusal', async () => {
      const { card } = await renderTwoPairs({ answer: () => problemAnswer(400, 'DUPLICATE_ANSWER_INVALID') })

      fireEvent.click(sameButton(card))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })

      expect(toastsShownWithOptions()).toEqual([])
    })
  })

  describe('when the pair is out of date', () => {
    it.each(
      languages.flatMap((language) =>
        [
          ['EXPENSE_NOT_FOUND', 'errors.EXPENSE_NOT_FOUND'],
          ['PERSONAL_ENTRY_NOT_FOUND', 'errors.PERSONAL_ENTRY_NOT_FOUND'],
        ].map(([code, key]) => [language, code, key] as const),
      ),
    )('shows the translated message for %s, reads the month again and shows the next pair (%s)', async (language, code, key) => {
      const { fetchMock, card } = await renderTwoPairs({
        language,
        after: octoberWithTwoPairsAfterDinnerIsNew,
        answer: (month) => month.refuseAnswer(() => problemResponse(404, code)),
      })

      fireEvent.click(sameButton(card, language))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated(language, key)])
      })
      await waitForCardToSay(taxiSentence(language))
      expect(requestCount(fetchMock, 'GET', monthUrl)).toBe(2)
      expect(requestCount(fetchMock, 'PUT', duplicateAnswersUrl)).toBe(1)
    })

    it.each(['EXPENSE_NOT_FOUND', 'PERSONAL_ENTRY_NOT_FOUND'])('removes the card when %s was the only pair', async (code) => {
      const month = monthThatChangesWhenAnswered(octoberWithOnlyTheDinnerPair, octoberWithOnlyTheDinnerAnsweredNew)
      await renderBudget({
        months: [octoberWithOnlyTheDinnerPair],
        answers: {
          [personalEntriesGet('2026-10')]: month.monthAnswer,
          [duplicateAnswersPut]: month.refuseAnswer(() => problemResponse(404, code)),
        },
      })
      const card = await findDuplicateCard()

      fireEvent.click(newButton(card))

      await waitFor(() => {
        expect(queryDuplicateCard()).toBeNull()
      })
      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated('en', `errors.${code}`)])
      })
    })
  })
})
