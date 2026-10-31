import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import { money, classesOf } from '@/test/budgetLeftCardTestHelpers'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { freezeTime, isBefore, unfreezeTime } from '@/test/expenseTestHelpers'
import {
  eurPlanOfSixHundred,
  groupWithEurPlan,
  groupWithMkdPlan,
  groupWithPlan,
  mkdPlanOfTwoThousand,
  planOf,
  spendingOfPlanSpent,
  spendingWithPlanOf,
  tenThousandDenarsMinor,
} from '@/test/groupPlanTestData'
import {
  findPlanCard,
  includedLine,
  lineWithText,
  nearLine,
  ofAmountLine,
  onTrackLine,
  overLine,
  planBarOf,
  planCard,
  planControlOf,
  planHeading,
  planThatChanges,
  queryPlanCard,
  querySetPlanRow,
  requireLine,
  setPlanRow,
} from '@/test/groupPlanTestHelpers'
import { mkdOnlySpending, noSpendingYet, spendingPath } from '@/test/groupSpendingTestData'
import { findCurrencyRegion, queryCurrencyRegion } from '@/test/groupSpendingTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { barCases, barCasesInCents, barLimitMinor, expectBarMoves, expectBarShows } from '@/test/progressBarTestHelpers'
import { jsonAnswer, neverAnswer } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { testGuestMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const serverError = () => new Response(null, { status: 500 })
const mkdPlanFields = { planAmountMinor: 200000, planCurrency: 'MKD' } as const

const closingGroupWithPlan: Group = { ...closingGroup, ...mkdPlanFields }
const finishedGroupWithPlan: Group = { ...finishedGroup, ...mkdPlanFields }
const guestAnswers = { 'GET /api/me': jsonAnswer(testGuestMe) }

function renderTotals(options: GroupAppOptions = {}) {
  return renderGroupApp(routes.groupTotals(groupId), { group: groupWithMkdPlan, spending: spendingWithPlanOf(mkdPlanOfTwoThousand), ...options })
}

async function planCardOf(options: GroupAppOptions = {}): Promise<HTMLElement> {
  await renderTotals(options)
  return findPlanCard(options.language)
}

describe('GroupScreen Totals tab, the spending plan card', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('what it shows', () => {
    it('is a region with a visible heading "Spending plan"', async () => {
      const card = await planCardOf()

      const heading = within(card).getByRole('heading', { name: 'Spending plan' })

      expect(heading.classList.contains('sr-only')).toBe(false)
    })

    it('is a region with the heading "План за трошоците" in Macedonian', async () => {
      const card = await planCardOf({ language: 'mk' })

      expect(within(card).getByRole('heading', { name: 'План за трошоците' })).toBeTruthy()
    })

    it('says how much of the plan is spent: "1,350 MKD of 2,000 MKD"', async () => {
      const card = await planCardOf()

      expect(requireLine(card, '1,350 MKD of 2,000 MKD')).toBeTruthy()
    })

    it('says it in euros for a plan in euros: "€150.00 of €600.00"', async () => {
      const card = await planCardOf({ group: groupWithEurPlan, spending: spendingWithPlanOf(eurPlanOfSixHundred) })

      expect(requireLine(card, '€150.00 of €600.00')).toBeTruthy()
    })

    it('says it in Macedonian with the denar written as ден.', async () => {
      const card = await planCardOf({ language: 'mk' })

      expect(requireLine(card, `${money(135000, 'MKD', 'mk')} од ${money(200000, 'MKD', 'mk')}`)).toBeTruthy()
      expect(card.textContent).toContain('ден.')
    })

    it('draws both amounts of that line with KvitAmount: a text for screen readers and a rolling number hidden from them', async () => {
      const card = await planCardOf()

      const line = requireLine(card, ofAmountLine(135000, 200000, 'MKD'))

      expect(line.querySelectorAll('.sr-only')).toHaveLength(2)
      expect(line.querySelectorAll('[aria-hidden="true"]').length).toBeGreaterThanOrEqual(2)
    })

    it('shows the plan amounts without deni for a plan in denars', async () => {
      const card = await planCardOf({ spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, spentMinor: 100 }) })

      expect(requireLine(card, '1 MKD of 2,000 MKD')).toBeTruthy()
    })

    it.each(languages)('has one progress bar named like the card (%s)', async (language) => {
      const card = await planCardOf({ language })

      expect(within(card).getAllByRole('progressbar')).toHaveLength(1)
      expect(planBarOf(card, language).getAttribute('aria-label')).toBe(planHeading(language))
    })

    it('uses the plan in the answer, so the card shows although the group has no plan fields yet', async () => {
      const card = await planCardOf({ group: testGroup })

      expect(requireLine(card, ofAmountLine(135000, 200000, 'MKD'))).toBeTruthy()
    })
  })

  describe('the line "N each · M people"', () => {
    it('says "1 person" for a plan of one member and uses the amount of the answer', async () => {
      const card = await planCardOf({
        group: groupOf({ ...mkdPlanFields, memberCount: 9 }),
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, perPersonMinor: 200000, memberCount: 1 }),
      })

      expect(requireLine(card, '2,000 MKD each · 1 person')).toBeTruthy()
    })

    it('says "3 people" for a plan of three, with the share the server worked out and no sum of its own', async () => {
      const card = await planCardOf({
        group: groupOf({ planAmountMinor: tenThousandDenarsMinor, planCurrency: 'MKD', memberCount: 8 }),
        spending: spendingWithPlanOf({ perPersonMinor: 333300, memberCount: 3 }),
      })

      expect(requireLine(card, '3,333 MKD each · 3 people')).toBeTruthy()
    })

    it('says it in euros for a plan in euros', async () => {
      const card = await planCardOf({
        group: groupOf({ planAmountMinor: 60000, planCurrency: 'EUR', memberCount: 2 }),
        spending: spendingWithPlanOf({ ...eurPlanOfSixHundred, perPersonMinor: 15000, memberCount: 4 }),
      })

      expect(requireLine(card, '€150.00 each · 4 people')).toBeTruthy()
    })

    it('counts the people of the plan in the answer, not the member count of the group', async () => {
      const card = await planCardOf({
        group: groupOf({ ...mkdPlanFields, memberCount: 7 }),
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, memberCount: 3 }),
      })

      expect(requireLine(card, '2,000 MKD each · 3 people')).toBeTruthy()
      expect(card.textContent).not.toContain('7 people')
    })

    it('shows the people of the plan in the answer although the group has no plan fields yet', async () => {
      const card = await planCardOf({
        group: groupOf({ memberCount: 6 }),
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, memberCount: 2 }),
      })

      expect(requireLine(card, '2,000 MKD each · 2 people')).toBeTruthy()
    })

    it.each([
      [1, '1 person'],
      [2, '2 people'],
      [5, '5 people'],
      [11, '11 people'],
      [21, '21 people'],
    ])('writes the English plural for %i people', async (count, words) => {
      const card = await planCardOf({
        group: groupOf({ ...mkdPlanFields, memberCount: count + 1 }),
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, memberCount: count }),
      })

      expect(requireLine(card, `2,000 MKD each · ${words}`)).toBeTruthy()
    })

    it.each([
      [1, 'лице'],
      [2, 'лица'],
      [5, 'лица'],
      [11, 'лица'],
      [21, 'лице'],
      [101, 'лице'],
    ])('writes the Macedonian plural for %i people like the group size line', async (count, word) => {
      const card = await planCardOf({
        language: 'mk',
        group: groupOf({ ...mkdPlanFields, memberCount: count + 1 }),
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, memberCount: count }),
      })

      expect(requireLine(card, `${money(200000, 'MKD', 'mk')} по човек · ${count} ${word}`)).toBeTruthy()
    })
  })

  describe('the state line', () => {
    it.each(barCases)('shows the bar of $label in the state "$state" and the matching words', async ({ spentMinor, state }) => {
      const card = await planCardOf({ group: groupWithPlan(planOf()), spending: spendingOfPlanSpent(spentMinor) })

      expectBarShows(planBarOf(card), spentMinor, barLimitMinor, state)
      expectStateWords(card, state, spentMinor, barLimitMinor, 'MKD')
    })

    it.each(barCasesInCents)('shows the bar of a plan in euros with $label in the state "$state" and the matching words', async ({ spentMinor, state }) => {
      const card = await planCardOf({
        group: groupWithPlan({ amountMinor: barLimitMinor, currency: 'EUR' }),
        spending: spendingOfPlanSpent(spentMinor, barLimitMinor, 'EUR'),
      })

      expectBarShows(planBarOf(card), spentMinor, barLimitMinor, state)
      expectStateWords(card, state, spentMinor, barLimitMinor, 'EUR')
    })

    it('says "On track" for a plan that is under 80 %', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(500000) })

      expect(requireLine(card, 'On track')).toBeTruthy()
    })

    it('says "Near the plan · 1,000 MKD left" at 90 % of a plan of 10,000 MKD', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(900000) })

      expect(requireLine(card, 'Near the plan · 1,000 MKD left')).toBeTruthy()
    })

    it('says "Near the plan · 0 MKD left" when the plan is exactly reached', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(1000000) })

      expect(requireLine(card, 'Near the plan · 0 MKD left')).toBeTruthy()
    })

    it('says "2,000 MKD over" in red at 120 % of a plan of 10,000 MKD, taking the amount from the answer', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(1200000) })

      const line = requireLine(card, '2,000 MKD over')

      expect(classesOf(line)).toContain('text-negative')
    })

    it('does not write the "over" line in red when the plan is only near', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(900000) })

      expect(classesOf(requireLine(card, nearLine(100000, 'MKD')))).not.toContain('text-negative')
    })

    it.each(languages)('writes the state words in the language of the app (%s)', async (language) => {
      const card = await planCardOf({ language, spending: spendingOfPlanSpent(1200000) })

      expect(requireLine(card, overLine(200000, 'MKD', language))).toBeTruthy()
    })

    it('writes the Macedonian words for a plan that is on track', async () => {
      const card = await planCardOf({ language: 'mk', spending: spendingOfPlanSpent(500000) })

      expect(requireLine(card, 'Во рамки')).toBeTruthy()
    })

    it('writes the Macedonian words for a plan that is near', async () => {
      const card = await planCardOf({ language: 'mk', spending: spendingOfPlanSpent(900000) })

      expect(requireLine(card, `Близу до планот · уште ${money(100000, 'MKD', 'mk')}`)).toBeTruthy()
    })

    it('writes the Macedonian words for a plan that is over', async () => {
      const card = await planCardOf({ language: 'mk', spending: spendingOfPlanSpent(1200000) })

      expect(requireLine(card, `${money(200000, 'MKD', 'mk')} над планот`)).toBeTruthy()
    })

    it('draws the bar like the Limits bar: from the left edge, only the transform animated, off under reduced motion', async () => {
      const card = await planCardOf({ spending: spendingOfPlanSpent(900000) })

      expectBarMoves(planBarOf(card))
    })
  })

  describe('the note about converted money', () => {
    it('says how many euros were converted into denars for a plan in denars', async () => {
      const card = await planCardOf({ spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, includedOtherCurrencyMinor: 2000 }) })

      expect(requireLine(card, 'includes €20.00 converted to MKD')).toBeTruthy()
    })

    it('says how many denars were converted into euros for a plan in euros', async () => {
      const card = await planCardOf({
        group: groupWithEurPlan,
        spending: spendingWithPlanOf({ ...eurPlanOfSixHundred, includedOtherCurrencyMinor: 123000 }),
      })

      expect(requireLine(card, 'includes 1,230 MKD converted to EUR')).toBeTruthy()
    })

    it('writes the plan currency in Macedonian as ден. and the converted euros as euros', async () => {
      const card = await planCardOf({
        language: 'mk',
        spending: spendingWithPlanOf({ ...mkdPlanOfTwoThousand, includedOtherCurrencyMinor: 2000 }),
      })

      expect(requireLine(card, includedLine(2000, 'EUR', 'MKD', 'mk'))).toBeTruthy()
      expect(requireLine(card, includedLine(2000, 'EUR', 'MKD', 'mk')).textContent).toContain('ден.')
    })

    it('shows no note when nothing was converted', async () => {
      const card = await planCardOf()

      expect(card.textContent).not.toContain('converted')
      expect(lineWithText(card, includedLine(0, 'EUR', 'MKD'))).toBeNull()
    })
  })

  describe('who can tap it', () => {
    it('is one real button for the owner of an open group, holding the heading, the amounts and the bar, at least 44 px high', async () => {
      const card = await planCardOf()
      const control = planControlOf(card)

      expect(control?.tagName).toBe('BUTTON')
      expect(control?.getAttribute('type')).toBe('button')
      expect(control?.contains(within(card).getByRole('heading', { name: 'Spending plan' }))).toBe(true)
      expect(control?.contains(planBarOf(card))).toBe(true)
      expect(control?.contains(requireLine(card, ofAmountLine(135000, 200000, 'MKD')))).toBe(true)
      expect(classesOf(control ?? document.body)).toContain('min-h-11')
      expect(screen.queryAllByRole('button', { name: /Set a plan/ })).toHaveLength(0)
    })

    it.each([
      { who: 'the owner of an open group', group: groupWithMkdPlan, answers: {}, hasControl: true },
      { who: 'the owner of a Closing group', group: closingGroupWithPlan, answers: {}, hasControl: true },
      { who: 'the owner of a Finished group', group: finishedGroupWithPlan, answers: {}, hasControl: false },
      { who: 'a member of an open group', group: asMemberOf(groupWithMkdPlan), answers: {}, hasControl: false },
      { who: 'a member of a Closing group', group: asMemberOf(closingGroupWithPlan), answers: {}, hasControl: false },
      { who: 'a member of a Finished group', group: asMemberOf(finishedGroupWithPlan), answers: {}, hasControl: false },
      { who: 'a guest in an open group', group: asMemberOf(groupWithMkdPlan), answers: guestAnswers, hasControl: false },
    ])('shows the plan to $who; button: $hasControl', async ({ group, answers, hasControl }) => {
      const card = await planCardOf({ group, answers })

      expect(requireLine(card, ofAmountLine(135000, 200000, 'MKD'))).toBeTruthy()
      expect(planControlOf(card) !== null).toBe(hasControl)
    })

    it.each([
      { who: 'the owner of an open group', group: groupOf({}), answers: {}, showsRow: true },
      { who: 'the owner of a Closing group', group: closingGroup, answers: {}, showsRow: true },
      { who: 'the owner of a Finished group', group: finishedGroup, answers: {}, showsRow: false },
      { who: 'a member of an open group', group: asMemberOf(groupOf({})), answers: {}, showsRow: false },
      { who: 'a member of a Closing group', group: asMemberOf(closingGroup), answers: {}, showsRow: false },
      { who: 'a member of a Finished group', group: asMemberOf(finishedGroup), answers: {}, showsRow: false },
      { who: 'a guest in an open group', group: asMemberOf(groupOf({})), answers: guestAnswers, showsRow: false },
    ])('with no plan, shows "Set a plan" to $who: $showsRow', async ({ group, answers, showsRow }) => {
      await renderTotals({ group, spending: mkdOnlySpending, answers })
      await findCurrencyRegion('MKD')

      expect(querySetPlanRow() !== null).toBe(showsRow)
      expect(queryPlanCard()).toBeNull()
      expect(screen.queryAllByRole('progressbar')).toHaveLength(0)
    })

    it('draws nothing at all for a member when there is no plan', async () => {
      await renderTotals({ group: asMemberOf(groupOf({})), spending: mkdOnlySpending })
      await findCurrencyRegion('MKD')

      expect(screen.queryByText(translated('en', 'groupPlan.heading'))).toBeNull()
      expect(screen.queryByText(translated('en', 'groupPlan.setRow'))).toBeNull()
    })

    it.each(languages)('shows the "Set a plan" row as a button with a target emoji, at least 44 px high (%s)', async (language) => {
      await renderTotals({ language, group: groupOf({}), spending: mkdOnlySpending })
      await findCurrencyRegion('MKD', language)

      const row = setPlanRow(language)

      expect(row.tagName).toBe('BUTTON')
      expect(row.textContent).toContain('\u{1F3AF}')
      expect(row.querySelector('[aria-hidden="true"]')?.textContent).toContain('\u{1F3AF}')
      expect(classesOf(row)).toContain('min-h-11')
    })

    it('writes the row as "Set a plan" in English and "Постави план" in Macedonian', async () => {
      await renderTotals({ language: 'mk', group: groupOf({}), spending: mkdOnlySpending })
      await findCurrencyRegion('MKD', 'mk')

      expect(setPlanRow('mk').textContent).toContain('Постави план')
      expect(translated('en', 'groupPlan.setRow')).toBe('Set a plan')
    })
  })

  describe('where it sits', () => {
    it('comes first in the panel, above the currency cards', async () => {
      const card = await planCardOf()
      const region = await findCurrencyRegion('MKD')

      expect(isBefore(card, region)).toBe(true)
      expect(card.closest('[data-tab-panel]')).not.toBeNull()
    })

    it('puts the "Set a plan" row above the currency cards in the same place', async () => {
      await renderTotals({ group: groupOf({}), spending: mkdOnlySpending })
      const region = await findCurrencyRegion('MKD')

      expect(isBefore(setPlanRow(), region)).toBe(true)
    })

    it('shows the "Set a plan" row above "No spending yet" for an owner of a group with no expenses', async () => {
      await renderTotals({ group: groupOf({}), spending: noSpendingYet })

      const empty = await screen.findByText(translated('en', 'groupTotals.emptyTitle'))

      expect(isBefore(setPlanRow(), empty)).toBe(true)
      expect(queryCurrencyRegion('MKD')).toBeNull()
    })

    it('shows the card above "No spending yet" for a group that has a plan but no expenses', async () => {
      await renderTotals({ spending: { currencies: [], plan: planOf({ amountMinor: 200000, spentMinor: 0 }) } })

      const empty = await screen.findByText(translated('en', 'groupTotals.emptyTitle'))

      expect(isBefore(planCard(), empty)).toBe(true)
      expect(requireLine(planCard(), ofAmountLine(0, 200000, 'MKD'))).toBeTruthy()
      expect(requireLine(planCard(), onTrackLine())).toBeTruthy()
    })

    it('shows a member nothing but "No spending yet" for a group with no plan and no expenses', async () => {
      await renderTotals({ group: asMemberOf(groupOf({})), spending: noSpendingYet })

      await screen.findByText(translated('en', 'groupTotals.emptyTitle'))

      expect(queryPlanCard()).toBeNull()
      expect(querySetPlanRow()).toBeNull()
    })
  })

  describe('while the spending loads, fails and refreshes', () => {
    it('shows no card and no "Set a plan" row, even to the owner, while the spending is being asked', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { answers: { [`GET ${spendingPath}`]: neverAnswer() } })
      await screen.findByRole('heading', { level: 1, name: testGroup.name })

      expect(loadingLook().skeletonCount).toBeGreaterThan(0)
      expect(queryPlanCard()).toBeNull()
      expect(querySetPlanRow()).toBeNull()
    })

    it('shows no card and no "Set a plan" row, even to the owner, when the spending cannot be loaded', async () => {
      await renderGroupApp(routes.groupTotals(groupId), { group: groupWithMkdPlan, answers: { [`GET ${spendingPath}`]: serverError } })

      await screen.findByRole('alert')

      expect(queryPlanCard()).toBeNull()
      expect(querySetPlanRow()).toBeNull()
      expect(screen.queryAllByRole('progressbar')).toHaveLength(0)
    })

    it('shows the new plan when the spending is marked out of date and asked again', async () => {
      const changing = planThatChanges(spendingOfPlanSpent(500000), spendingOfPlanSpent(900000))
      const { queryClient } = await renderGroupApp(routes.groupTotals(groupId), {
        group: groupWithPlan(planOf()),
        answers: { [`GET ${spendingPath}`]: changing.spendingAnswer },
      })
      const card = await findPlanCard()
      expect(requireLine(card, onTrackLine())).toBeTruthy()
      changing.acceptChange()

      await queryClient.invalidateQueries({ queryKey: ['groups', groupId, 'spending'] })

      await waitFor(() => {
        expect(lineWithText(planCard(), nearLine(100000, 'MKD'))).not.toBeNull()
      })
      expectBarShows(planBarOf(planCard()), 900000, barLimitMinor, 'near')
    })

    it('takes the card away when the new answer has no plan', async () => {
      const changing = planThatChanges(spendingOfPlanSpent(500000), mkdOnlySpending)
      const { queryClient } = await renderGroupApp(routes.groupTotals(groupId), {
        group: asMemberOf(groupWithPlan(planOf())),
        answers: { [`GET ${spendingPath}`]: changing.spendingAnswer },
      })
      await findPlanCard()
      changing.acceptChange()

      await queryClient.invalidateQueries({ queryKey: ['groups', groupId, 'spending'] })

      await waitFor(() => {
        expect(queryPlanCard()).toBeNull()
      })
      expect(querySetPlanRow()).toBeNull()
    })
  })
})

function expectStateWords(
  card: HTMLElement,
  state: 'ok' | 'near' | 'over',
  spentMinor: number,
  amountMinor: number,
  currency: 'MKD' | 'EUR',
): void {
  const wordsOf = {
    ok: onTrackLine(),
    near: nearLine(amountMinor - spentMinor, currency),
    over: overLine(spentMinor - amountMinor, currency),
  }
  for (const [name, words] of Object.entries(wordsOf)) {
    const line = lineWithText(card, words)
    if (name === state) {
      expect(line, `the state line "${words}" should be on the card`).not.toBeNull()
    } else {
      expect(line, `the state line "${words}" should not be on the card`).toBeNull()
    }
  }
}
