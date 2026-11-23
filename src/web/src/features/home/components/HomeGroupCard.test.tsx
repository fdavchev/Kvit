import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Language } from '@/core/i18n/language'
import type { DashboardGroup } from '@/core/services/dashboard/dashboardService'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { HomeGroupCard } from './HomeGroupCard'

type Balance = DashboardGroup['balances'][number]

function groupWith(balances: Balance[]): DashboardGroup {
  return { id: 'g0000000-0000-4000-8000-000000000001', kind: 'Group', name: 'Trip', emoji: '🏖️', status: 'Open', balances }
}

async function renderedAmountSpans(balances: Balance[], language: Language): Promise<HTMLElement[]> {
  await renderElementWithProviders(
    <ul>
      <HomeGroupCard group={groupWith(balances)} />
    </ul>,
    '/home',
    { language },
  )
  return Array.from(screen.getByRole('link').querySelectorAll<HTMLElement>('span.font-extrabold'))
}

function breakOpportunitiesIn(amount: HTMLElement): HTMLElement[] {
  return Array.from(amount.querySelectorAll<HTMLElement>('wbr'))
}

describe('the balance on a group card of Home', () => {
  it.each<[Language, string, number]>([
    ['en', '+5,000,000,899 MKD', 3],
    ['mk', '+5.000.000.899 ден.', 3],
  ])('offers a line break after each thousands separator of %s %s and leaves the visible text unchanged', async (language, _label, separators) => {
    const [amount] = await renderedAmountSpans([{ currency: 'MKD', amountMinor: 500000089900 }], language)

    expect(breakOpportunitiesIn(amount)).toHaveLength(separators)
    expect(amount.textContent).toBe(formatSignedMoney(500000089900, 'MKD', language))
  })

  it('puts each break opportunity directly after a thousands separator, never inside a group of digits', async () => {
    const [amount] = await renderedAmountSpans([{ currency: 'MKD', amountMinor: 500000089900 }], 'en')

    for (const breakOpportunity of breakOpportunitiesIn(amount)) {
      expect(breakOpportunity.previousSibling?.textContent?.endsWith(',')).toBe(true)
    }
  })

  it('does not split the sign and the currency of a euro amount from the digits around them', async () => {
    const [amount] = await renderedAmountSpans([{ currency: 'EUR', amountMinor: -123450 }], 'en')

    expect(breakOpportunitiesIn(amount)).toHaveLength(1)
    expect(amount.textContent).toBe(formatSignedMoney(-123450, 'EUR', 'en'))
  })

  it('adds no break opportunity to an amount below one thousand', async () => {
    const [amount] = await renderedAmountSpans([{ currency: 'MKD', amountMinor: 85000 }], 'en')

    expect(breakOpportunitiesIn(amount)).toHaveLength(0)
    expect(amount.textContent).toBe(formatSignedMoney(85000, 'MKD', 'en'))
  })

  it('shows one amount per currency, each keeping the break-words look', async () => {
    const amounts = await renderedAmountSpans(
      [
        { currency: 'MKD', amountMinor: 185000 },
        { currency: 'EUR', amountMinor: 2000 },
      ],
      'en',
    )

    expect(amounts.map((amount) => amount.textContent)).toEqual([
      formatSignedMoney(185000, 'MKD', 'en'),
      formatSignedMoney(2000, 'EUR', 'en'),
    ])
    for (const amount of amounts) {
      expect(Array.from(amount.classList)).toContain('break-words')
    }
  })
})

describe('the status of a group card on Home', () => {
  it.each<[Language, string]>([
    ['en', translated('en', 'closing.badgeClosing')],
    ['mk', translated('mk', 'closing.badgeClosing')],
  ])('shows the Closing badge on a group that is being closed (%s)', async (language, badge) => {
    await renderElementWithProviders(
      <ul>
        <HomeGroupCard group={{ ...groupWith([]), status: 'Closing' }} />
      </ul>,
      '/home',
      { language },
    )

    expect(within(screen.getByRole('link')).getByText(badge)).toBeTruthy()
  })

  it('shows no status badge on an open group', async () => {
    await renderElementWithProviders(
      <ul>
        <HomeGroupCard group={groupWith([])} />
      </ul>,
      '/home',
      { language: 'en' },
    )

    expect(screen.queryByText(translated('en', 'closing.badgeClosing'))).toBeNull()
    expect(screen.queryByText(translated('en', 'closing.badgeFinished'))).toBeNull()
  })
})
