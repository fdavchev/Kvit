import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { groupStateChangedCodes } from '@/core/api/queryClient'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  anaClosingItem,
  dashboardOf,
  dashboardPath,
  flatGroupId,
  fullDashboard,
  markoPaymentItem,
  quietDashboard,
} from '@/test/dashboardTestData'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import {
  cardButton,
  cardOf,
  findNeedsBar,
  liveDashboardAnswer,
  openNeedsBar,
  plainTextOf,
  renderHome,
  textMatching,
  type LiveDashboard,
} from '@/test/homeTestHelpers'
import {
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { neverAnsweringFetch } from '@/test/settlementActionsTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const confirmClosingPath = `/api/groups/${flatGroupId}/closing/confirm`
const objectClosingPath = `/api/groups/${flatGroupId}/closing/object`

function closingSentence(language: Language = 'en'): string {
  return translated(language, 'closing.prompt', { name: 'Ana', group: 'Flat' })
}

async function renderLive(
  answers: Record<string, AnswerFactory> = {},
  language: Language = 'en',
  startFrom = fullDashboard,
) {
  const live: LiveDashboard = { current: startFrom }
  const rendered = await renderHome({
    language,
    answers: {
      [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
      [`POST ${confirmClosingPath}`]: () => {
        live.current = dashboardOf({ needsYou: [markoPaymentItem] })
        return new Response(null, { status: 204 })
      },
      [`POST ${objectClosingPath}`]: () => {
        live.current = dashboardOf({ needsYou: [markoPaymentItem] })
        return new Response(null, { status: 204 })
      },
      ...answers,
    },
  })
  await screen.findByRole('heading', { name: translated(language, 'home.groups') })
  openNeedsBar(language)
  return { live, ...rendered }
}

function objectButton(card: HTMLElement, language: Language = 'en'): HTMLElement {
  return cardButton(card, translated(language, 'closing.object'))
}

function confirmButton(card: HTMLElement, language: Language = 'en'): HTMLElement {
  return cardButton(card, translated(language, 'settlements.confirm'))
}

async function openObjectSheet(language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(objectButton(cardOf(closingSentence(language)), language))
  return screen.findByRole('dialog', { name: translated(language, 'closing.objectTitle') })
}

describe('a closing card', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(languages)('says who wants to close which group, in the same words as the closing bar, in %s', async (language) => {
    await renderLive({}, language)

    const card = cardOf(closingSentence(language))

    expect(plainTextOf(card)).toContain(closingSentence(language))
  })

  it('opens the group when the text of the card is tapped', async () => {
    const { router } = await renderLive()

    fireEvent.click(screen.getByText(textMatching(closingSentence())))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(flatGroupId))
    })
  })

  it('wraps the text of the card in a link to the group', async () => {
    await renderLive()

    expect(screen.getByText(textMatching(closingSentence())).closest('a')?.getAttribute('href')).toBe(
      routes.group(flatGroupId),
    )
  })

  it.each(languages)('has the buttons Object and Confirm in %s', async (language) => {
    await renderLive({}, language)
    const card = cardOf(closingSentence(language))

    const labels = within(card)
      .getAllByRole('button')
      .map((button) => plainTextOf(button))

    expect(labels).toEqual([translated(language, 'closing.object'), translated(language, 'settlements.confirm')])
  })

  describe('Confirm', () => {
    it('sends POST confirm with no body to the closing of the group of the card', async () => {
      const { fetchMock } = await renderLive()

      fireEvent.click(confirmButton(cardOf(closingSentence())))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', confirmClosingPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', confirmClosingPath)[0].body).toBeUndefined()
    })

    it('asks for the dashboard again and the card disappears', async () => {
      const { fetchMock } = await renderLive()

      fireEvent.click(confirmButton(cardOf(closingSentence())))

      await waitFor(() => {
        expect(screen.queryByText(textMatching(closingSentence()))).toBeNull()
      })
      expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(2)
      expect(within(findNeedsBar()).getByText('1')).toBeTruthy()
    })

    it('replaces the bar with the quiet line when it was the last thing', async () => {
      let timesAsked = 0
      await renderLive({
        [`GET ${dashboardPath}`]: () => {
          timesAsked += 1
          return Response.json(timesAsked === 1 ? dashboardOf({ needsYou: [anaClosingItem] }) : quietDashboard)
        },
        [`POST ${confirmClosingPath}`]: () => new Response(null, { status: 204 }),
      })

      fireEvent.click(confirmButton(cardOf(closingSentence())))

      expect(await screen.findByText(translated('en', 'home.nothingNeedsYou'))).toBeTruthy()
    })

    it('disables both buttons of the card while the server has not answered, and sends one request', async () => {
      await renderLive()
      const neverAnswering = neverAnsweringFetch()
      const card = cardOf(closingSentence())
      const confirm = confirmButton(card)

      fireEvent.click(confirm)
      await expectDisabledWhilePending(confirm)
      fireEvent.click(confirm)

      expect(objectButton(card).matches(':disabled')).toBe(true)
      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'CLOSING_ALREADY_ANSWERED'],
      [400, 'CLOSING_OWNER_CANNOT_ANSWER'],
      [404, 'GROUP_NOT_FOUND'],
    ])('shows the translated text of %i %s inside the card and no toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive({ [`POST ${confirmClosingPath}`]: problemAnswer(status, code) })
      const card = cardOf(closingSentence())

      fireEvent.click(confirmButton(card))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
      expect(confirmButton(card).matches(':disabled')).toBe(false)
    })

    it('shows the network text inside the card when the server cannot be reached', async () => {
      await renderLive({ [`POST ${confirmClosingPath}`]: networkFailureAnswer() })
      const card = cardOf(closingSentence())

      fireEvent.click(confirmButton(card))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it.each(groupStateChangedCodes)(
      'shows the translated text of 400 %s as a toast, takes the stale card away and shows nothing inside the card',
      async (code) => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
        const { live } = await renderLive({
          [`POST ${confirmClosingPath}`]: () => {
            live.current = quietDashboard
            return problemAnswer(400, code)()
          },
        })
        const card = cardOf(closingSentence())

        fireEvent.click(confirmButton(card))

        await waitFor(() => {
          expect(screen.queryByText(textMatching(closingSentence()))).toBeNull()
        })
        expect(vi.mocked(toast.error)).toHaveBeenCalledTimes(1)
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', `errors.${code}`))
      },
    )
  })

  describe('Object', () => {
    it('opens the reason sheet of the closing screens, with the reason field and Send', async () => {
      await renderLive()

      const dialog = await openObjectSheet()

      expect(within(dialog).getByRole('textbox')).toBeTruthy()
      expect(within(dialog).getByRole('button', { name: translated('en', 'closing.send') })).toBeTruthy()
    })

    it.each(languages)('titles the sheet "Object to closing" in %s', async (language) => {
      await renderLive({}, language)

      expect(await openObjectSheet(language)).toBeTruthy()
    })

    it('sends nothing until Send is tapped', async () => {
      const { fetchMock } = await renderLive()

      await openObjectSheet()

      expect(requestCount(fetchMock, 'POST', objectClosingPath)).toBe(0)
    })

    it('sends the reason that was typed to the closing of the group of the card', async () => {
      const { fetchMock } = await renderLive()
      const dialog = await openObjectSheet()

      fireEvent.change(within(dialog).getByRole('textbox'), { target: { value: 'Not yet, Dime has not paid' } })
      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'closing.send') }))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', objectClosingPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', objectClosingPath)[0].body).toEqual({ reason: 'Not yet, Dime has not paid' })
    })

    it('sends a null reason when the reason was left empty, because the reason is optional', async () => {
      const { fetchMock } = await renderLive()
      const dialog = await openObjectSheet()

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'closing.send') }))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', objectClosingPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', objectClosingPath)[0].body).toEqual({ reason: null })
    })

    it('closes the sheet, shows the success toast and takes the card away after Send', async () => {
      await renderLive()
      const dialog = await openObjectSheet()

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'closing.send') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith(translated('en', 'closing.objected'))
      await waitFor(() => {
        expect(screen.queryByText(textMatching(closingSentence()))).toBeNull()
      })
    })

    it('shows the translated error inside the sheet and keeps it open when the server refuses', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive({ [`POST ${objectClosingPath}`]: problemAnswer(400, 'CLOSING_REASON_TOO_LONG') })
      const dialog = await openObjectSheet()

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'closing.send') }))

      expect((await within(dialog).findByRole('alert')).textContent).toContain(
        translated('en', 'errors.CLOSING_REASON_TOO_LONG'),
      )
      expect(screen.getByRole('dialog')).toBeTruthy()
      expect(vi.mocked(toast.success)).not.toHaveBeenCalled()
    })

    it('leaves the card and sends nothing when the sheet is cancelled', async () => {
      const { fetchMock } = await renderLive()
      const dialog = await openObjectSheet()

      fireEvent.click(within(dialog).getByRole('button', { name: translated('en', 'settlements.cancel') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(requestCount(fetchMock, 'POST', objectClosingPath)).toBe(0)
      expect(screen.getByText(textMatching(closingSentence()))).toBeTruthy()
    })
  })
})
