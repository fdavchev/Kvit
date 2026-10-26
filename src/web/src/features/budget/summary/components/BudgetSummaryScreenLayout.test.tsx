import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { budgetNow } from '@/test/budgetTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { activePersonalCategories, coffeeCategory } from '@/test/personalCategoryTestData'
import { longNameSummary } from '@/test/personalSummaryTestData'
import {
  amountReadout,
  categoryRows,
  changeOf,
  findCurrencyCard,
  nameOf,
  previousAmountOf,
  renderSummaryScreen,
  thisAmountOf,
  totalRow,
} from '@/test/personalSummaryTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const longName = 'Coffee, tea, cakes and every other little thing I buy on the way to work in the morning'
const longNamedCoffee = { ...coffeeCategory, name: longName }
const categoriesWithLongName = activePersonalCategories.map((category) =>
  category.id === coffeeCategory.id ? longNamedCoffee : category,
)

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

describe('the monthly summary screen, the layout at 360 px', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(budgetNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('lets a long category name wrap and shrink instead of pushing the amounts out', async () => {
    await renderSummaryScreen({ summaries: [longNameSummary], categories: categoriesWithLongName })
    const card = await findCurrencyCard('MKD')

    const name = nameOf(categoryRows(card)[0])

    expect(name.textContent).toBe(longName)
    expect(classesOf(name)).toContain('break-words')
    expect(classesOf(name)).toContain('min-w-0')
  })

  it('keeps an amount of a category on one line, this month and the previous month', async () => {
    await renderSummaryScreen({ summaries: [longNameSummary], categories: categoriesWithLongName })
    const card = await findCurrencyCard('MKD')

    const row = categoryRows(card)[0]

    expect(classesOf(thisAmountOf(row))).toContain('whitespace-nowrap')
    expect(classesOf(previousAmountOf(row))).toContain('whitespace-nowrap')
  })

  it('keeps the amounts of the totals on one line too', async () => {
    await renderSummaryScreen({ summaries: [longNameSummary], categories: categoriesWithLongName })
    const card = await findCurrencyCard('MKD')

    const row = totalRow(card, 'spending')

    expect(classesOf(thisAmountOf(row))).toContain('whitespace-nowrap')
    expect(classesOf(previousAmountOf(row))).toContain('whitespace-nowrap')
  })

  it('keeps the change of a row on one line', async () => {
    await renderSummaryScreen({ summaries: [longNameSummary], categories: categoriesWithLongName })
    const card = await findCurrencyCard('MKD')

    expect(classesOf(changeOf(categoryRows(card)[0]))).toContain('whitespace-nowrap')
    expect(classesOf(changeOf(totalRow(card, 'spending')))).toContain('whitespace-nowrap')
  })

  it('shows the largest amounts the server allows in full', async () => {
    await renderSummaryScreen({ summaries: [longNameSummary], categories: categoriesWithLongName })
    const card = await findCurrencyCard('MKD')

    const row = categoryRows(card)[0]

    expect(amountReadout(thisAmountOf(row))).toBe(money(99999999900, 'MKD'))
    expect(amountReadout(previousAmountOf(row))).toBe(money(12345678900, 'MKD'))
  })
})
