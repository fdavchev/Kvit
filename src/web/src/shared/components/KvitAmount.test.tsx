import type { NumberFlowElement } from '@number-flow/react'
import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages } from '@/core/i18n/language'
import { formatMoney, moneyParts, type Currency } from '@/shared/utils/formatMoney'
import { formatSignedMoney, signedMoneyParts } from '@/shared/utils/formatSignedMoney'
import { amountRollDurationMs, KvitAmount } from './KvitAmount'

const amountsByCurrency: Record<Currency, readonly number[]> = {
  MKD: [0, 100, 99_900, 100_000, 120_000, 123_456_700, -30_000, -120_000],
  EUR: [0, 5, 99, 1_550, 100_000, 123_450, 100_000_001, Number.MAX_SAFE_INTEGER, -5, -123_450],
}

const cases = languages.flatMap((language) =>
  (['MKD', 'EUR'] as const).flatMap((currency) =>
    amountsByCurrency[currency].map((amountMinor) => ({ language, currency, amountMinor })),
  ),
)

function rollingNumberIn(container: HTMLElement): NumberFlowElement {
  const flow = container.querySelector<NumberFlowElement>('number-flow-react')
  if (flow === null) {
    throw new Error(`Found no rolling number in: ${container.innerHTML}`)
  }
  return flow
}

function shownTextOf(flow: NumberFlowElement): string {
  if (flow.shadowRoot === null) {
    throw new Error('The rolling number drew nothing: it has no shadow root')
  }
  return [...flow.shadowRoot.querySelectorAll('.digit__num, .symbol__value')]
    .filter((character) => character.closest('[inert]') === null)
    .map((character) => character.textContent)
    .join('')
}

function screenReaderTextIn(container: HTMLElement): string {
  const readable = [...container.querySelectorAll('span')].filter((span) => span.closest('[aria-hidden="true"]') === null)
  return readable.map((span) => span.textContent).join('')
}

describe('KvitAmount', () => {
  it.each(cases)('shows $amountMinor $currency in $language exactly as formatMoney writes it', ({ language, currency, amountMinor }) => {
    const { container } = render(<KvitAmount parts={moneyParts(amountMinor, currency, language)} />)

    expect(shownTextOf(rollingNumberIn(container))).toBe(formatMoney(amountMinor, currency, language))
  })

  it.each(cases)('shows $amountMinor $currency in $language with its sign exactly as formatSignedMoney writes it', ({ language, currency, amountMinor }) => {
    const { container } = render(<KvitAmount parts={signedMoneyParts(amountMinor, currency, language)} />)

    expect(shownTextOf(rollingNumberIn(container))).toBe(formatSignedMoney(amountMinor, currency, language))
  })

  it.each(cases)('gives screen readers the whole amount of $amountMinor $currency in $language, not digit by digit', ({ language, currency, amountMinor }) => {
    const { container } = render(<KvitAmount parts={signedMoneyParts(amountMinor, currency, language)} />)

    expect(screenReaderTextIn(container)).toBe(formatSignedMoney(amountMinor, currency, language))
    expect(rollingNumberIn(container).getAttribute('aria-hidden')).toBe('true')
  })

  it.each(languages)('shows the new amount after the value changes in %s, on screen and for screen readers', (language) => {
    const { container, rerender } = render(<KvitAmount parts={signedMoneyParts(150_000, 'MKD', language)} />)

    rerender(<KvitAmount parts={signedMoneyParts(-70_000, 'MKD', language)} />)

    expect(shownTextOf(rollingNumberIn(container))).toBe(formatSignedMoney(-70_000, 'MKD', language))
    expect(screenReaderTextIn(container)).toBe(formatSignedMoney(-70_000, 'MKD', language))
  })

  it('keeps the reduced-motion setting of the rolling number switched on, so a device that asks for less motion gets none', () => {
    const { container } = render(<KvitAmount parts={moneyParts(120_000, 'MKD', 'en')} />)

    expect(rollingNumberIn(container).respectMotionPreference).toBe(true)
  })

  it('starts no animation when the value changes on a device that asks for reduced motion', () => {
    const { container, rerender } = render(<KvitAmount parts={moneyParts(120_000, 'MKD', 'en')} />)
    const flow = rollingNumberIn(container)
    let animationsStarted = 0
    flow.addEventListener('animationsstart', () => {
      animationsStarted += 1
    })

    rerender(<KvitAmount parts={moneyParts(185_000, 'MKD', 'en')} />)

    expect(window.matchMedia('(prefers-reduced-motion: reduce)').matches).toBe(true)
    expect(flow.computedAnimated).toBe(false)
    expect(animationsStarted).toBe(0)
  })

  it('rolls the digits in 250 ms', () => {
    const { container } = render(<KvitAmount parts={moneyParts(120_000, 'MKD', 'en')} />)

    expect(amountRollDurationMs).toBe(250)
    expect(rollingNumberIn(container).transformTiming.duration).toBe(250)
    expect(rollingNumberIn(container).opacityTiming.duration).toBe(250)
  })

  it('uses digits of one width, so the amount does not jump sideways while it rolls', () => {
    const { container } = render(<KvitAmount parts={moneyParts(120_000, 'MKD', 'en')} />)

    expect(rollingNumberIn(container).classList.contains('tabular-nums')).toBe(true)
  })
})
