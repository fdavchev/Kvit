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

describe('the size of AmountField', () => {
  async function renderedInputWith(amountText: string, onCurrencyChange?: () => void): Promise<HTMLInputElement> {
    await renderElementWithProviders(
      <AmountField amountText={amountText} currency="EUR" onAmountTextChange={() => {}} onCurrencyChange={onCurrencyChange} />,
      '/amount',
    )
    return screen.getByLabelText<HTMLInputElement>(translated('en', 'expense.amount'))
  }

  it.each([
    ['', 'text-5xl'],
    ['120', 'text-5xl'],
    ['1234567', 'text-5xl'],
    ['12345678', 'text-4xl'],
    ['1234567.89', 'text-4xl'],
    ['12345678.90', 'text-3xl'],
    ['99999999999', 'text-3xl'],
  ])('shows "%s" in %s, smaller for long amounts so the first digit stays inside the box', async (amountText, size) => {
    const input = await renderedInputWith(amountText, () => {})

    const sizes = Array.from(input.classList).filter((token) => /^text-\dxl$/.test(token))

    expect(sizes).toEqual([size])
  })

  it.each([
    ['with the currency button', () => {}],
    ['with the currency as plain text', undefined],
  ])('keeps an inner padding on both sides of the box (%s)', async (_name, onCurrencyChange) => {
    const input = await renderedInputWith('1234567.89', onCurrencyChange)

    expect(Array.from(input.classList)).toContain('px-3')
    expect(Array.from(input.classList).filter((token) => /^p[lr]-/.test(token))).toEqual([])
  })
})
