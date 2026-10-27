import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { isChecked } from '@/test/checkedStateTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import {
  neverAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const budgetPut = `PUT ${endpoints.meBudgetCurrency}`

type BudgetTotalCurrency = Me['budgetTotalCurrency']

interface Choice {
  name: string
  textKey: string
  value: BudgetTotalCurrency
}

const choices: Choice[] = [
  { name: 'keep apart', textKey: 'settings.budget.apart', value: null },
  { name: 'in MKD', textKey: 'settings.budget.inMkd', value: 'MKD' },
  { name: 'in EUR', textKey: 'settings.budget.inEur', value: 'EUR' },
]

const keepApart = choices[0]
const inMkd = choices[1]
const inEur = choices[2]

interface RenderOptions {
  me?: Me
  language?: Language
  answers?: Record<string, AnswerFactory>
}

async function renderSettings(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({ ...options.answers })
  const rendered = await renderElementWithProviders(<SettingsScreen />, routes.settings, {
    language,
    seedCache: seedMe({ ...(options.me ?? testMe), language }),
  })
  return { fetchMock, ...rendered }
}

function totalChoiceGroup(language: Language = 'en'): HTMLElement {
  return screen.getByRole('radiogroup', { name: translated(language, 'settings.budget.showOneTotal') })
}

function radioFor(choice: Choice, language: Language = 'en'): HTMLElement {
  return within(totalChoiceGroup(language)).getByRole('radio', { name: translated(language, choice.textKey) })
}

function expectOnlyChecked(chosen: Choice, language: Language = 'en'): void {
  for (const choice of choices) {
    expect(isChecked(radioFor(choice, language)), choice.name).toBe(choice === chosen)
  }
}

async function letRequestsSettle(): Promise<void> {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0))
  })
}

function cachedBudgetTotalCurrency(queryClient: QueryClient): unknown {
  return queryClient.getQueryData<Me>(meQueryKey)?.budgetTotalCurrency
}

