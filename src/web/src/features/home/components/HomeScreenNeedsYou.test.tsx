import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  anaClosingItem,
  danaPaymentItem,
  dashboardOf,
  dashboardPath,
  fullDashboard,
  greeceGroupId,
  markoPaymentItem,
  markoPaymentSettlementId,
  quietDashboard,
} from '@/test/dashboardTestData'
import {
  cardButton,
  cardOf,
  findNeedsBar,
  liveDashboardAnswer,
  openNeedsBar,
  paragraphWithText,
  plainTextOf,
  renderHome,
  type LiveDashboard,
} from '@/test/homeTestHelpers'
import { problemResponse } from '@/test/apiTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { neverAnsweringFetch } from '@/test/settlementActionsTestHelpers'
import {
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

const confirmPaymentPath = `/api/groups/${greeceGroupId}/settlements/${markoPaymentSettlementId}/confirm`
const rejectPaymentPath = `/api/groups/${greeceGroupId}/settlements/${markoPaymentSettlementId}/reject`

function markoSentence(language: 'en' | 'mk' = 'en'): string {
  return translated(language, 'balances.saysPaidYou', {
    from: 'Marko',
    amount: formatMoney(60000, 'MKD', language),
  })
}

function danaSentence(language: 'en' | 'mk' = 'en'): string {
  return translated(language, 'balances.saysPaidYou', {
    from: 'Dana',
    amount: formatMoney(1550, 'EUR', language),
  })
}

function closingSentence(language: 'en' | 'mk' = 'en'): string {
  return translated(language, 'closing.prompt', { name: 'Ana', group: 'Flat' })
}

async function findBar(language: 'en' | 'mk' = 'en'): Promise<HTMLElement> {
  await screen.findByRole('heading', { name: translated(language, 'home.groups') })
  return findNeedsBar(language)
}

describe('the pinned Needs you bar', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the number of things in a circle of its own, apart from the text', async () => {
    await renderHome()
    const bar = await findBar()

    const circle = within(bar).getByText('2')

    expect(circle.textContent).toBe('2')
    expect(circle.classList.contains('rounded-full')).toBe(true)
  })

  it('writes the text without a number', async () => {
    await renderHome()
    const bar = await findBar()

    const text = within(bar).getByText(translated('en', 'home.thingsNeedYou_other'))

    expect(text.textContent).toBe('things need you')
    expect(text.textContent).not.toMatch(/\d/)
    expect(bar.textContent).not.toContain('2 things')
  })

  it('uses the singular text and the number 1 when one thing needs the person', async () => {
    await renderHome({ dashboard: dashboardOf({ needsYou: [markoPaymentItem] }) })
    const bar = await findBar()

    expect(within(bar).getByText('1').classList.contains('rounded-full')).toBe(true)
    expect(within(bar).getByText(translated('en', 'home.thingsNeedYou_one'))).toBeTruthy()
    expect(within(bar).queryByText(translated('en', 'home.thingsNeedYou_other'))).toBeNull()
  })

  it.each(languages)('uses the plural text for two things in %s', async (language) => {
    await renderHome({ language })
    const bar = await findBar(language)

    expect(within(bar).getByText(translated(language, 'home.thingsNeedYou_other'))).toBeTruthy()
  })

  it.each(languages)('uses the singular text for one thing in %s', async (language) => {
    await renderHome({ language, dashboard: dashboardOf({ needsYou: [anaClosingItem] }) })
    const bar = await findBar(language)

    expect(within(bar).getByText(translated(language, 'home.thingsNeedYou_one'))).toBeTruthy()
  })

  it('counts three things with the number 3', async () => {
    await renderHome({ dashboard: dashboardOf({ needsYou: [anaClosingItem, markoPaymentItem, danaPaymentItem] }) })
    const bar = await findBar()

    expect(within(bar).getByText('3')).toBeTruthy()
  })

  it('is one button that starts closed', async () => {
    await renderHome()
    const bar = await findBar()

    expect(bar.tagName).toBe('BUTTON')
    expect(bar.getAttribute('aria-expanded')).toBe('false')
  })

  it('keeps the cards out of the page while it is closed', async () => {
    await renderHome()
    await findBar()

    expect(screen.queryByText(paragraphWithText(markoSentence()))).toBeNull()
    expect(screen.queryByText(paragraphWithText(closingSentence()))).toBeNull()
    expect(screen.queryByRole('button', { name: translated('en', 'settlements.confirm') })).toBeNull()
  })

  it('opens the cards when tapped and says so with aria-expanded', async () => {
    await renderHome()
    await findBar()

    openNeedsBar()

    expect(findNeedsBar().getAttribute('aria-expanded')).toBe('true')
    expect(screen.getByText(paragraphWithText(markoSentence()))).toBeTruthy()
    expect(screen.getByText(paragraphWithText(closingSentence()))).toBeTruthy()
  })

  it('closes the cards again when tapped a second time', async () => {
    await renderHome()
    await findBar()

    openNeedsBar()
    openNeedsBar()

    expect(findNeedsBar().getAttribute('aria-expanded')).toBe('false')
    expect(screen.queryByText(paragraphWithText(markoSentence()))).toBeNull()
  })

  it('shows the cards in the order the server sent them, closing first', async () => {
    await renderHome()
    await findBar()

    openNeedsBar()

    const cards = [cardOf(closingSentence()), cardOf(markoSentence())]
    const parent = cards[0].parentElement
    expect(parent).toBe(cards[1].parentElement)
    expect(Array.from(parent?.children ?? [])).toEqual(cards)
  })

  it('shows no bar and the quiet line when nothing needs the person', async () => {
    await renderHome({ dashboard: quietDashboard })

    expect(await screen.findByText(translated('en', 'home.nothingNeedsYou'))).toBeTruthy()
    expect(screen.queryByText(translated('en', 'home.thingsNeedYou_one'))).toBeNull()
    expect(screen.queryByText(translated('en', 'home.thingsNeedYou_other'))).toBeNull()
  })

  it.each(languages)('writes the quiet line in %s', async (language) => {
    await renderHome({ language, dashboard: quietDashboard })

    expect(await screen.findByText(translated(language, 'home.nothingNeedsYou'))).toBeTruthy()
  })

  it('shows no quiet line while something needs the person', async () => {
    await renderHome()
    await findBar()

    expect(screen.queryByText(translated('en', 'home.nothingNeedsYou'))).toBeNull()
  })
})

