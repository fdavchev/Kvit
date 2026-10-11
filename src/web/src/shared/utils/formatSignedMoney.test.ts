import { describe, expect, it } from 'vitest'
import { formatMoney } from './formatMoney'
import { formatSignedMoney } from './formatSignedMoney'

const nbsp = ' '
const minus = '−'

describe('formatSignedMoney', () => {
  it.each([
    [150_000, 'en', `+1,500${nbsp}MKD`],
    [150_000, 'mk', `+1.500${nbsp}ден.`],
    [100, 'en', `+1${nbsp}MKD`],
    [123_456_700, 'en', `+1,234,567${nbsp}MKD`],
  ] as const)('writes %i deni in %s with a plus sign as %s', (amount, language, expected) => {
    expect(formatSignedMoney(amount, 'MKD', language)).toBe(expected)
  })

  it.each([
    [-70_000, 'en', `${minus}700${nbsp}MKD`],
    [-70_000, 'mk', `${minus}700${nbsp}ден.`],
    [-100, 'en', `${minus}1${nbsp}MKD`],
    [-123_456_700, 'mk', `${minus}1.234.567${nbsp}ден.`],
  ] as const)('writes %i deni in %s with the real minus sign U+2212 as %s', (amount, language, expected) => {
    expect(formatSignedMoney(amount, 'MKD', language)).toBe(expected)
  })

  it.each([
    [2_000, 'en', '+€20.00'],
    [2_000, 'mk', '+€20,00'],
    [-1_550, 'en', `${minus}€15.50`],
    [-1_550, 'mk', `${minus}€15,50`],
    [5, 'en', '+€0.05'],
    [-5, 'mk', `${minus}€0,05`],
  ] as const)('writes %i cents in %s as %s', (amount, language, expected) => {
    expect(formatSignedMoney(amount, 'EUR', language)).toBe(expected)
  })

  it.each([
    ['MKD', 'en'],
    ['MKD', 'mk'],
    ['EUR', 'en'],
    ['EUR', 'mk'],
  ] as const)('writes zero %s in %s with no sign, exactly like formatMoney', (currency, language) => {
    expect(formatSignedMoney(0, currency, language)).toBe(formatMoney(0, currency, language))
    expect(formatSignedMoney(0, currency, language)).not.toMatch(/[+−-]/)
  })

  it('never writes the hyphen-minus that formatMoney writes for a negative amount', () => {
    expect(formatSignedMoney(-70_000, 'MKD', 'en')).not.toContain('-')
  })

  it('throws an error naming the amount when a whole-denar MKD amount has deni left over', () => {
    expect(() => formatSignedMoney(150, 'MKD', 'en')).toThrow('150')
  })

  it('throws an error naming the amount when the amount is not a safe integer', () => {
    expect(() => formatSignedMoney(12.5, 'EUR', 'en')).toThrow('12.5')
  })
})
