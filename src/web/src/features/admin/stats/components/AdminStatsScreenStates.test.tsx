import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { adminStatsNow, defaultStatsUrl, novemberStats, statsWithEvents } from '@/test/adminStatsTestData'
import {
  answerWith,
  cardsContainer,
  chip,
  findStatsCard,
  findStatsTitle,
  holdAnswerFor,
  isPressed,
  pressChip,
  queryCardsContainer,
  renderAdminStats,
  requestedPaths,
  statsRequestCount,
  statsRequests,
  totalOf,
  type StatsAnswerFor,
} from '@/test/adminStatsTestHelpers'
import { problemResponse } from '@/test/apiTestHelpers'
import { classesOf } from '@/test/budgetLeftCardTestHelpers'
import { restoreRealRetryPolicy } from '@/test/expenseTestHelpers'
import { colorOfToken, compositeOver, contrastOfColors, type ThemeName } from '@/test/indexCssColors'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const neverAnsweringStats: StatsAnswerFor = () => new Promise<Response>(() => {})
const sevenDaysUrl = '/api/admin/stats?from=2026-10-29&to=2026-11-04&by=day'
const weekUrl = '/api/admin/stats?from=2026-11-01&to=2026-11-04&by=week'
const allowedPaths = ['/api/admin/stats', '/api/me', '/api/me/active']

const smallTextTokensOfTheCards = ['card-foreground', 'muted-foreground', 'positive', 'negative']
const smallestTextContrast = 4.5
const smallestBarContrast = 3

function opacityOfClasses(classes: readonly string[]): number {
  const opacities = classes.filter((name) => /^opacity-\d+$/.test(name))
  if (opacities.length !== 1) {
    throw new Error(`Expected exactly one opacity class, got "${opacities.join(' ')}" in ${classes.join(' ')}`)
  }
  return Number(opacities[0].slice('opacity-'.length)) / 100
}

function dimmedContrast(foregroundToken: string, opacity: number, theme: ThemeName): number {
  const page = colorOfToken('background', theme)
  const card = compositeOver({ rgb: colorOfToken('card', theme), alpha: opacity }, page)
  const foreground = compositeOver({ rgb: colorOfToken(foregroundToken, theme), alpha: opacity }, page)
  return contrastOfColors(foreground, card)
}

function failingThenAnswering(failure: () => Response | Error): StatsAnswerFor {
  let calls = 0
  return () => {
    calls += 1
    return calls === 1 ? failure() : Response.json(novemberStats)
  }
}

function failingFor(failingUrl: string, failure: () => Response | Error): StatsAnswerFor {
  return (url) => (url === failingUrl ? failure() : Response.json(novemberStats))
}

