import { describe, expect, it } from 'vitest'
import { summaryChangeOf } from './summaryChange'

describe('summaryChangeOf', () => {
  it.each([
    [1850000, 1647000, 12],
    [2920000, 1747000, 67],
    [400, 300, 33],
    [350000, 100000, 250],
    [1000000, 100000, 900],
  ])('says up and rounds the change from %i to %i to the whole percent %i', (thisMinor, previousMinor, percent) => {
    expect(summaryChangeOf(thisMinor, previousMinor)).toEqual({ direction: 'up', percent })
  })

  it.each([
    [720000, 758000, 5],
    [200, 300, 33],
    [1000000, 4000000, 75],
  ])('says down and rounds the change from %i to %i to the whole percent %i', (thisMinor, previousMinor, percent) => {
    expect(summaryChangeOf(thisMinor, previousMinor)).toEqual({ direction: 'down', percent })
  })

  it('rounds an exact half up when this month is higher: 12.5 percent is up 13', () => {
    expect(summaryChangeOf(900000, 800000)).toEqual({ direction: 'up', percent: 13 })
  })

  it('rounds an exact half away from zero when this month is lower: 12.5 percent is down 13', () => {
    expect(summaryChangeOf(700000, 800000)).toEqual({ direction: 'down', percent: 13 })
  })

  it('says down 100 percent when this month is 0 and the previous one is not', () => {
    expect(summaryChangeOf(0, 239000)).toEqual({ direction: 'down', percent: 100 })
  })

  it('says new, with no percent, when the previous month is 0 and this one is not', () => {
    expect(summaryChangeOf(900000, 0)).toEqual({ direction: 'new' })
  })

  it('says same, with no percent, when both months are equal', () => {
    expect(summaryChangeOf(650000, 650000)).toEqual({ direction: 'same' })
  })

  it('says same, not new, when both months are 0', () => {
    expect(summaryChangeOf(0, 0)).toEqual({ direction: 'same' })
  })

  it('goes by the amounts and not by the rounded percent: one denar more in 20000 is up 0 percent', () => {
    expect(summaryChangeOf(2000100, 2000000)).toEqual({ direction: 'up', percent: 0 })
  })

  it('works with the largest amounts the server allows', () => {
    expect(summaryChangeOf(999999999999, 499999999999)).toEqual({ direction: 'up', percent: 100 })
  })
})