describe('a payment card', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  async function renderWithCardsOpen(language: 'en' | 'mk' = 'en') {
    const rendered = await renderHome({ language })
    await findBar(language)
    openNeedsBar(language)
    return rendered
  }

  it.each(languages)('says who paid and how much, with the group name, in %s', async (language) => {
    await renderWithCardsOpen(language)

    const card = cardOf(markoSentence(language))

    expect(plainTextOf(card)).toContain('Greece trip')
  })

  it('opens the group when the text of the card is tapped', async () => {
    const { router } = await renderWithCardsOpen()

    fireEvent.click(screen.getByText(paragraphWithText(markoSentence())))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(greeceGroupId))
    })
  })

  it('wraps the text of the card in a link to the group', async () => {
    await renderWithCardsOpen()

    const link = screen.getByText(paragraphWithText(markoSentence())).closest('a')

    expect(link?.getAttribute('href')).toBe(routes.group(greeceGroupId))
  })

  it.each(languages)('has the buttons Reject and Confirm in %s', async (language) => {
    await renderWithCardsOpen(language)
    const card = cardOf(markoSentence(language))

    const labels = within(card)
      .getAllByRole('button')
      .map((button) => plainTextOf(button))

    expect(labels).toEqual([translated(language, 'settlements.reject'), translated(language, 'settlements.confirm')])
  })
})

