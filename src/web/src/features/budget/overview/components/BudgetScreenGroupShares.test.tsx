import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  findGroupSharesBlock,
  queryGroupSharesBlock,
  shareAmountOf,
  shareGroupLabelOf,
  shareRowText,
  shareRows,
  shareTextColumnOf,
  shareTitleOf,
} from '@/test/budgetGroupShareTestHelpers'
import { classesOf, money } from '@/test/budgetLeftCardTestHelpers'
import {
  budgetNow,
  findEntryList,
  findTotalCard,
  monthButton,
  personalEntriesGet,
  renderBudget,
} from '@/test/budgetTestHelpers'
import { isBefore } from '@/test/expenseTestHelpers'
import {
  dinnerShare,
  euroTaxiShare,
  flatGroupId,
  greeceTripGroupId,
  groupSharesOnlyOctober,
  hotelShare,
  longGroupNameShare,
  longTitleShare,
  longTitleWord,
  octoberWithGroupShares,
  untitledGroceriesShare,
  untitledPlainShare,
} from '@/test/groupShareTestData'
import { catchUpUrl } from '@/test/recurringIncomeTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { personalFoodCategory, personalGroceriesCategory } from '@/test/personalCategoryTestData'
import { emptyMonth, octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { neverAnswer } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { translated } from '@/test/translated'

const foodLabel = personalCategoryLabel('en', personalFoodCategory)
const groceriesLabel = personalCategoryLabel('en', personalGroceriesCategory)

describe('BudgetScreen, the "From groups" block', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('when it is shown', () => {
    it.each(languages)('shows a region headed "From groups" with one row per group share when the month has some (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithGroupShares] })

      const block = await findGroupSharesBlock(language)

      expect(within(block).getByRole('heading', { level: 2, name: translated(language, 'budget.groupShares.title') })).toBeTruthy()
      expect(shareRows(block)).toHaveLength(5)
    })

    it('is not shown when the month has no group shares', async () => {
      await renderBudget({ months: [octoberMonth] })
      await findTotalCard()

      expect(queryGroupSharesBlock()).toBeNull()
      expect(screen.queryByText(translated('en', 'budget.groupShares.title'))).toBeNull()
    })

    it('is not shown in an empty month', async () => {
      await renderBudget({ months: [emptyMonth('2026-10')] })
      await screen.findByText(translated('en', 'budget.screen.emptyTitle'))

      expect(queryGroupSharesBlock()).toBeNull()
    })

    it('comes after the list of the personal entries', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const block = await findGroupSharesBlock()

      expect(isBefore(await findEntryList(), block)).toBe(true)
    })

    it('goes away when another month without group shares is picked, and comes back with the month that has them', async () => {
      await renderBudget({ months: [octoberWithGroupShares, septemberWithArchivedCategory] })
      await findGroupSharesBlock()

      fireEvent.click(monthButton('Sep'))

      await waitFor(() => {
        expect(queryGroupSharesBlock()).toBeNull()
      })
      fireEvent.click(monthButton('Oct'))
      expect(await findGroupSharesBlock()).toBeTruthy()
    })

    it('shows skeletons and no block while the month loads', async () => {
      await renderBudget({ answers: { [personalEntriesGet('2026-10')]: neverAnswer() } })

      expect(loadingLook().skeletonCount).toBeGreaterThanOrEqual(6)
      expect(queryGroupSharesBlock()).toBeNull()
    })
  })

  describe('the order of the rows', () => {
    it('puts the newest date first and the expenses of one day by expense id, whatever order the server sends', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      expect(rows.map((row) => shareRowText(row).title)).toEqual(['Groceries', 'Dinner', 'Taxi', 'Hotel', 'Flat'])
    })

    it('sorts the same way when the server sends the newest first and the ids the other way round', async () => {
      const shuffled = [untitledGroceriesShare, euroTaxiShare, dinnerShare, hotelShare, untitledPlainShare]
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: shuffled }] })

      const rows = shareRows(await findGroupSharesBlock())

      expect(rows.map((row) => shareRowText(row).title)).toEqual(['Groceries', 'Dinner', 'Taxi', 'Hotel', 'Flat'])
    })
  })

  describe('what a row shows', () => {
    it('shows the title, the group, the category with the day, and the amount of every share', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      expect(rows.map(shareRowText)).toEqual([
        { title: groceriesLabel, group: 'Flat', details: 'Today', amount: money(30000, 'MKD') },
        { title: 'Dinner', group: 'Flat', details: `${foodLabel} · Yesterday`, amount: money(95000, 'MKD') },
        { title: 'Taxi', group: 'Greece trip', details: `${foodLabel} · Yesterday`, amount: money(800, 'EUR') },
        { title: 'Hotel', group: 'Greece trip', details: '14 Oct', amount: money(480000, 'MKD') },
        { title: 'Flat', group: 'Flat', details: '10 Oct', amount: money(12000, 'MKD') },
      ])
    })

    it('shows the title when the expense has one', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [dinnerShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(shareRowText(row).title).toBe('Dinner')
    })

    it('shows the category name instead when the title is empty', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [untitledGroceriesShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(shareRowText(row).title).toBe(groceriesLabel)
    })

    it('shows the group name instead when the title and the category are both empty', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [untitledPlainShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(shareRowText(row).title).toBe('Flat')
    })

    it('shows the group name in a small label on every row, also when it is also the title', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      expect(rows.map((row) => shareRowText(row).group)).toEqual(['Flat', 'Flat', 'Greece trip', 'Greece trip', 'Flat'])
    })

    it('does not repeat the category in the small line when the category is already the title', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [untitledGroceriesShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(shareRowText(row).details).toBe('Today')
    })

    it('leaves the category out of the small line when the expense has none', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [hotelShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(shareRowText(row).details).toBe('14 Oct')
    })

    it('writes the day and the category in Macedonian', async () => {
      await renderBudget({ language: 'mk', months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock('mk'))

      expect(rows.map((row) => shareRowText(row).details)).toEqual([
        'Денес',
        `${personalCategoryLabel('mk', personalFoodCategory)} · Вчера`,
        `${personalCategoryLabel('mk', personalFoodCategory)} · Вчера`,
        '14 окт',
        '10 окт',
      ])
      expect(shareRowText(rows[0]).title).toBe(personalCategoryLabel('mk', personalGroceriesCategory))
    })
  })

  describe('the amounts', () => {
    it.each(languages)('shows MKD and EUR shares each in their own currency and never adds them (%s)', async (language) => {
      await renderBudget({ language, months: [octoberWithGroupShares] })

      const block = await findGroupSharesBlock(language)

      expect(shareRows(block).map((row) => shareRowText(row).amount)).toEqual([
        money(30000, 'MKD', language),
        money(95000, 'MKD', language),
        money(800, 'EUR', language),
        money(480000, 'MKD', language),
        money(12000, 'MKD', language),
      ])
    })

    it('draws every amount with KvitAmount: a text for screen readers and a rolling number hidden from them', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      for (const row of rows) {
        const amount = shareAmountOf(row)
        expect(amount.querySelector('.sr-only')).not.toBeNull()
        expect(amount.querySelector('[aria-hidden="true"]')).not.toBeNull()
      }
    })

    it('writes no plus or minus in front of a share', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      for (const row of rows) {
        expect(shareRowText(row).amount).toMatch(/^[^+\-−]/)
      }
    })
  })

  describe('tapping a row', () => {
    it('is a link to the expense of that group, the same page the list of the group opens', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      expect(rows.map((row) => within(row).getByRole('link').getAttribute('href'))).toEqual([
        routes.groupExpense(flatGroupId, untitledGroceriesShare.expenseId),
        routes.groupExpense(flatGroupId, dinnerShare.expenseId),
        routes.groupExpense(greeceTripGroupId, euroTaxiShare.expenseId),
        routes.groupExpense(greeceTripGroupId, hotelShare.expenseId),
        routes.groupExpense(flatGroupId, untitledPlainShare.expenseId),
      ])
    })

    it('goes to the expense of the group when a row is tapped', async () => {
      const { router } = await renderBudget({ months: [octoberWithGroupShares] })
      const rows = shareRows(await findGroupSharesBlock())

      fireEvent.click(within(rows[2]).getByRole('link'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groupExpense(greeceTripGroupId, euroTaxiShare.expenseId))
      })
    })

    it('opens no sheet and sends no write except the repeating-income catch-up: a group share cannot be edited or deleted here', async () => {
      const { fetchMock } = await renderBudget({ months: [octoberWithGroupShares] })
      const block = await findGroupSharesBlock()

      expect(within(block).queryAllByRole('button')).toHaveLength(0)
      fireEvent.click(within(shareRows(block)[1]).getByRole('link'))

      await waitFor(() => {
        expect(screen.queryByRole('region', { name: translated('en', 'budget.groupShares.title') })).toBeNull()
      })
      expect(screen.queryByRole('dialog')).toBeNull()
      const writes = fetchMock.mock.calls.filter(([input, init]) => (init?.method ?? 'GET') !== 'GET' && String(input) !== catchUpUrl)
      expect(writes).toHaveLength(0)
    })
  })

  describe('the look on a 360 px phone', () => {
    it('lets the group name wrap, even a very long one, instead of widening the row', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [longGroupNameShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())
      const label = shareGroupLabelOf(row)

      expect(shareRowText(row).group).toBe(longGroupNameShare.groupName)
      expect(classesOf(label)).toContain('break-words')
      expect(classesOf(label)).not.toContain('whitespace-nowrap')
      expect(classesOf(label)).not.toContain('truncate')
    })

    it('keeps long titles inside the row and the amount on one line', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const [row] = shareRows(await findGroupSharesBlock())

      expect(classesOf(shareTextColumnOf(row))).toEqual(expect.arrayContaining(['min-w-0', 'flex-1']))
      expect(classesOf(shareTitleOf(row))).toContain('break-words')
      expect(classesOf(shareAmountOf(row))).toContain('whitespace-nowrap')
    })

    it('keeps a very long one-word title inside the column: it breaks inside the word and is never wider than its column', async () => {
      await renderBudget({ months: [{ ...octoberWithGroupShares, groupShares: [longTitleShare] }] })

      const [row] = shareRows(await findGroupSharesBlock())
      const title = shareTitleOf(row)

      expect(shareRowText(row).title).toBe(longTitleWord)
      expect(classesOf(title)).toContain('break-words')
      expect(classesOf(title)).toContain('max-w-full')
      expect(classesOf(title)).not.toContain('whitespace-nowrap')
      expect(classesOf(title)).not.toContain('truncate')
      expect(classesOf(shareTextColumnOf(row))).toEqual(expect.arrayContaining(['min-w-0', 'flex-1']))
    })

    it('makes every row at least 44 px high', async () => {
      await renderBudget({ months: [octoberWithGroupShares] })

      const rows = shareRows(await findGroupSharesBlock())

      for (const row of rows) {
        expect(classesOf(within(row).getByRole('link'))).toContain('min-h-16')
      }
    })
  })

  describe('a month with only group shares', () => {
    it('shows the block, the card and the tiles and not the empty state', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })

      const block = await findGroupSharesBlock()

      expect(shareRows(block)).toHaveLength(3)
      expect(await findTotalCard()).toBeTruthy()
      expect(screen.getByRole('region', { name: translated('en', 'budget.screen.categoryTotals') })).toBeTruthy()
      expect(screen.queryByText(translated('en', 'budget.screen.emptyTitle'))).toBeNull()
      expect(screen.queryByText(translated('en', 'expenses.emptyHint'))).toBeNull()
    })

    it('shows no list of personal entries, because there are none', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })
      await findGroupSharesBlock()

      expect(screen.queryByRole('list', { name: translated('en', 'budget.screen.entries') })).toBeNull()
    })

    it('keeps the plus button so a personal entry can still be added', async () => {
      await renderBudget({ months: [groupSharesOnlyOctober] })
      await findGroupSharesBlock()

      expect(screen.getByRole('button', { name: translated('en', 'budget.screen.add') })).toBeTruthy()
    })
  })
})