describe('SettingsScreen budget card', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('what is shown to a full account', () => {
    it.each(languages)('has a Budget heading (%s)', async (language) => {
      await renderSettings({ language })

      expect(
        screen.getByRole('heading', { level: 2, name: translated(language, 'settings.budget.title') }),
      ).toBeTruthy()
    })

    it.each(languages)('has a radio group named "Show one total" with the three options in the order keep apart, MKD, EUR (%s)', async (language) => {
      await renderSettings({ language })

      const radios = within(totalChoiceGroup(language)).getAllByRole('radio')

      expect(radios).toEqual(choices.map((choice) => radioFor(choice, language)))
    })

    it.each(languages)('explains that euro amounts are converted with the rate saved on each entry (%s)', async (language) => {
      await renderSettings({ language })

      expect(screen.getByText(translated(language, 'settings.budget.hint'))).toBeTruthy()
    })

    it.each([
      ['keep apart', keepApart, null],
      ['in MKD', inMkd, 'MKD'],
      ['in EUR', inEur, 'EUR'],
    ] as const)('shows only "%s" as chosen when the saved setting is %j', async (_name, chosen, saved) => {
      await renderSettings({ me: { ...testMe, budgetTotalCurrency: saved } })

      expectOnlyChecked(chosen)
    })

    it.each(languages)('shows the saved choice as chosen (%s)', async (language) => {
      await renderSettings({ language, me: { ...testMe, budgetTotalCurrency: 'EUR' } })

      expectOnlyChecked(inEur, language)
    })

    it('has no Save button because choosing saves at once', async () => {
      await renderSettings()

      expect(screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })).toBeNull()
    })
  })

  describe('what a guest sees', () => {
    it.each(languages)('shows no Budget card, no choice and no helper line (%s)', async (language) => {
      await renderSettings({ me: testGuestMe, language })

      expect(screen.queryByRole('heading', { name: translated(language, 'settings.budget.title') })).toBeNull()
      expect(screen.queryByRole('radiogroup')).toBeNull()
      expect(screen.queryAllByRole('radio')).toEqual([])
      expect(screen.queryByText(translated(language, 'settings.budget.hint'))).toBeNull()
    })
  })

  describe('choosing', () => {
    it.each([inMkd, inEur])('sends PUT /api/me/budget-currency for $name after one tap and nothing else', async (choice) => {
      const { fetchMock } = await renderSettings({ answers: { [budgetPut]: noContentAnswer() } })

      fireEvent.click(radioFor(choice))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', endpoints.meBudgetCurrency)).toBe(1)
      })
      expect(fetchMock).toHaveBeenCalledOnce()
      expect(requestsOf(fetchMock, 'PUT', endpoints.meBudgetCurrency)[0].body).toEqual({
        budgetTotalCurrency: choice.value,
      })
    })

    it('sends null when "No, keep MKD and EUR apart" is tapped while a currency is chosen', async () => {
      const { fetchMock } = await renderSettings({
        me: { ...testMe, budgetTotalCurrency: 'EUR' },
        answers: { [budgetPut]: noContentAnswer() },
      })

      fireEvent.click(radioFor(keepApart))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', endpoints.meBudgetCurrency)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'PUT', endpoints.meBudgetCurrency)[0].body).toEqual({
        budgetTotalCurrency: null,
      })
    })

    it.each([
      ['in MKD', inMkd, 'MKD'],
      ['in EUR', inEur, 'EUR'],
    ] as const)('shows "%s" as chosen and puts %s in the me cache once the server accepted it', async (_name, choice, saved) => {
      const { queryClient } = await renderSettings({ answers: { [budgetPut]: noContentAnswer() } })

      fireEvent.click(radioFor(choice))

      await waitFor(() => {
        expectOnlyChecked(choice)
      })
      expect(cachedBudgetTotalCurrency(queryClient)).toBe(saved)
    })

    it('shows "No, keep MKD and EUR apart" as chosen and puts null in the me cache after it is tapped', async () => {
      const { queryClient } = await renderSettings({
        me: { ...testMe, budgetTotalCurrency: 'MKD' },
        answers: { [budgetPut]: noContentAnswer() },
      })

      fireEvent.click(radioFor(keepApart))

      await waitFor(() => {
        expectOnlyChecked(keepApart)
      })
      expect(cachedBudgetTotalCurrency(queryClient)).toBeNull()
    })

    it.each([
      ['keep apart', keepApart, null],
      ['in MKD', inMkd, 'MKD'],
      ['in EUR', inEur, 'EUR'],
    ] as const)('sends nothing when the chosen option "%s" is tapped again', async (_name, chosen, saved) => {
      const { fetchMock } = await renderSettings({ me: { ...testMe, budgetTotalCurrency: saved } })

      fireEvent.click(radioFor(chosen))
      await letRequestsSettle()

      expect(fetchMock).not.toHaveBeenCalled()
      expectOnlyChecked(chosen)
    })

    it('shows no success toast, because the chosen option is the feedback', async () => {
      const { fetchMock } = await renderSettings({ answers: { [budgetPut]: noContentAnswer() } })

      fireEvent.click(radioFor(inEur))
      await waitFor(() => {
        expectOnlyChecked(inEur)
      })

      expect(fetchMock).toHaveBeenCalledOnce()
      expect(toast.success).not.toHaveBeenCalled()
      expect(toast.error).not.toHaveBeenCalled()
    })
  })

  describe('when saving fails', () => {
    it.each(languages)('shows the translated message for ME_BUDGET_CURRENCY_INVALID in an error toast (%s)', async (language) => {
      await renderSettings({ language, answers: { [budgetPut]: problemAnswer(400, 'ME_BUDGET_CURRENCY_INVALID') } })

      fireEvent.click(radioFor(inEur, language))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.ME_BUDGET_CURRENCY_INVALID'))
      })
    })

    it('shows the generic message after a server error', async () => {
      await renderSettings({ answers: { [budgetPut]: () => new Response(null, { status: 500 }) } })

      fireEvent.click(radioFor(inEur))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
    })

    it('shows the network message when the request never got an answer', async () => {
      await renderSettings({ answers: { [budgetPut]: networkFailureAnswer() } })

      fireEvent.click(radioFor(inEur))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
    })

    it('keeps the old choice and the me cache after a failure', async () => {
      const { queryClient } = await renderSettings({
        me: { ...testMe, budgetTotalCurrency: 'MKD' },
        answers: { [budgetPut]: problemAnswer(400, 'ME_BUDGET_CURRENCY_INVALID') },
      })

      fireEvent.click(radioFor(inEur))
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalled()
      })

      expectOnlyChecked(inMkd)
      expect(cachedBudgetTotalCurrency(queryClient)).toBe('MKD')
    })

    it('lets the person try again after a failure', async () => {
      let tries = 0
      const { fetchMock } = await renderSettings({
        answers: {
          [budgetPut]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : new Response(null, { status: 204 })
          },
        },
      })
      fireEvent.click(radioFor(inEur))
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledOnce()
      })

      fireEvent.click(radioFor(inEur))

      await waitFor(() => {
        expectOnlyChecked(inEur)
      })
      expect(requestCount(fetchMock, 'PUT', endpoints.meBudgetCurrency)).toBe(2)
    })
  })

  describe('while a save is running', () => {
    it('sends only one request when another option is tapped before the server answered', async () => {
      const { fetchMock } = await renderSettings({ answers: { [budgetPut]: neverAnswer() } })

      fireEvent.click(radioFor(inEur))
      fireEvent.click(radioFor(inMkd))
      await letRequestsSettle()

      expect(requestCount(fetchMock, 'PUT', endpoints.meBudgetCurrency)).toBe(1)
    })

    it('sends only one request when the same option is tapped twice before the server answered', async () => {
      const { fetchMock } = await renderSettings({ answers: { [budgetPut]: neverAnswer() } })

      fireEvent.click(radioFor(inEur))
      fireEvent.click(radioFor(inEur))
      await letRequestsSettle()

      expect(requestCount(fetchMock, 'PUT', endpoints.meBudgetCurrency)).toBe(1)
    })
  })
})
