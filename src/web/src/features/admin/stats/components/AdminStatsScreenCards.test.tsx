import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import {
  adminStatsNow,
  allTimeStats,
  novemberStats,
  statsWith,
  statsWithActivePeople,
  statsWithDailyBuckets,
  statsWithEvents,
  statsWithSignUps,
  weeklyStats,
} from '@/test/adminStatsTestData'
import {
  allTimeLine,
  answerByGrouping,
  answerWith,
  axisLabelsOf,
  barContainerOf,
  barPercentsOf,
  barsOf,
  cardRegionsTitles,
  cardTitlesInOrder,
  cardsContainer,
  compareOf,
  findStatsCard,
  lineText,
  normalizedLine,
  pressChip,
  renderAdminStats,
  totalOf,
  type StatsAnswerFor,
} from '@/test/adminStatsTestHelpers'
import { classesOf } from '@/test/budgetLeftCardTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import type { AdminStats } from '@/core/services/adminStats/adminStatsService'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const signUpsTitle = 'New sign-ups'
const activeTitle = 'Active people'

const barBoxWidthOnA320PxPhone = 240

const pixelsOfSpacingClass: Record<string, number> = {
  'gap-0': 0,
  'gap-px': 1,
  'gap-0.5': 2,
  'gap-1': 4,
  'min-w-0': 0,
  'min-w-px': 1,
  'min-w-0.5': 2,
  'min-w-1': 4,
}

function pixelsOf(classes: readonly string[], prefix: 'gap-' | 'min-w-'): number {
  const spacing = classes.filter((name) => name.startsWith(prefix))
  if (spacing.length > 1) {
    throw new Error(`Expected at most one ${prefix} class, got ${spacing.join(' ')}`)
  }
  if (spacing.length === 0) {
    return 0
  }
  const pixels = pixelsOfSpacingClass[spacing[0]]
  if (pixels === undefined) {
    throw new Error(`The test does not know how wide ${spacing[0]} is`)
  }
  return pixels
}

async function openStats(stats: AdminStats | StatsAnswerFor = novemberStats, language: Language = 'en') {
  const statsAnswerFor = typeof stats === 'function' ? stats : answerWith(stats)
  const rendered = await renderAdminStats({ language, statsAnswerFor })
  await findStatsCard(translated(language, 'adminStats.card.signUps'))
  return rendered
}

type AllTimeCounts = AdminStats['allTime']

const pluralFormsOfTheCounts: Record<Language, Record<keyof AllTimeCounts, 'one' | 'other'>> = {
  en: { accounts: 'other', guests: 'other', groups: 'other', expenses: 'other', payments: 'other' },
  mk: { accounts: 'one', guests: 'other', groups: 'other', expenses: 'one', payments: 'other' },
}

const countNouns: Record<keyof AllTimeCounts, string> = {
  accounts: 'account',
  guests: 'guest',
  groups: 'group',
  expenses: 'expense',
  payments: 'payment',
}

function allTimeSentence(language: Language, counts: AllTimeCounts): string {
  const countTexts = Object.fromEntries(
    (Object.keys(countNouns) as (keyof AllTimeCounts)[]).map((name) => [
      name,
      translated(language, `adminStats.count.${countNouns[name]}_${pluralFormsOfTheCounts[language][name]}`, {
        count: String(counts[name]),
      }),
    ]),
  )
  return translated(language, 'adminStats.allTime.line', countTexts)
}

