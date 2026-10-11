import { describe, expect, it } from 'vitest'
import { readWholeNumber } from './readFields'

describe('readWholeNumber', () => {
  it.each([
    ['a positive number', 150000],
    ['zero', 0],
    ['a negative number', -70000],
    ['the largest safe integer', Number.MAX_SAFE_INTEGER],
    ['the smallest safe integer', Number.MIN_SAFE_INTEGER],
  ])('returns %s as it is', (_name, value) => {
    expect(readWholeNumber({ balanceMinor: value }, 'balanceMinor', 'a balance')).toBe(value)
  })

  it.each([
    ['a text', '100'],
    ['a fraction', 1.5],
    ['null', null],
    ['true', true],
    ['an object', {}],
    ['a list', [5]],
    ['not a number', Number.NaN],
    ['infinity', Number.POSITIVE_INFINITY],
    ['a number above the largest safe integer', 2 ** 53],
    ['a number below the smallest safe integer', -(2 ** 53)],
  ])('throws an error naming the field, what it belongs to and a whole number when the value is %s', (_name, value) => {
    expect(() => readWholeNumber({ balanceMinor: value }, 'balanceMinor', 'a balance')).toThrow(
      /"balanceMinor".*a balance.*whole number/,
    )
  })

  it('throws an error naming the field and saying nothing was there when the field is missing', () => {
    expect(() => readWholeNumber({}, 'balanceMinor', 'a balance')).toThrow(/"balanceMinor".*nothing/)
  })
})