describe('the statistics screen, loading, errors and the look', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(adminStatsNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('while the first answer is on its way', () => {
    it('shows the title at once and placeholder blocks in a loading status, not a spinner', async () => {
      await renderAdminStats({ statsAnswerFor: neverAnsweringStats })

      expect(await findStatsTitle()).toBeTruthy()
      const look = loadingLook()
      expect(look.skeletonCount).toBeGreaterThan(0)
      expect(look.hasSpinner).toBe(false)
    })

    it('shows no card and no total yet', async () => {
      await renderAdminStats({ statsAnswerFor: neverAnsweringStats })
      await findStatsTitle()

      expect(screen.queryAllByRole('region')).toEqual([])
      expect(screen.queryByText(/Since the start/)).toBeNull()
    })

    it('takes the placeholders away when the answer arrives', async () => {
      await renderAdminStats()

      await findStatsCard('New sign-ups')

      expect(screen.queryByRole('status')).toBeNull()
      expect(document.querySelector('[data-skeleton]')).toBeNull()
    })

    it('marks the cards as not busy once they are shown', async () => {
      await renderAdminStats()
      await findStatsCard('New sign-ups')

      expect(cardsContainer().getAttribute('aria-busy')).not.toBe('true')
    })
  })

  describe('when the server refuses or does not answer', () => {
    it.each([
      'ADMIN_STATS_DAY_RANGE_TOO_LONG',
      'ADMIN_STATS_RANGE_INVALID',
      'ADMIN_STATS_FUTURE_DATE',
      'ADMIN_STATS_GROUPING_INVALID',
    ])('shows the translated message of %s in the standard error with Try again, and no card', async (code) => {
      await renderAdminStats({ statsAnswerFor: () => problemResponse(400, code) })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', `errors.${code}`))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(screen.queryAllByRole('region')).toEqual([])
    })

    it.each(languages)('writes the message of a refused range in the language of the person (%s)', async (language) => {
      await renderAdminStats({ language, statsAnswerFor: () => problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG') })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated(language, 'errors.ADMIN_STATS_DAY_RANGE_TOO_LONG'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
    })

    it('keeps the title and the chips, so another range or grouping can be picked', async () => {
      await renderAdminStats({ statsAnswerFor: () => problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG') })
      await screen.findByRole('alert')

      expect(await findStatsTitle()).toBeTruthy()
      expect(chip('Week')).toBeTruthy()
      expect(chip('12 months')).toBeTruthy()
    })

    it('shows the cards when another grouping is picked after a refusal', async () => {
      await renderAdminStats({ statsAnswerFor: failingFor(defaultStatsUrl, () => problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG')) })
      await screen.findByRole('alert')

      pressChip('Week')

      expect(await findStatsCard('New sign-ups')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows the message for a lost connection, with Try again', async () => {
      await renderAdminStats({ statsAnswerFor: () => new TypeError('Failed to fetch') })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.network'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('shows the generic message when the answer cannot be read', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderAdminStats({ statsAnswerFor: answerWith({ ...novemberStats, signUps: null }) })

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', 'errors.generic'))).toBeTruthy()
    })

    it('asks again and shows the cards when Try again is pressed', async () => {
      const { fetchMock } = await renderAdminStats({
        statsAnswerFor: failingThenAnswering(() => new TypeError('Failed to fetch')),
      })
      const alert = await screen.findByRole('alert')

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await findStatsCard('New sign-ups')).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
      expect(statsRequestCount(fetchMock, defaultStatsUrl)).toBe(2)
    })

    it('asks only once, and shows the error at once, for a refused range, with the retry policy of the app', async () => {
      const { fetchMock } = await renderAdminStats({
        statsAnswerFor: () => problemResponse(400, 'ADMIN_STATS_DAY_RANGE_TOO_LONG'),
        seedCache: restoreRealRetryPolicy,
      })

      await screen.findByRole('alert')

      expect(statsRequestCount(fetchMock, defaultStatsUrl)).toBe(1)
    })

    it('shows the error with Try again when the next range is refused after cards were shown', async () => {
      await renderAdminStats({ statsAnswerFor: failingFor(sevenDaysUrl, () => problemResponse(400, 'ADMIN_STATS_FUTURE_DATE')) })
      await findStatsCard('New sign-ups')

      pressChip('7 days')

      const alert = await screen.findByRole('alert')
      expect(within(alert).getByText(translated('en', 'errors.ADMIN_STATS_FUTURE_DATE'))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })
  })

  describe('when the range or the grouping changes', () => {
    const newStats = statsWithEvents('expensesAdded', 99, 52, [10, 40, 0, 49])

    it('keeps the old cards on the screen, dimmed and marked busy, instead of going back to placeholders', async () => {
      const held = holdAnswerFor(sevenDaysUrl, newStats)
      await renderAdminStats({ statsAnswerFor: held.answerFor })
      expect(totalOf(await findStatsCard('Expenses added'))).toBe('78')

      pressChip('7 days')

      await waitFor(() => {
        expect(cardsContainer().getAttribute('aria-busy')).toBe('true')
      })
      expect(totalOf(screen.getByRole('region', { name: 'Expenses added' }))).toBe('78')
      expect(screen.queryByRole('status')).toBeNull()
      expect(document.querySelector('[data-skeleton]')).toBeNull()
    })

    it('updates the cards and takes the busy mark away when the new answer arrives', async () => {
      const held = holdAnswerFor(sevenDaysUrl, newStats)
      await renderAdminStats({ statsAnswerFor: held.answerFor })
      await findStatsCard('Expenses added')
      pressChip('7 days')
      await waitFor(() => {
        expect(cardsContainer().getAttribute('aria-busy')).toBe('true')
      })

      held.release()

      await waitFor(() => {
        expect(totalOf(screen.getByRole('region', { name: 'Expenses added' }))).toBe('99')
      })
      expect(cardsContainer().getAttribute('aria-busy')).not.toBe('true')
    })

    it('keeps the old cards while a new grouping loads too', async () => {
      const held = holdAnswerFor(weekUrl, newStats)
      await renderAdminStats({ statsAnswerFor: held.answerFor })
      await findStatsCard('Expenses added')

      pressChip('Week')

      await waitFor(() => {
        expect(cardsContainer().getAttribute('aria-busy')).toBe('true')
      })
      expect(totalOf(screen.getByRole('region', { name: 'Expenses added' }))).toBe('78')
      expect(screen.queryByRole('status')).toBeNull()
    })

    it.each(['light', 'dark'] as const)(
      'dims the old cards only so far that their small text keeps 4.5:1 and the bars 3:1 against the card (%s)',
      async (theme) => {
        const held = holdAnswerFor(sevenDaysUrl, newStats)
        await renderAdminStats({ statsAnswerFor: held.answerFor })
        await findStatsCard('Expenses added')

        pressChip('7 days')

        await waitFor(() => {
          expect(cardsContainer().getAttribute('aria-busy')).toBe('true')
        })
        const opacity = opacityOfClasses(classesOf(cardsContainer()))
        expect(opacity).toBeLessThan(1)
        for (const token of smallTextTokensOfTheCards) {
          expect(dimmedContrast(token, opacity, theme), token).toBeGreaterThanOrEqual(smallestTextContrast)
        }
        expect(dimmedContrast('primary', opacity, theme)).toBeGreaterThanOrEqual(smallestBarContrast)
      },
    )

    it('keeps the chips usable while the old cards are dimmed', async () => {
      const held = holdAnswerFor(sevenDaysUrl, newStats)
      await renderAdminStats({ statsAnswerFor: held.answerFor })
      await findStatsCard('Expenses added')

      pressChip('7 days')

      expect(isPressed(chip('7 days'))).toBe(true)
      expect(isPressed(chip('This month'))).toBe(false)
    })

    it('asks only for the new range, once', async () => {
      const held = holdAnswerFor(sevenDaysUrl, newStats)
      const { fetchMock } = await renderAdminStats({ statsAnswerFor: held.answerFor })
      await findStatsCard('Expenses added')

      pressChip('7 days')
      held.release()
      await waitFor(() => {
        expect(totalOf(screen.getByRole('region', { name: 'Expenses added' }))).toBe('99')
      })

      expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl, sevenDaysUrl])
    })
  })

  describe('the look', () => {
    it('uses the wide column on a computer, 64rem, and not the narrow one', async () => {
      await renderAdminStats()
      await findStatsCard('New sign-ups')

      const mainClasses = classesOf(screen.getByRole('main'))

      expect(mainClasses).toContain('lg:max-w-[64rem]')
      expect(mainClasses).not.toContain('lg:max-w-[40rem]')
    })

    it('puts the cards in one column on a phone and in two columns from the lg breakpoint', async () => {
      await renderAdminStats()
      await findStatsCard('New sign-ups')

      const classes = classesOf(cardsContainer())

      expect(classes).toContain('lg:grid-cols-2')
      expect(classes.filter((name) => name.includes('grid-cols-') && name !== 'lg:grid-cols-2' && name !== 'grid-cols-1')).toEqual([])
      expect(classes.some((name) => name === 'grid' || name === 'lg:grid')).toBe(true)
    })

    it('holds all six cards in that one container', async () => {
      await renderAdminStats()
      await findStatsCard('New sign-ups')

      expect(within(cardsContainer()).getAllByRole('region')).toHaveLength(6)
      expect(queryCardsContainer()).toBe(cardsContainer())
    })
  })

  describe('what the screen asks the server', () => {
    it('asks only for the statistics, and for me and the visit mark, never for groups, budgets or names', async () => {
      const { fetchMock } = await renderAdminStats()
      await findStatsCard('New sign-ups')

      const paths = requestedPaths(fetchMock)

      expect(paths.filter((path) => !allowedPaths.includes(path))).toEqual([])
      expect(paths).toContain('/api/admin/stats')
    })
  })

  describe('in Macedonian', () => {
    it('writes the title and every chip in Macedonian', async () => {
      await renderAdminStats({ language: 'mk' })
      await findStatsCard(translated('mk', 'adminStats.card.signUps'))

      expect(await findStatsTitle('mk')).toBeTruthy()
      for (const key of [
        'range.days7',
        'range.days30',
        'range.thisMonth',
        'range.months12',
        'range.all',
        'range.custom',
        'by.day',
        'by.week',
        'by.month',
        'by.year',
      ]) {
        expect(chip(translated('mk', `adminStats.${key}`)), key).toBeTruthy()
      }
    })

    it('opens with This month and Day pressed, as in English', async () => {
      const { fetchMock } = await renderAdminStats({ language: 'mk' })
      await findStatsCard(translated('mk', 'adminStats.card.signUps'))

      expect(isPressed(chip(translated('mk', 'adminStats.range.thisMonth')))).toBe(true)
      expect(isPressed(chip(translated('mk', 'adminStats.by.day')))).toBe(true)
      expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl])
    })

    it('labels the two date fields in Macedonian after Custom is pressed', async () => {
      await renderAdminStats({ language: 'mk' })
      await findStatsCard(translated('mk', 'adminStats.card.signUps'))

      pressChip(translated('mk', 'adminStats.range.custom'))

      expect(screen.getByLabelText(translated('mk', 'adminStats.custom.from'))).toBeTruthy()
      expect(screen.getByLabelText(translated('mk', 'adminStats.custom.to'))).toBeTruthy()
    })
  })
})
