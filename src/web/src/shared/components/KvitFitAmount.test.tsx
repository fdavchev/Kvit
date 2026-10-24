import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages } from '@/core/i18n/language'
import { formatMoney, moneyParts } from '@/shared/utils/formatMoney'
import { formatSignedMoney, signedMoneyParts } from '@/shared/utils/formatSignedMoney'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { KvitFitAmount, KvitFitBreakableAmount, KvitFitRollingAmount } from './KvitFitAmount'

function classesOf(element: Element): string[] {
  return element.className.split(/\s+/)
}

function onlyChild(container: HTMLElement): HTMLElement {
  const box = container.firstElementChild
  if (!(box instanceof HTMLElement)) {
    throw new Error(`Expected one element, found: ${container.innerHTML}`)
  }
  return box
}

function fittedIn(box: HTMLElement): HTMLElement {
  const fitted = box.querySelector<HTMLElement>('.fit-amount')
  if (fitted === null) {
    throw new Error(`Found no .fit-amount in: ${box.innerHTML}`)
  }
  return fitted
}

function unbrokenPieces(element: HTMLElement): string[] {
  return [...element.querySelectorAll<HTMLElement>('.whitespace-nowrap')].map((piece) => piece.textContent ?? '')
}

function rollingText(rolling: Element): string {
  if (rolling.shadowRoot === null) {
    throw new Error('The rolling number drew nothing: it has no shadow root')
  }
  return [...rolling.shadowRoot.querySelectorAll('.digit__num, .symbol__value')]
    .filter((character) => character.closest('[inert]') === null)
    .map((character) => character.textContent)
    .join('')
}

describe('KvitFitAmount', () => {
  it('is its own container, a full-width block, that carries the given look and attributes', () => {
    const { container } = render(<KvitFitAmount parts={moneyParts(1234500, 'MKD', 'en')} className="text-lg font-bold" data-spent-amount="" />)

    const box = onlyChild(container)

    expect(classesOf(box)).toEqual(expect.arrayContaining(['@container', 'block', 'w-full', 'text-lg', 'font-bold']))
    expect(box.hasAttribute('data-spent-amount')).toBe(true)
  })

  it.each(languages)('puts the shrinking amount inside, with its length as --amount-length (%s)', (language) => {
    const { container } = render(<KvitFitAmount parts={moneyParts(1234500, 'MKD', language)} />)

    const fitted = fittedIn(onlyChild(container))

    expect(fitted.style.getPropertyValue('--amount-length')).toBe(String(formatMoney(1234500, 'MKD', language).length))
    expect(plainSpaces(fitted.textContent ?? '')).toBe(plainSpaces(formatMoney(1234500, 'MKD', language)))
  })

  it.each(languages)('keeps the number and the denar label each in one unbroken piece, with an ordinary space between them as the only place to wrap (%s)', (language) => {
    const { container } = render(<KvitFitAmount parts={moneyParts(1234500, 'MKD', language)} />)

    const fitted = fittedIn(onlyChild(container))

    expect(unbrokenPieces(fitted)).toEqual([language === 'en' ? '12,345' : '12.345', language === 'en' ? 'MKD' : 'ден.'])
    expect(fitted.textContent).toBe(`${unbrokenPieces(fitted)[0]} ${unbrokenPieces(fitted)[1]}`)
  })

  it('keeps the plus sign in the same unbroken piece as the number, so it never stands alone', () => {
    const { container } = render(<KvitFitAmount parts={signedMoneyParts(6500000, 'MKD', 'en')} />)

    const fitted = fittedIn(onlyChild(container))

    expect(unbrokenPieces(fitted)).toEqual(['+65,000', 'MKD'])
    expect(plainSpaces(fitted.textContent ?? '')).toBe(plainSpaces(formatSignedMoney(6500000, 'MKD', 'en')))
  })

  it('writes a euro amount as one unbroken piece, since the € sits before the number with no space', () => {
    const { container } = render(<KvitFitAmount parts={signedMoneyParts(123456789, 'EUR', 'en')} />)

    const fitted = fittedIn(onlyChild(container))

    expect(unbrokenPieces(fitted)).toEqual(['+€1,234,567.89'])
    expect(fitted.textContent).toBe(formatSignedMoney(123456789, 'EUR', 'en'))
  })
})