describe('the statistics screen, the cards', () => {
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

  describe('the line with the totals since the start', () => {
    it('reads "Since the start: 58 accounts · 6 guests · 14 groups · 412 expenses · 96 payments"', async () => {
      await openStats()

      expect(lineText(allTimeLine())).toBe('Since the start: 58 accounts · 6 guests · 14 groups · 412 expenses · 96 payments')
    })

    it('uses the singular for one of each', async () => {
      await openStats(statsWith({ allTime: { accounts: 1, guests: 1, groups: 1, expenses: 1, payments: 1 } }))

      expect(lineText(allTimeLine())).toBe('Since the start: 1 account · 1 guest · 1 group · 1 expense · 1 payment')
    })

    it('uses the plural for none', async () => {
      await openStats(statsWith({ allTime: { accounts: 0, guests: 0, groups: 0, expenses: 0, payments: 0 } }))

      expect(lineText(allTimeLine())).toBe('Since the start: 0 accounts · 0 guests · 0 groups · 0 expenses · 0 payments')
    })

    it('uses the plural for 2 and 11 and the singular only for exactly 1 in English', async () => {
      await openStats(statsWith({ allTime: { accounts: 2, guests: 11, groups: 21, expenses: 101, payments: 1 } }))

      expect(lineText(allTimeLine())).toBe('Since the start: 2 accounts · 11 guests · 21 groups · 101 expenses · 1 payment')
    })

    it.each(languages)('is written with the plural form of the app for the counts 21, 11, 2, 101 and 111 (%s)', async (language) => {
      const counts = { accounts: 21, guests: 11, groups: 2, expenses: 101, payments: 111 }
      await openStats(statsWith({ allTime: counts }), language)

      expect(lineText(allTimeLine())).toBe(normalizedLine(allTimeSentence(language, counts)))
    })

    it('sits above the cards', async () => {
      await openStats()

      expect(isBefore(allTimeLine(), cardsContainer())).toBe(true)
    })
  })

  describe('the six cards', () => {
    it('shows New sign-ups, Active people, Joined via invite, Groups created, Expenses added and Payments confirmed, in this order', async () => {
      await openStats()

      expect(cardTitlesInOrder()).toEqual([
        'New sign-ups',
        'Active people',
        'Joined via invite',
        'Groups created',
        'Expenses added',
        'Payments confirmed',
      ])
    })

    it.each(languages)('names the cards in the language of the person (%s)', async (language) => {
      await openStats(novemberStats, language)

      expect(cardTitlesInOrder()).toEqual(cardRegionsTitles(language))
    })

    it('gives every card a heading of level 2 as its name', async () => {
      await openStats()

      for (const title of cardTitlesInOrder()) {
        expect(within(await findStatsCard(title)).getByRole('heading', { level: 2, name: title })).toBeTruthy()
      }
    })

    it.each([
      [signUpsTitle, '9'],
      [activeTitle, '21'],
      ['Joined via invite', '8'],
      ['Groups created', '3'],
      ['Expenses added', '78'],
      ['Payments confirmed', '12'],
    ])('shows the total of %s as the big number %s', async (title, total) => {
      await openStats()

      expect(totalOf(await findStatsCard(title))).toBe(total)
    })

    it('shows a zero total as 0', async () => {
      await openStats(statsWithEvents('groupsCreated', 0, 0, [0, 0, 0, 0]))

      expect(totalOf(await findStatsCard('Groups created'))).toBe('0')
    })

    it.each([
      [999, '999'],
      [1000, '1,000'],
      [1234, '1,234'],
      [1234567, '1,234,567'],
    ])('separates the thousands of %i with commas in English: %s', async (total, shown) => {
      await openStats(statsWithEvents('expensesAdded', total, null, [total, 0, 0, 0]))

      expect(totalOf(await findStatsCard('Expenses added'))).toBe(shown)
    })

    it.each([
      [999, '999'],
      [1234, '1.234'],
      [1234567, '1.234.567'],
    ])('separates the thousands of %i with dots in Macedonian: %s', async (total, shown) => {
      await openStats(statsWithEvents('expensesAdded', total, null, [total, 0, 0, 0]), 'mk')

      expect(totalOf(await findStatsCard(translated('mk', 'adminStats.card.expensesAdded')))).toBe(shown)
    })

    it('separates the thousands of the sign-ups and of the active people too', async () => {
      await openStats(
        statsWith({
          signUps: { ...novemberStats.signUps, total: 12345 },
          activePeople: { ...novemberStats.activePeople, people: 4321 },
        }),
      )

      expect(totalOf(await findStatsCard(signUpsTitle))).toBe('12,345')
      expect(totalOf(await findStatsCard(activeTitle))).toBe('4,321')
    })
  })

  describe('New sign-ups', () => {
    it('shows how the people signed up, by email, by Google and as guests', async () => {
      await openStats()
      const card = await findStatsCard(signUpsTitle)

      expect(within(card).getByText('Email 4 · Google 3 · Guest 2')).toBeTruthy()
    })

    it('shows that line with zeros too', async () => {
      await openStats(statsWithSignUps({ total: 0, email: 0, google: 0, guest: 0, guestsUpgraded: 0 }))
      const card = await findStatsCard(signUpsTitle)

      expect(within(card).getByText('Email 0 · Google 0 · Guest 0')).toBeTruthy()
    })

    it('shows how many guests made an account when there are some', async () => {
      await openStats()
      const card = await findStatsCard(signUpsTitle)

      expect(within(card).getByText('Guests who made an account: 1')).toBeTruthy()
    })

    it('leaves that line out when no guest made an account', async () => {
      await openStats(statsWithSignUps({ guestsUpgraded: 0 }))
      const card = await findStatsCard(signUpsTitle)

      expect(within(card).queryByText(/Guests who made an account/)).toBeNull()
    })

    it.each(languages)('writes both lines in the language of the person (%s)', async (language) => {
      await openStats(novemberStats, language)
      const card = await findStatsCard(translated(language, 'adminStats.card.signUps'))

      expect(
        within(card).getByText(translated(language, 'adminStats.signUps.breakdown', { email: '4', google: '3', guest: '2' })),
      ).toBeTruthy()
      expect(within(card).getByText(translated(language, 'adminStats.signUps.upgraded', { count: '1' }))).toBeTruthy()
    })

    it('does not show these lines on the other cards', async () => {
      await openStats()

      for (const title of ['Joined via invite', 'Groups created', 'Expenses added', 'Payments confirmed']) {
        const card = await findStatsCard(title)
        expect(within(card).queryByText(/Email|Google|Guest/)).toBeNull()
      }
    })
  })

  describe('Active people', () => {
    it('shows the number of different people as the big number, not the person-days', async () => {
      await openStats(weeklyStats)

      expect(totalOf(await findStatsCard(activeTitle))).toBe('21')
    })

    it('shows how many of them are guests when there are some', async () => {
      await openStats()
      const card = await findStatsCard(activeTitle)

      expect(within(card).getByText('of them guests 4')).toBeTruthy()
    })

    it('leaves that line out when no guest was active', async () => {
      await openStats(statsWithActivePeople({ guests: 0 }))
      const card = await findStatsCard(activeTitle)

      expect(within(card).queryByText(/of them guests/)).toBeNull()
    })

    it('shows no person-days line when grouped by day, because there the people and the person-days are the same', async () => {
      await openStats()
      const card = await findStatsCard(activeTitle)

      expect(within(card).queryByText(/person-days/)).toBeNull()
    })

    it.each(['Week', 'Month', 'Year'])('shows the person-days line when grouped by %s', async (name) => {
      await openStats(answerByGrouping)

      pressChip(name)

      await waitFor(() => {
        expect(within(screen.getByRole('region', { name: activeTitle })).getByText('59 person-days')).toBeTruthy()
      })
    })

    it('takes the person-days line away again after going back to Day', async () => {
      await openStats(answerByGrouping)
      pressChip('Week')
      await waitFor(() => {
        expect(within(screen.getByRole('region', { name: activeTitle })).getByText('59 person-days')).toBeTruthy()
      })

      pressChip('Day')

      await waitFor(() => {
        expect(within(screen.getByRole('region', { name: activeTitle })).queryByText(/person-days/)).toBeNull()
      })
    })

    it.each(languages)('writes the lines in the language of the person (%s)', async (language) => {
      await openStats(answerByGrouping, language)
      pressChip(translated(language, 'adminStats.by.week'))

      await waitFor(() => {
        const card = screen.getByRole('region', { name: translated(language, 'adminStats.card.activePeople') })
        expect(within(card).getByText(translated(language, 'adminStats.active.guests', { count: '4' }))).toBeTruthy()
        expect(within(card).getByText(translated(language, 'adminStats.active.personDays', { count: '59' }))).toBeTruthy()
      })
    })
  })

  describe('the line that compares with the period before, from 1000 per cent up', () => {
    it.each([
      ['en', '1,043'],
      ['mk', '1.043'],
    ] as const)('separates the thousands of the per cent as the counts are separated (%s: %s)', async (language, percent) => {
      await openStats(statsWithEvents('expensesAdded', 1143, 100, [1143, 0, 0, 0]), language)

      const card = await findStatsCard(translated(language, 'adminStats.card.expensesAdded'))

      expect(compareOf(card)).toBe(normalizedLine(translated(language, 'adminStats.compare.up', { percent })))
    })

    it('writes "+1,043% vs the period before" in English', async () => {
      await openStats(statsWithEvents('expensesAdded', 1143, 100, [1143, 0, 0, 0]))

      expect(compareOf(await findStatsCard('Expenses added'))).toBe('+1,043% vs the period before')
    })
  })

  describe('the line that compares with the period before', () => {
    const cases: [number, number | null, string | null][] = [
      [78, 52, '+50% vs the period before'],
      [8, 10, '-20% vs the period before'],
      [12, 12, 'same as the period before'],
      [3, 0, 'new, none before'],
      [0, 0, null],
      [4, 3, '+33% vs the period before'],
      [2, 3, '-33% vs the period before'],
      [200, 100, '+100% vs the period before'],
      [0, 5, '-100% vs the period before'],
      [5, null, null],
      [0, null, null],
    ]

    it.each(cases)('says %i against %s before: %s, on a card of events', async (total, previousTotal, line) => {
      await openStats(statsWithEvents('groupsCreated', total, previousTotal, [total, 0, 0, 0]))

      expect(compareOf(await findStatsCard('Groups created'))).toBe(line)
    })

    it.each(cases)('says %i against %s before: %s, on New sign-ups', async (total, previousTotal, line) => {
      await openStats(statsWithSignUps({ total, previousTotal, counts: [total, 0, 0, 0] }))

      expect(compareOf(await findStatsCard(signUpsTitle))).toBe(line)
    })

    it.each(cases)('says %i against %s before: %s, on Active people, by the people and not the person-days', async (people, previousPeople, line) => {
      await openStats(statsWithActivePeople({ people, previousPeople, personDays: 500, previousPersonDays: 1000 }))

      expect(compareOf(await findStatsCard(activeTitle))).toBe(line)
    })

    it('compares every card with its own period before', async () => {
      await openStats()

      expect(compareOf(await findStatsCard(signUpsTitle))).toBe('+50% vs the period before')
      expect(compareOf(await findStatsCard(activeTitle))).toBe('same as the period before')
      expect(compareOf(await findStatsCard('Joined via invite'))).toBe('-20% vs the period before')
      expect(compareOf(await findStatsCard('Groups created'))).toBe('new, none before')
      expect(compareOf(await findStatsCard('Expenses added'))).toBe('+50% vs the period before')
      expect(compareOf(await findStatsCard('Payments confirmed'))).toBe('same as the period before')
    })

    it('shows no comparison on any card for All, where there is no period before', async () => {
      await openStats(allTimeStats)

      for (const title of cardTitlesInOrder()) {
        expect(compareOf(await findStatsCard(title)), title).toBeNull()
      }
    })

    it.each(languages)('writes all four sentences in the language of the person (%s)', async (language) => {
      const stats = statsWith({
        signUps: { ...novemberStats.signUps, total: 9, previousTotal: 6 },
        activePeople: { ...novemberStats.activePeople, people: 8, previousPeople: 10 },
        groupsCreated: { total: 3, previousTotal: 0, counts: [0, 1, 2, 0] },
        paymentsConfirmed: { total: 12, previousTotal: 12, counts: [3, 3, 3, 3] },
      })
      await openStats(stats, language)

      const compareLine = async (key: string) => compareOf(await findStatsCard(translated(language, key)))
      expect(await compareLine('adminStats.card.signUps')).toBe(normalizedLine(translated(language, 'adminStats.compare.up', { percent: '50' })))
      expect(await compareLine('adminStats.card.activePeople')).toBe(normalizedLine(translated(language, 'adminStats.compare.down', { percent: '20' })))
      expect(await compareLine('adminStats.card.groupsCreated')).toBe(normalizedLine(translated(language, 'adminStats.compare.new')))
      expect(await compareLine('adminStats.card.paymentsConfirmed')).toBe(normalizedLine(translated(language, 'adminStats.compare.same')))
    })
  })

  describe('the bars', () => {
    it('draws one bar for each bucket on every card, in a container hidden from screen readers', async () => {
      await openStats()

      for (const title of cardTitlesInOrder()) {
        const card = await findStatsCard(title)
        expect(barContainerOf(card).getAttribute('aria-hidden'), title).toBe('true')
        expect(barsOf(card), title).toHaveLength(novemberStats.buckets.length)
      }
    })

    it('sizes each bar by its bucket against the largest bucket of the same card', async () => {
      await openStats()

      const heights = barPercentsOf(await findStatsCard('Expenses added'))

      expect(heights[0]).toBeCloseTo(25, 5)
      expect(heights[1]).toBeCloseTo(100, 5)
      expect(heights[3]).toBeCloseTo(70, 5)
    })

    it('scales every card by its own largest bucket', async () => {
      await openStats()

      const signUps = barPercentsOf(await findStatsCard(signUpsTitle))
      const payments = barPercentsOf(await findStatsCard('Payments confirmed'))

      expect(signUps[0]).toBeCloseTo(50, 5)
      expect(signUps[1]).toBeCloseTo(75, 5)
      expect(signUps[3]).toBeCloseTo(100, 5)
      expect(payments).toEqual([100, 100, 100, 100])
    })

    it('draws a bucket with nothing in it as a minimal bar, not as no bar', async () => {
      await openStats()

      const emptyBucket = barPercentsOf(await findStatsCard('Expenses added'))[2]

      expect(emptyBucket).toBeGreaterThan(0)
      expect(emptyBucket).toBeLessThanOrEqual(10)
    })

    it('draws only minimal bars, with a real height, on a card where every bucket is empty', async () => {
      await openStats(statsWithEvents('paymentsConfirmed', 0, 0, [0, 0, 0, 0]))

      const heights = barPercentsOf(await findStatsCard('Payments confirmed'))

      expect(heights).toHaveLength(4)
      for (const height of heights) {
        expect(height).toBeGreaterThan(0)
        expect(height).toBeLessThanOrEqual(10)
      }
    })

    it('sets the height as a percentage in the style of the bar', async () => {
      await openStats()

      for (const bar of barsOf(await findStatsCard('Expenses added'))) {
        expect(bar.style.height).toMatch(/^\d+(\.\d+)?%$/)
      }
    })

    it('draws the bars of Active people from the different people of each bucket, as the big number counts them', async () => {
      await openStats(statsWithActivePeople({ peopleCounts: [5, 10, 0, 10], personDayCounts: [7, 7, 7, 7] }))

      const heights = barPercentsOf(await findStatsCard(activeTitle))

      expect(heights[0]).toBeCloseTo(50, 5)
      expect(heights[1]).toBeCloseTo(100, 5)
      expect(heights[3]).toBeCloseTo(100, 5)
    })

    it('draws one bar for each bucket of a grouping with other buckets, such as the 2 weeks', async () => {
      await openStats(answerByGrouping)
      pressChip('Week')

      await waitFor(() => {
        expect(barsOf(screen.getByRole('region', { name: signUpsTitle }))).toHaveLength(weeklyStats.buckets.length)
      })
    })

    it('does not animate the bars unless the animation is turned off for people who want less motion', async () => {
      await openStats()

      for (const title of cardTitlesInOrder()) {
        for (const bar of barsOf(await findStatsCard(title))) {
          const classes = classesOf(bar)
          if (classes.some((name) => name.startsWith('transition'))) {
            expect(classes, title).toContain('motion-reduce:transition-none')
          }
          if (classes.some((name) => name.startsWith('animate-'))) {
            expect(classes, title).toContain('motion-reduce:animate-none')
          }
        }
      }
    })
  })

  describe('many bars on a narrow phone', () => {
    it.each([31, 62, 92, 100])('fits %i bars into the bar box of a 320 px phone on every card, each bar at least 1 px wide', async (count) => {
      await openStats(statsWithDailyBuckets(count))

      for (const title of cardTitlesInOrder()) {
        const card = await findStatsCard(title)
        const bars = barsOf(card)
        const gap = pixelsOf(classesOf(barContainerOf(card)), 'gap-')
        const narrowestRow = bars.reduce((width, bar) => width + pixelsOf(classesOf(bar), 'min-w-'), gap * (bars.length - 1))
        const widthOfEachBar = (barBoxWidthOnA320PxPhone - gap * (bars.length - 1)) / bars.length

        expect(bars, title).toHaveLength(count)
        expect(narrowestRow, title).toBeLessThanOrEqual(barBoxWidthOnA320PxPhone)
        expect(widthOfEachBar, title).toBeGreaterThanOrEqual(1)
        for (const bar of bars) {
          expect(classesOf(bar), title).toContain('flex-1')
        }
      }
    })

    it('still draws every bucket with nothing in it as a minimal bar among 92 bars', async () => {
      await openStats(statsWithDailyBuckets(92))

      const heights = barPercentsOf(await findStatsCard('Expenses added'))

      expect(heights).toHaveLength(92)
      for (const height of heights) {
        expect(height).toBeGreaterThan(0)
      }
    })
  })

  describe('the first and the last bucket under the bars', () => {
    it.each([
      ['Day', ['1 Nov', '4 Nov']],
      ['Week', ['26 Oct', '2 Nov']],
      ['Month', ['Oct 2026', 'Nov 2026']],
      ['Year', ['2025', '2026']],
    ])('writes the grouping %s in English as %j, on every card', async (name, labels) => {
      await openStats(answerByGrouping)
      pressChip(name)

      await waitFor(() => {
        for (const title of cardTitlesInOrder()) {
          expect(axisLabelsOf(screen.getByRole('region', { name: title })), title).toEqual(labels)
        }
      })
    })

    it.each([
      ['day', ['1 ное', '4 ное']],
      ['week', ['26 окт', '2 ное']],
      ['month', ['окт 2026', 'ное 2026']],
      ['year', ['2025', '2026']],
    ])('writes the grouping %s in Macedonian as %j, on every card', async (by, labels) => {
      await openStats(answerByGrouping, 'mk')
      pressChip(translated('mk', `adminStats.by.${by}`))

      await waitFor(() => {
        for (const title of cardRegionsTitles('mk')) {
          expect(axisLabelsOf(screen.getByRole('region', { name: title })), title).toEqual(labels)
        }
      })
    })

    it('shows the day of the week that starts a week, the Monday, and not the day the range starts on', async () => {
      await openStats(answerByGrouping)
      pressChip('Week')

      await waitFor(() => {
        expect(axisLabelsOf(screen.getByRole('region', { name: signUpsTitle }))[0]).toBe('26 Oct')
      })
    })
  })

  describe('what is never on the screen', () => {
    it('shows no name, email, amount or currency of anyone', async () => {
      await openStats()

      const text = lineText(screen.getByRole('main'))

      expect(text).not.toContain('filip@example.com')
      expect(text).not.toContain('Filip')
      expect(text).not.toMatch(/MKD|EUR|€|ден/)
    })

    it('shows no empty value written out as text', async () => {
      await openStats(allTimeStats)

      expect(lineText(screen.getByRole('main'))).not.toMatch(/NaN|undefined|null|Infinity/)
    })
  })
})
