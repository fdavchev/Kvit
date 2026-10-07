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
    [300_000, 'en', `3,000${nbsp}MKD`],
    [300_000, 'mk', `3.000${nbsp}ден.`],
    [99_900, 'en', `999${nbsp}MKD`],
    [99_900, 'mk', `999${nbsp}ден.`],
    [100_000, 'mk', `1.000${nbsp}ден.`],
    [10_000_000, 'mk', `100.000${nbsp}ден.`],
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
    [99, 'mk', '€0,99'],
    [100, 'mk', '€1,00'],
    [100_000, 'mk', '€1.000,00'],
    [100_000, 'en', '€1,000.00'],
    [4_500, 'mk', '€45,00'],
    [600, 'mk', '€6,00'],
  ] as const)('formats %i cents in %s as %s', (amount, language, expected) => {
    expect(formatMoney(amount, 'EUR', language)).toBe(expected)
  })

  it('puts the minus sign before the number for negative MKD', () => {
    expect(formatMoney(-120_000, 'MKD', 'en')).toBe(`-1,200${nbsp}MKD`)
    expect(formatMoney(-120_000, 'MKD', 'mk')).toBe(`-1.200${nbsp}ден.`)
  })

  it.each([
    [-30_000, 'en', `-300${nbsp}MKD`],
    [-30_000, 'mk', `-300${nbsp}ден.`],
    [-100, 'mk', `-1${nbsp}ден.`],
  ] as const)('writes the negative amount of %i deni in %s as %s', (amount, language, expected) => {
    expect(formatMoney(amount, 'MKD', language)).toBe(expected)
  })

  it('puts the minus sign before the euro sign for negative EUR', () => {
    expect(formatMoney(-1_550, 'EUR', 'en')).toBe('-€15.50')
    expect(formatMoney(-123_450, 'EUR', 'mk')).toBe('-€1.234,50')
  })

  it.each([
    [-5, 'en', '-€0.05'],
    [-5, 'mk', '-€0,05'],
    [-600, 'mk', '-€6,00'],
  ] as const)('writes the negative amount of %i cents in %s as %s', (amount, language, expected) => {
    expect(formatMoney(amount, 'EUR', language)).toBe(expected)
  })

  it.each([
    [300_000, 'MKD'],
    [123_450, 'EUR'],
  ] as const)('writes %i minor units of %s in Macedonian with no Latin letter in it', (amount, currency) => {
    expect(formatMoney(amount, currency, 'mk')).not.toMatch(/[A-Za-z]/)
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