describe('answering a payment card from the home screen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  async function renderLive(extra: Record<string, AnswerFactory> = {}) {
    const live: LiveDashboard = { current: fullDashboard }
    const rendered = await renderHome({
      answers: {
        [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
        [`POST ${confirmPaymentPath}`]: () => {
          live.current = dashboardOf({ needsYou: [anaClosingItem], totals: { owed: [], owe: [] } })
          return new Response(null, { status: 204 })
        },
        [`POST ${rejectPaymentPath}`]: () => {
          live.current = dashboardOf({ needsYou: [anaClosingItem] })
          return new Response(null, { status: 204 })
        },
        ...extra,
      },
    })
    await findBar()
    openNeedsBar()
    return { live, ...rendered }
  }

  it('sends POST confirm to the settlement of the card when Confirm is tapped', async () => {
    const { fetchMock } = await renderLive()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', confirmPaymentPath)).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', confirmPaymentPath)[0].body).toBeUndefined()
    expect(requestCount(fetchMock, 'POST', rejectPaymentPath)).toBe(0)
  })

  it('sends POST reject to the settlement of the card when Reject is tapped', async () => {
    const { fetchMock } = await renderLive()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.reject')))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', rejectPaymentPath)).toBe(1)
    })
    expect(requestCount(fetchMock, 'POST', confirmPaymentPath)).toBe(0)
  })

  it.each(['settlements.confirm', 'settlements.reject'])(
    'asks for the dashboard again and the card disappears after %s',
    async (buttonKey) => {
      const { fetchMock } = await renderLive()

      fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', buttonKey)))

      await waitFor(() => {
        expect(screen.queryByText(paragraphWithText(markoSentence()))).toBeNull()
      })
      expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(2)
    },
  )

  it('keeps the other card and lowers the number in the circle to 1', async () => {
    await renderLive()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

    await waitFor(() => {
      expect(screen.queryByText(paragraphWithText(markoSentence()))).toBeNull()
    })
    expect(screen.getByText(paragraphWithText(closingSentence()))).toBeTruthy()
    expect(within(findNeedsBar()).getByText('1')).toBeTruthy()
    expect(within(findNeedsBar()).getByText(translated('en', 'home.thingsNeedYou_one'))).toBeTruthy()
  })

  it('replaces the bar with the quiet line when the last card is answered', async () => {
    const live: LiveDashboard = { current: dashboardOf({ needsYou: [markoPaymentItem] }) }
    await renderHome({
      answers: {
        [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
        [`POST ${confirmPaymentPath}`]: () => {
          live.current = quietDashboard
          return new Response(null, { status: 204 })
        },
      },
    })
    await findBar()
    openNeedsBar()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

    expect(await screen.findByText(translated('en', 'home.nothingNeedsYou'))).toBeTruthy()
    expect(screen.queryByText(translated('en', 'home.thingsNeedYou_one'))).toBeNull()
  })

  it('refreshes the totals and the group balances too, because the payment changes them', async () => {
    await renderLive()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

    await waitFor(() => {
      expect(screen.queryByRole('region', { name: translated('en', 'home.owed') })?.textContent).not.toContain(
        formatMoney(185000, 'MKD', 'en'),
      )
    })
  })

  it('disables both buttons of the card while the server has not answered, and sends one request', async () => {
    await renderLive()
    const neverAnswering = neverAnsweringFetch()
    const card = cardOf(markoSentence())
    const confirm = cardButton(card, translated('en', 'settlements.confirm'))

    fireEvent.click(confirm)
    await expectDisabledWhilePending(confirm)
    fireEvent.click(confirm)

    expect(cardButton(card, translated('en', 'settlements.reject')).matches(':disabled')).toBe(true)
    expect(neverAnswering).toHaveBeenCalledTimes(1)
  })

  it('leaves the second payment card alone while the first is answered', async () => {
    const live: LiveDashboard = { current: dashboardOf({ needsYou: [markoPaymentItem, danaPaymentItem] }) }
    const { fetchMock } = await renderHome({
      answers: {
        [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
        [`POST ${confirmPaymentPath}`]: noContentAnswer(),
      },
    })
    await findBar()
    openNeedsBar()

    fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', confirmPaymentPath)).toBe(1)
    })
    expect(cardButton(cardOf(danaSentence()), translated('en', 'settlements.confirm')).matches(':disabled')).toBe(false)
  })

  describe('when the server refuses', () => {
    it.each([
      [400, 'SETTLEMENT_NOT_PENDING'],
      [403, 'SETTLEMENT_NOT_ALLOWED'],
      [404, 'SETTLEMENT_NOT_FOUND'],
    ])('shows the translated text of %i %s inside the card, as the Balances tab does', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive({ [`POST ${confirmPaymentPath}`]: problemAnswer(status, code) })
      const card = cardOf(markoSentence())

      fireEvent.click(cardButton(card, translated('en', 'settlements.confirm')))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(cardButton(card, translated('en', 'settlements.confirm')).matches(':disabled')).toBe(false)
    })

    it('shows the network text inside the card when the server cannot be reached', async () => {
      await renderLive({ [`POST ${rejectPaymentPath}`]: networkFailureAnswer() })
      const card = cardOf(markoSentence())

      fireEvent.click(cardButton(card, translated('en', 'settlements.reject')))

      expect((await within(card).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it.each([
      ['settlements.confirm', 400, 'SETTLEMENT_NOT_PENDING', confirmPaymentPath],
      ['settlements.reject', 400, 'SETTLEMENT_NOT_PENDING', rejectPaymentPath],
      ['settlements.confirm', 403, 'SETTLEMENT_NOT_ALLOWED', confirmPaymentPath],
      ['settlements.reject', 404, 'SETTLEMENT_NOT_FOUND', rejectPaymentPath],
    ])(
      'asks for the dashboard again after %s is refused with %i %s, so the stale card can go away',
      async (buttonKey, status, code, path) => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
        const { fetchMock } = await renderLive({ [`POST ${path}`]: problemAnswer(status, code) })

        fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', buttonKey)))

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', path)).toBe(1)
          expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(2)
        })
      },
    )

    it('asks for the dashboard again when the server cannot be reached', async () => {
      const { fetchMock } = await renderLive({ [`POST ${confirmPaymentPath}`]: networkFailureAnswer() })

      fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', dashboardPath)).toBe(2)
      })
    })

    it('removes the card when the refused payment is already gone from the fresh dashboard', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const live: LiveDashboard = { current: fullDashboard }
      await renderHome({
        answers: {
          [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
          [`POST ${confirmPaymentPath}`]: () => {
            live.current = dashboardOf({ needsYou: [anaClosingItem] })
            return problemResponse(400, 'SETTLEMENT_NOT_PENDING')
          },
        },
      })
      await findBar()
      openNeedsBar()

      fireEvent.click(cardButton(cardOf(markoSentence()), translated('en', 'settlements.confirm')))

      await waitFor(() => {
        expect(screen.queryByText(paragraphWithText(markoSentence()))).toBeNull()
      })
      expect(screen.getByText(paragraphWithText(closingSentence()))).toBeTruthy()
    })

    it('shows the error in Macedonian for a person who uses Macedonian', async () => {
      const live: LiveDashboard = { current: fullDashboard }
      await renderHome({
        language: 'mk',
        answers: {
          [`GET ${dashboardPath}`]: liveDashboardAnswer(live),
          [`POST ${confirmPaymentPath}`]: problemAnswer(400, 'SETTLEMENT_NOT_PENDING'),
        },
      })
      await findBar('mk')
      openNeedsBar('mk')
      const card = cardOf(markoSentence('mk'))

      fireEvent.click(cardButton(card, translated('mk', 'settlements.confirm')))

      expect((await within(card).findByRole('alert')).textContent).toContain(
        translated('mk', 'errors.SETTLEMENT_NOT_PENDING'),
      )
    })
  })
})
