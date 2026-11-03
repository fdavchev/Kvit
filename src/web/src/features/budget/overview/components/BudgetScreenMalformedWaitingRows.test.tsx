import { fireEvent, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { budgetNow, findEntryList, renderBudget } from '@/test/budgetTestHelpers'
import { queuedPersonalEntry, withoutDisplayFields } from '@/test/outboxScreenTestData'
import {
  consoleErrorCallsAbout,
  discardSheet,
  discardThroughTheSheet,
  expectMalformedRowShown,
  findWaitingRowWith,
  rowWithoutText,
  savedIdsOfTheSignedInPerson,
  tapTargetOf,
  waitingRowText,
  waitingRows,
} from '@/test/outboxScreenTestHelpers'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { octoberMonth, septemberWithArchivedCategory } from '@/test/personalEntryTestData'
import { personalFoodCategory } from '@/test/personalCategoryTestData'
import { personalCategoryLabel } from '@/test/personalCategoryTestHelpers'
import { testMe } from '@/test/testMe'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const categoryNobodyHas = 'e0000000-0000-4000-8000-0000000000ff'

async function renderBudgetWith(waiting: OutboxItem[]) {
  seedOutbox(testMe.id, waiting)
  return renderBudget({ months: [octoberMonth, septemberWithArchivedCategory] })
}

function realEntryItems(list: HTMLElement): HTMLElement[] {
  return Array.from(list.querySelectorAll<HTMLElement>('li')).filter(
    (item) => !item.matches('[data-waiting-row]') && item.querySelector('[data-waiting-row]') === null,
  )
}

function neutralTileOf(row: HTMLElement): HTMLElement | null {
  return row.querySelector<HTMLElement>('span.size-11.border-dashed')
}

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

describe('BudgetScreen with a waiting spending whose category is not in the list any more', () => {
  async function renderWithLostCategory(display: { title?: string } = { title: 'Lost category lunch' }) {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderBudgetWith([
      queuedPersonalEntry('good', { display: { title: 'Bakery run' } }),
      queuedPersonalEntry('lost-category', { display: { ...display, categoryId: categoryNobodyHas } }),
    ])
    await findWaitingRowWith('Bakery run')
    return consoleError
  }

  it('still shows the Budget screen with the real entries and the other waiting row', async () => {
    await renderWithLostCategory()

    const list = await findEntryList()

    expect(realEntryItems(list)).toHaveLength(octoberMonth.entries.length)
    expect(waitingRows().some((row) => waitingRowText(row).includes('Bakery run'))).toBe(true)
  })

  it('shows the row with its note as the title', async () => {
    await renderWithLostCategory()

    expect(await findWaitingRowWith('Lost category lunch')).toBeTruthy()
  })

  it('shows the row with the neutral tile of the entries that have no category, and without a category name', async () => {
    await renderWithLostCategory()
    const row = await findWaitingRowWith('Lost category lunch')

    expect(neutralTileOf(row)).not.toBeNull()
    expect(neutralTileOf(row)?.textContent).toBe('')
    expect(waitingRowText(row)).not.toContain(personalCategoryLabel('en', personalFoodCategory))
    expect(waitingRowText(row)).not.toContain(categoryNobodyHas)
  })

  it('shows the dash as the title when the entry has no note either', async () => {
    await renderWithLostCategory({ title: undefined })
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expectMalformedRowShown(rowWithoutText('Bakery run'))
  })

  it('opens the discard sheet when the row is tapped', async () => {
    await renderWithLostCategory()
    const row = await findWaitingRowWith('Lost category lunch')

    fireEvent.click(tapTargetOf(row))

    expect(discardSheet()).toBeTruthy()
  })

  it('lets the person discard it', async () => {
    await renderWithLostCategory()
    const row = await findWaitingRowWith('Lost category lunch')

    await discardThroughTheSheet(row)

    expect(savedIdsOfTheSignedInPerson()).toEqual(['good'])
  })

  it('logs it once with console.error, naming the entry and its category', async () => {
    const consoleError = await renderWithLostCategory()
    await findWaitingRowWith('Lost category lunch')

    const calls = consoleErrorCallsAbout(consoleError, 'lost-category')

    expect(calls).toHaveLength(1)
    expect(calls[0].map(String).join(' ')).toContain(categoryNobodyHas)
  })
})

describe('BudgetScreen with a stored waiting entry that has none of its display fields', () => {
  async function renderWithBrokenEntry() {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderBudgetWith([
      queuedPersonalEntry('good', { display: { title: 'Bakery run' } }),
      withoutDisplayFields(queuedPersonalEntry('broken')),
    ])
    await findWaitingRowWith('Bakery run')
    return consoleError
  }

  it('still shows the real entries and the other waiting row', async () => {
    await renderWithBrokenEntry()

    const list = await findEntryList()

    expect(realEntryItems(list)).toHaveLength(octoberMonth.entries.length)
    expect(waitingRows().some((row) => waitingRowText(row).includes('Bakery run'))).toBe(true)
  })

  it('shows the broken entry as a visible waiting row of the month on the screen, titled with the dash', async () => {
    await renderWithBrokenEntry()

    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    const row = expectMalformedRowShown(rowWithoutText('Bakery run'))
    expect(neutralTileOf(row)).not.toBeNull()
  })

  it('opens the discard sheet when the broken row is tapped', async () => {
    await renderWithBrokenEntry()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    fireEvent.click(tapTargetOf(expectMalformedRowShown(rowWithoutText('Bakery run'))))

    expect(discardSheet()).toBeTruthy()
  })

  it('lets the person discard the broken entry', async () => {
    await renderWithBrokenEntry()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    await discardThroughTheSheet(expectMalformedRowShown(rowWithoutText('Bakery run')))

    expect(savedIdsOfTheSignedInPerson()).toEqual(['good'])
  })

  it('logs the broken entry once with console.error, naming its id', async () => {
    const consoleError = await renderWithBrokenEntry()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'broken')).toHaveLength(1)
  })

  it('does not log the good entry', async () => {
    const consoleError = await renderWithBrokenEntry()
    await waitFor(() => {
      expect(waitingRows()).toHaveLength(2)
    })

    expect(consoleErrorCallsAbout(consoleError, 'good')).toEqual([])
  })
})
