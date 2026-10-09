import { describe, expect, it } from 'vitest'
import { parseMoneyInput } from './parseMoneyInput'

describe('parseMoneyInput for MKD', () => {
  it.each([
    ['1', 100],
    ['5', 500],
    ['10', 1000],
    ['1200', 120000],
    ['3000', 300000],
    ['9999999999', 999999999900],
    ['007', 700],
    ['0001', 100],
    [' 1200 ', 120000],
  ])('turns the whole denars "%s" into %i deni', (text, expectedMinor) => {
    expect(parseMoneyInput(text, 'MKD')).toBe(expectedMinor)
  })

  it.each([
    ['an empty text', ''],
    ['zero', '0'],
    ['several zeros', '000'],
    ['letters', 'abc'],
    ['a number with letters', '12abc'],
    ['a thousands comma', '1,200'],
    ['a thousands dot', '1.200'],
    ['a decimal comma', '12,5'],
    ['a decimal dot', '12.5'],
    ['a space inside the number', '1 200'],
    ['only spaces', '   '],
    ['the largest amount plus one denar', '10000000000'],
    ['a very long number', '99999999999999999999'],
    ['a minus sign', '-5'],
    ['a plus sign', '+5'],
    ['an exponent', '1e3'],
    ['only a space', ' '],
    ['13 digits', '1234567890123'],
    ['zeros only, however many', '0000000000000'],
  ])('answers null for %s ("%s")', (_name, text) => {
    expect(parseMoneyInput(text, 'MKD')).toBeNull()
  })
})

describe('parseMoneyInput for EUR', () => {
  it.each([
    ['1', 100],
    ['12', 1200],
    ['12.5', 1250],
    ['12,5', 1250],
    ['12.50', 1250],
    ['12,50', 1250],
    ['12.05', 1205],
    ['0.5', 50],
    ['0,05', 5],
    ['0.01', 1],
    ['1200', 120000],
    ['45.99', 4599],
    ['9999999999.99', 999999999999],
    ['9999999999', 999999999900],
    ['0012.5', 1250],
    ['012', 1200],
    [' 12,50 ', 1250],
  ])('turns "%s" into %i cents', (text, expectedMinor) => {
    expect(parseMoneyInput(text, 'EUR')).toBe(expectedMinor)
  })

  it.each([
    ['an empty text', ''],
    ['zero', '0'],
    ['zero with decimals', '0.00'],
    ['zero with a decimal comma', '0,00'],
    ['letters', 'abc'],
    ['three decimals', '12.555'],
    ['three decimals with a comma', '12,555'],
    ['two separators', '1.234,56'],
    ['two dots', '12.5.5'],
    ['two commas', '12,5,5'],
    ['an exponent', '1e3'],
    ['a minus sign', '-5'],
    ['a plus sign', '+5'],
    ['a number with letters', '12.5x'],
    ['13 digits before the separator', '1234567890123'],
    ['a dot with nothing before it', '.5'],
    ['a comma with nothing before it', ',5'],
    ['a dot with nothing after it', '5.'],
    ['a comma with nothing after it', '5,'],
    ['only a dot', '.'],
    ['one cent more than the largest amount', '10000000000.00'],
    ['the largest amount plus one euro', '10000000000'],
  ])('answers null for %s ("%s")', (_name, text) => {
    expect(parseMoneyInput(text, 'EUR')).toBeNull()
  })
})

describe('parseMoneyInput results', () => {
  it.each(['MKD', 'EUR'] as const)('answers a whole number of minor units for %s', (currency) => {
    const minor = parseMoneyInput('12', currency)

    expect(Number.isSafeInteger(minor)).toBe(true)
  })

  it('does not lose a cent to floating point rounding', () => {
    expect(parseMoneyInput('0.29', 'EUR')).toBe(29)
    expect(parseMoneyInput('1.15', 'EUR')).toBe(115)
    expect(parseMoneyInput('4.35', 'EUR')).toBe(435)
  })

  it('multiplies the whole units by 100 in both currencies', () => {
    expect(parseMoneyInput('25', 'MKD')).toBe(2500)
    expect(parseMoneyInput('25', 'EUR')).toBe(2500)
  })
})
