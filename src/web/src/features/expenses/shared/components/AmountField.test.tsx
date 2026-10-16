import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { expectPlaceholderLegibleOn, placeholderTokenOf } from '@/test/placeholderTestHelpers'
import { translated } from '@/test/translated'
import { AmountField } from './AmountField'

async function renderedAmountInput(): Promise<HTMLInputElement> {
  await renderElementWithProviders(
    <AmountField amountText="" currency="MKD" onAmountTextChange={() => {}} />,
    '/amount',
  )
  return screen.getByLabelText<HTMLInputElement>(translated('en', 'expense.amount'))
}

describe('the placeholder of AmountField', () => {
  it('still shows 0 as the placeholder', async () => {
    const input = await renderedAmountInput()

    expect(input.placeholder).toBe('0')
  })

  it.each(['light', 'dark'] as const)('is not faded with an opacity in %s mode', async (theme) => {
    const input = await renderedAmountInput()

    expect(() => placeholderTokenOf(input, theme)).not.toThrow()
  })

  it('has at least 4.5:1 contrast on the page and on a sheet, in light and in dark mode', async () => {
    const input = await renderedAmountInput()

    expectPlaceholderLegibleOn(input, ['background', 'card'])
  })
})
