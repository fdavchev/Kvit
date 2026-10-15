import { screen } from '@testing-library/react'
import { afterEach, describe, it, vi } from 'vitest'
import { dashboardOf, markoPaymentItem } from '@/test/dashboardTestData'
import { openNeedsBar, renderHome } from '@/test/homeTestHelpers'
import { expectOnlyTheVerbPhraseDoesNotWrap, macedonianVerbPhrase } from '@/test/noWrapTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

async function paymentCardInMacedonian(): Promise<HTMLElement> {
  await renderHome({ language: 'mk', dashboard: dashboardOf({ needsYou: [markoPaymentItem] }) })
  await screen.findByRole('heading', { name: translated('mk', 'home.groups') })
  openNeedsBar('mk')
  const card = screen.getByRole('button', { name: translated('mk', 'settlements.reject') }).closest('li')
  if (card === null) {
    throw new Error('The Reject button is not inside a card')
  }
  return card
}

describe('the payment card on Home in Macedonian', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('keeps платил/а on one line', async () => {
    const card = await paymentCardInMacedonian()
    const sentence = translated('mk', 'balances.saysPaidYou', {
      from: 'Marko',
      amount: formatMoney(60000, 'MKD', 'mk'),
    })

    if (!sentence.includes(macedonianVerbPhrase)) {
      throw new Error(`The Macedonian sentence no longer holds "${macedonianVerbPhrase}": ${sentence}`)
    }
    expectOnlyTheVerbPhraseDoesNotWrap(card, sentence)
  })
})
