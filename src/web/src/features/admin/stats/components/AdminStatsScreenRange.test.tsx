import { screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { adminStatsNow, defaultStatsUrl } from '@/test/adminStatsTestData'
import {
  chip,
  findStatsCard,
  isDisabledChip,
  isPressed,
  pressChip,
  renderAdminStats,
  setCustomDates,
  statsRequestCount,
  statsRequests,
  statsUrl,
} from '@/test/adminStatsTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testAdminMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const rangeNames = ['7 days', '30 days', 'This month', '12 months', 'All', 'Custom']
const groupingNames = ['Day', 'Week', 'Month', 'Year']

async function openStats(options: Parameters<typeof renderAdminStats>[0] = {}) {
  const rendered = await renderAdminStats(options)
  await findStatsCard('New sign-ups')
  return rendered
}

async function expectRequested(fetchMock: Mock<typeof fetch>, url: string): Promise<void> {
  await waitFor(() => {
    expect(statsRequestCount(fetchMock, url)).toBe(1)
  })
}

function pressedNames(names: string[]): string[] {
  return names.filter((name) => isPressed(chip(name)))
}

describe('the statistics screen, the range and the grouping', () => {
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
  })

  describe('what it opens with', () => {
    it('asks for this month, from the 1st to today, grouped by day, and for nothing else', async () => {
      const { fetchMock } = await openStats()

      expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl])
    })

    it('has This month and Day pressed, and no other range or grouping chip pressed', async () => {
      await openStats()

      expect(pressedNames(rangeNames)).toEqual(['This month'])
      expect(pressedNames(groupingNames)).toEqual(['Day'])
    })

    it('has the six range chips and the four grouping chips as buttons, in this order', async () => {
      await openStats()

      const buttons = screen.getAllByRole('button').map((button) => button.textContent)

      expect(buttons.filter((name) => rangeNames.includes(name ?? ''))).toEqual(rangeNames)
      expect(buttons.filter((name) => groupingNames.includes(name ?? ''))).toEqual(groupingNames)
    })

    it('has the Day chip enabled', async () => {
      await openStats()

      expect(isDisabledChip(chip('Day'))).toBe(false)
    })

    it('shows no date fields until Custom is pressed', async () => {
      await openStats()

      expect(screen.queryByLabelText(translated('en', 'adminStats.custom.from'))).toBeNull()
      expect(screen.queryByLabelText(translated('en', 'adminStats.custom.to'))).toBeNull()
    })
  })

  describe('the day on the phone is the day in Skopje', () => {
    it.each([
      ['a normal afternoon', '2026-11-04T10:00:00Z', '2026-11-01', '2026-11-04'],
      ['half an hour before midnight in UTC, which is already the next day in Skopje', '2026-11-03T23:30:00Z', '2026-11-01', '2026-11-04'],
      ['the first night of a month in Skopje, still the last day of the month in UTC', '2026-10-31T23:30:00Z', '2026-11-01', '2026-11-01'],
      ['the last minute of the year in Skopje, in winter time', '2026-12-31T22:59:00Z', '2026-12-01', '2026-12-31'],
      ['the first minute of the new year in Skopje, in winter time', '2026-12-31T23:00:00Z', '2027-01-01', '2027-01-01'],
      ['the last evening of June in Skopje, in summer time', '2026-06-30T21:30:00Z', '2026-06-01', '2026-06-30'],
      ['the first night of July in Skopje, in summer time', '2026-06-30T22:30:00Z', '2026-07-01', '2026-07-01'],
    ])('asks for this month up to today on %s', async (_when, now, from, to) => {
      vi.setSystemTime(new Date(now))

      const { fetchMock } = await openStats()

      expect(statsRequests(fetchMock)).toEqual([statsUrl({ from, to }, 'day')])
    })

    it('uses the day in Skopje even when the time zone of the person is another one', async () => {
      vi.setSystemTime(new Date('2026-11-03T23:30:00Z'))

      const { fetchMock } = await openStats({ me: { ...testAdminMe, timeZone: 'America/New_York' } })

      expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl])
    })
  })

  describe('the range chips', () => {
    it.each([
      ['7 days', '2026-10-29', '2026-11-04', 'day', 'Day'],
      ['30 days', '2026-10-06', '2026-11-04', 'day', 'Day'],
      ['12 months', '2025-12-01', '2026-11-04', 'month', 'Month'],
    ])('asks for %s from %s to %s grouped by %s, with %s pressed', async (name, from, to, by, groupingName) => {
      const { fetchMock } = await openStats()

      pressChip(name)

      await expectRequested(fetchMock, statsUrl({ from, to }, by))
      expect(pressedNames(rangeNames)).toEqual([name])
      expect(pressedNames(groupingNames)).toEqual([groupingName])
    })

    it('asks for All with neither a start nor an end, grouped by month', async () => {
      const { fetchMock } = await openStats()

      pressChip('All')

      await expectRequested(fetchMock, '/api/admin/stats?by=month')
      expect(pressedNames(rangeNames)).toEqual(['All'])
      expect(pressedNames(groupingNames)).toEqual(['Month'])
    })

    it('goes back to this month, grouped by day, when This month is pressed again', async () => {
      const { fetchMock } = await openStats()
      pressChip('12 months')
      await expectRequested(fetchMock, statsUrl({ from: '2025-12-01', to: '2026-11-04' }, 'month'))

      pressChip('This month')

      await waitFor(() => {
        expect(pressedNames(groupingNames)).toEqual(['Day'])
      })
      expect(pressedNames(rangeNames)).toEqual(['This month'])
      expect(statsRequests(fetchMock).at(-1)).toBe(defaultStatsUrl)
    })

    it.each([
      ['across the end of February', '2026-03-03T10:00:00Z', '7 days', '2026-02-25', '2026-03-03'],
      ['across the end of February', '2026-03-03T10:00:00Z', '30 days', '2026-02-02', '2026-03-03'],
      ['across the end of a leap February', '2028-03-01T10:00:00Z', '7 days', '2028-02-24', '2028-03-01'],
      ['across the end of a leap February', '2028-03-01T10:00:00Z', '30 days', '2028-02-01', '2028-03-01'],
      ['across the new year', '2026-01-03T10:00:00Z', '7 days', '2025-12-28', '2026-01-03'],
      ['in March, 11 months back is April of last year', '2026-03-03T10:00:00Z', '12 months', '2025-04-01', '2026-03-03'],
      ['in January, 11 months back is February of last year', '2026-01-15T10:00:00Z', '12 months', '2025-02-01', '2026-01-15'],
      ['in December, 11 months back is January of the same year', '2026-12-15T10:00:00Z', '12 months', '2026-01-01', '2026-12-15'],
    ])('counts the days back correctly %s: %s, %s', async (_label, now, name, from, to) => {
      vi.setSystemTime(new Date(now))
      const { fetchMock } = await openStats()
      const by = name === '12 months' ? 'month' : 'day'

      pressChip(name)

      await expectRequested(fetchMock, statsUrl({ from, to }, by))
    })

    it('counts 7 days as today and the 6 days before it, and 30 days as today and the 29 days before it', async () => {
      const { fetchMock } = await openStats()

      pressChip('7 days')
      await expectRequested(fetchMock, statsUrl({ from: '2026-10-29', to: '2026-11-04' }, 'day'))
      pressChip('30 days')
      await expectRequested(fetchMock, statsUrl({ from: '2026-10-06', to: '2026-11-04' }, 'day'))
    })
  })

  describe('the grouping chips', () => {
    it.each([
      ['Week', 'week'],
      ['Month', 'month'],
      ['Year', 'year'],
    ])('asks for the same range grouped by %s and marks only %s as pressed', async (name, by) => {
      const { fetchMock } = await openStats()

      pressChip(name)

      await expectRequested(fetchMock, statsUrl({ from: '2026-11-01', to: '2026-11-04' }, by))
      expect(pressedNames(groupingNames)).toEqual([name])
      expect(pressedNames(rangeNames)).toEqual(['This month'])
    })

    it('keeps the range of 12 months when the grouping changes', async () => {
      const { fetchMock } = await openStats()
      pressChip('12 months')
      await expectRequested(fetchMock, statsUrl({ from: '2025-12-01', to: '2026-11-04' }, 'month'))

      pressChip('Week')

      await expectRequested(fetchMock, statsUrl({ from: '2025-12-01', to: '2026-11-04' }, 'week'))
      expect(pressedNames(rangeNames)).toEqual(['12 months'])
    })

    it('keeps All when the grouping changes', async () => {
      const { fetchMock } = await openStats()
      pressChip('All')
      await expectRequested(fetchMock, '/api/admin/stats?by=month')

      pressChip('Year')

      await expectRequested(fetchMock, '/api/admin/stats?by=year')
      expect(pressedNames(rangeNames)).toEqual(['All'])
    })

    it('sends nothing again when the chip that is already pressed is pressed', async () => {
      const { fetchMock } = await openStats()

      pressChip('Day')

      expect(statsRequests(fetchMock)).toEqual([defaultStatsUrl])
      expect(pressedNames(groupingNames)).toEqual(['Day'])
    })

    it.each([
      ['Week', '7 days', 'Day'],
      ['Year', '30 days', 'Day'],
      ['Month', 'This month', 'Day'],
      ['Week', 'All', 'Month'],
      ['Day', '12 months', 'Month'],
    ])('goes back to the usual grouping when the range is picked: %s, then %s gives %s', async (chosenGrouping, range, expectedGrouping) => {
      const { fetchMock } = await openStats()
      pressChip(chosenGrouping)
      await waitFor(() => {
        expect(statsRequests(fetchMock)).toHaveLength(chosenGrouping === 'Day' ? 1 : 2)
      })

      pressChip(range)

      await waitFor(() => {
        expect(pressedNames(groupingNames)).toEqual([expectedGrouping])
      })
    })
  })

  describe('the Day chip for a range longer than 92 days', () => {
    it.each(['12 months', 'All'])('is disabled and not pressed for %s', async (name) => {
      const { fetchMock } = await openStats()
      pressChip(name)
      await waitFor(() => {
        expect(statsRequests(fetchMock)).toHaveLength(2)
      })

      expect(isDisabledChip(chip('Day'))).toBe(true)
      expect(isPressed(chip('Day'))).toBe(false)
    })

    it.each(['7 days', '30 days', 'This month'])('is enabled for %s', async (name) => {
      const { fetchMock } = await openStats()
      pressChip('12 months')
      await waitFor(() => {
        expect(statsRequests(fetchMock)).toHaveLength(2)
      })

      pressChip(name)

      await waitFor(() => {
        expect(isDisabledChip(chip('Day'))).toBe(false)
      })
    })

    it('cannot be selected: pressing it sends no request and keeps the grouping', async () => {
      const { fetchMock } = await openStats()
      pressChip('12 months')
      await expectRequested(fetchMock, statsUrl({ from: '2025-12-01', to: '2026-11-04' }, 'month'))
      const requestsBefore = statsRequests(fetchMock).length

      pressChip('Day')

      expect(statsRequests(fetchMock)).toHaveLength(requestsBefore)
      expect(pressedNames(groupingNames)).toEqual(['Month'])
    })

    it.each(['Week', 'Month', 'Year'])('leaves the %s chip enabled for All', async (name) => {
      await openStats()
      pressChip('All')

      await waitFor(() => {
        expect(isDisabledChip(chip(name))).toBe(false)
      })
    })
  })

  describe('Custom', () => {
    it('presses Custom and shows two date fields called From and To', async () => {
      await openStats()

      pressChip('Custom')

      expect(pressedNames(rangeNames)).toEqual(['Custom'])
      const from = screen.getByLabelText(translated('en', 'adminStats.custom.from'))
      const to = screen.getByLabelText(translated('en', 'adminStats.custom.to'))
      expect(from.getAttribute('type')).toBe('date')
      expect(to.getAttribute('type')).toBe('date')
    })

    it('hides the date fields again when another range is pressed', async () => {
      await openStats()
      pressChip('Custom')

      pressChip('7 days')

      expect(screen.queryByLabelText(translated('en', 'adminStats.custom.from'))).toBeNull()
      expect(screen.queryByLabelText(translated('en', 'adminStats.custom.to'))).toBeNull()
    })

    it('asks for the dates once both are valid, grouped by day for a range of 92 days or less', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates('2026-10-01', '2026-11-04')

      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-04' }, 'day'))
      expect(pressedNames(groupingNames)).toEqual(['Day'])
    })

    it('allows one single day, from and to the same date', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates('2026-11-04', '2026-11-04')

      await expectRequested(fetchMock, statsUrl({ from: '2026-11-04', to: '2026-11-04' }, 'day'))
    })

    it('counts a range of exactly 92 days as short enough for Day', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates('2026-08-05', '2026-11-04')

      await expectRequested(fetchMock, statsUrl({ from: '2026-08-05', to: '2026-11-04' }, 'day'))
      expect(isDisabledChip(chip('Day'))).toBe(false)
    })

    it('groups a range of 93 days by week and turns Day off', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates('2026-08-04', '2026-11-04')

      await expectRequested(fetchMock, statsUrl({ from: '2026-08-04', to: '2026-11-04' }, 'week'))
      expect(pressedNames(groupingNames)).toEqual(['Week'])
      expect(isDisabledChip(chip('Day'))).toBe(true)
    })

    it('groups a long range by week, not by month', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates('2025-01-01', '2026-11-04')

      await expectRequested(fetchMock, statsUrl({ from: '2025-01-01', to: '2026-11-04' }, 'week'))
    })

    it('lets the grouping be changed after the dates are chosen', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')
      setCustomDates('2026-10-01', '2026-11-04')
      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-04' }, 'day'))

      pressChip('Month')

      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-04' }, 'month'))
    })

    it.each([
      ['the start is after the end', '2026-11-04', '2026-10-01'],
      ['the end is after today', '2026-10-01', '2026-11-05'],
      ['the whole range is after today', '2026-11-05', '2026-11-06'],
    ])('sends no request and shows a short message when %s', async (_why, from, to) => {
      const { fetchMock } = await openStats()
      pressChip('Custom')

      setCustomDates(from, to)

      expect(await screen.findByText(translated('en', 'adminStats.custom.invalid'))).toBeTruthy()
      expect(statsRequests(fetchMock)).not.toContain(statsUrl({ from, to }, 'day'))
      expect(statsRequests(fetchMock)).not.toContain(statsUrl({ from, to }, 'week'))
    })

    it('sends no request and shows the message when the end date is cleared', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')
      setCustomDates('2026-10-01', '2026-11-04')
      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-04' }, 'day'))
      const requestsBefore = statsRequests(fetchMock).length

      setCustomDates('2026-10-01', '')

      expect(await screen.findByText(translated('en', 'adminStats.custom.invalid'))).toBeTruthy()
      expect(statsRequests(fetchMock)).toHaveLength(requestsBefore)
    })

    it('shows no message while both dates are valid, and takes it away when an invalid pair is fixed', async () => {
      const { fetchMock } = await openStats()
      pressChip('Custom')
      setCustomDates('2026-10-01', '2026-11-04')
      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-04' }, 'day'))
      expect(screen.queryByText(translated('en', 'adminStats.custom.invalid'))).toBeNull()

      setCustomDates('2026-10-01', '2026-11-05')
      expect(await screen.findByText(translated('en', 'adminStats.custom.invalid'))).toBeTruthy()
      setCustomDates('2026-10-01', '2026-11-03')

      await expectRequested(fetchMock, statsUrl({ from: '2026-10-01', to: '2026-11-03' }, 'day'))
      expect(screen.queryByText(translated('en', 'adminStats.custom.invalid'))).toBeNull()
    })
  })
})