describe('KvitFitRollingAmount', () => {
  it('is its own container with the given look, and puts the shrinking amount inside with its length', () => {
    const { container } = render(<KvitFitRollingAmount parts={moneyParts(-120000, 'MKD', 'mk')} className="text-negative" data-total-amount="main" />)

    const box = onlyChild(container)
    const fitted = fittedIn(box)

    expect(classesOf(box)).toEqual(expect.arrayContaining(['@container', 'block', 'w-full', 'text-negative']))
    expect(box.getAttribute('data-total-amount')).toBe('main')
    expect(fitted.style.getPropertyValue('--amount-length')).toBe(String(formatMoney(-120000, 'MKD', 'mk').length))
  })

  it.each(languages)('gives screen readers the whole amount in one piece (%s)', (language) => {
    const { container } = render(<KvitFitRollingAmount parts={moneyParts(1234500, 'MKD', language)} />)

    const readable = container.querySelector('.sr-only')

    expect(readable?.textContent).toBe(formatMoney(1234500, 'MKD', language))
  })

  it('rolls only the number and puts the denar label after an ordinary space, as its own unbroken piece', () => {
    const { container } = render(<KvitFitRollingAmount parts={moneyParts(1234500, 'MKD', 'mk')} />)

    const shown = container.querySelector<HTMLElement>('[aria-hidden="true"]')
    const rolling = shown?.querySelector('number-flow-react')

    expect(rolling === null || rolling === undefined ? null : rollingText(rolling)).toBe('12.345')
    expect(unbrokenPieces(shown ?? container)).toEqual(['ден.'])
    expect(shown?.textContent?.endsWith(' ден.')).toBe(true)
  })

  it('shows a euro amount as the rolling number alone, with no label piece', () => {
    const { container } = render(<KvitFitRollingAmount parts={moneyParts(4250, 'EUR', 'en')} />)

    const shown = container.querySelector<HTMLElement>('[aria-hidden="true"]')

    expect(shown?.querySelector('number-flow-react')).not.toBeNull()
    expect(unbrokenPieces(shown ?? container)).toEqual([])
  })
})

function piecesAndBreaks(element: HTMLElement): string[] {
  return [...element.childNodes].map((node) => {
    if (node instanceof HTMLElement && node.tagName === 'WBR') {
      return '<wbr>'
    }
    return node.textContent ?? ''
  })
}

describe('KvitFitBreakableAmount', () => {
  it('is its own container with the given look and attributes, with the shrinking amount and its length inside', () => {
    const { container } = render(<KvitFitBreakableAmount parts={moneyParts(99999999999, 'EUR', 'en')} className="font-bold" data-tile-amount="" />)

    const box = onlyChild(container)
    const fitted = fittedIn(box)

    expect(classesOf(box)).toEqual(expect.arrayContaining(['@container', 'block', 'w-full', 'font-bold']))
    expect(box.hasAttribute('data-tile-amount')).toBe(true)
    expect(fitted.style.getPropertyValue('--amount-length')).toBe(String(formatMoney(99999999999, 'EUR', 'en').length))
  })

  it.each([
    ['en', ['€999,', '<wbr>', '999,', '<wbr>', '999.99']],
    ['mk', ['€999.', '<wbr>', '999.', '<wbr>', '999,99']],
  ] as const)('lets a long euro amount wrap only right after a thousands separator, never inside a group (%s)', (language, expected) => {
    const { container } = render(<KvitFitBreakableAmount parts={moneyParts(99999999999, 'EUR', language)} />)

    const fitted = fittedIn(onlyChild(container))

    expect(piecesAndBreaks(fitted)).toEqual(expected)
    expect(unbrokenPieces(fitted)).toEqual(expected.filter((piece) => piece !== '<wbr>'))
    expect(fitted.textContent).toBe(formatMoney(99999999999, 'EUR', language))
  })

  it.each(languages)('lets a long denar amount wrap after a thousands separator or before its label (%s)', (language) => {
    const { container } = render(<KvitFitBreakableAmount parts={moneyParts(1999999999800, 'MKD', language)} />)

    const fitted = fittedIn(onlyChild(container))
    const separator = language === 'en' ? ',' : '.'

    expect(unbrokenPieces(fitted)).toEqual([`19${separator}`, `999${separator}`, `999${separator}`, '998', language === 'en' ? 'MKD' : 'ден.'])
    expect(fitted.querySelectorAll('wbr')).toHaveLength(3)
    expect(plainSpaces(fitted.textContent ?? '')).toBe(plainSpaces(formatMoney(1999999999800, 'MKD', language)))
  })

  it('draws an amount under a thousand as one unbroken piece with no break point', () => {
    const { container } = render(<KvitFitBreakableAmount parts={moneyParts(99999, 'EUR', 'en')} />)

    const fitted = fittedIn(onlyChild(container))

    expect(unbrokenPieces(fitted)).toEqual(['€999.99'])
    expect(fitted.querySelectorAll('wbr')).toHaveLength(0)
  })
})
