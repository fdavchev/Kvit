import { describe, expect, it } from 'vitest'
import { formatMoney } from './formatMoney'

const nbsp = ' '

describe('formatMoney', () => {
  it.each([
    [120_000, 'en', `1,200${nbsp}MKD`],
    [120_000, 'mk', `1.200${nbsp}ден.`],
    [500, 'en', `5${nbsp}MKD`],
    [123_456_700, 'en', `1,234,567${nbsp}MKD`],
    [123_456_700, 'mk', `1.234.567${nbsp}ден.`],
  ] as const)('formats %i deni in %s as %s', (amount, language, expected) => {
    expect(formatMoney(amount, 'MKD', language)).toBe(expected)
  })

  it.each([
    [1_550, 'en', '€15.50'],
    [1_550, 'mk', '€15,50'],
    [123_450, 'en', '€1,234.50'],
    [123_450, 'mk', '€1.234,50'],
    [5, 'en', '€0.05'],
    [100_000_001, 'mk', '€1.000.000,01'],
  ] as const)('formats %i cents in %s as %s', (amount, language, expected) => {
    expect(formatMoney(amount, 'EUR', language)).toBe(expected)
  })

  it('puts the minus sign before the number for negative MKD', () => {
    expect(formatMoney(-120_000, 'MKD', 'en')).toBe(`-1,200${nbsp}MKD`)
    expect(formatMoney(-120_000, 'MKD', 'mk')).toBe(`-1.200${nbsp}ден.`)
  })

  it('puts the minus sign before the euro sign for negative EUR', () => {
    expect(formatMoney(-1_550, 'EUR', 'en')).toBe('-€15.50')
    expect(formatMoney(-123_450, 'EUR', 'mk')).toBe('-€1.234,50')
  })

  it('formats zero in both currencies', () => {
    expect(formatMoney(0, 'MKD', 'en')).toBe(`0${nbsp}MKD`)
    expect(formatMoney(0, 'MKD', 'mk')).toBe(`0${nbsp}ден.`)
    expect(formatMoney(0, 'EUR', 'en')).toBe('€0.00')
    expect(formatMoney(0, 'EUR', 'mk')).toBe('€0,00')
  })

  it('formats the largest safe integer exactly', () => {
    expect(formatMoney(Number.MAX_SAFE_INTEGER, 'EUR', 'en')).toBe(
      '€90,071,992,547,409.91',
    )
  })

  it('throws for an MKD amount that is not whole denars', () => {
    expect(() => formatMoney(120_050, 'MKD', 'en')).toThrow(
      'MKD amount must be whole denars (a multiple of 100 deni), got 120050',
    )
  })

  it.each([12.5, Number.MAX_SAFE_INTEGER + 1, Number.NaN, Infinity])(
    'throws for %s, which is not a safe integer',
    (amount) => {
      expect(() => formatMoney(amount, 'EUR', 'en')).toThrow(
        `Money amount must be a safe integer of minor units, got ${amount}`,
      )
    },
  )
})
