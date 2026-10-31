import { describe, expect, it } from 'vitest'
import { backoffMilliseconds } from './backoff'

describe('backoffMilliseconds', () => {
  it.each([
    [1, 2000],
    [2, 5000],
    [3, 15000],
    [4, 30000],
    [5, 60000],
  ])('waits %i failed attempt(s) for %i ms', (failedAttempts, expected) => {
    expect(backoffMilliseconds(failedAttempts)).toBe(expected)
  })

  it.each([6, 7, 10, 100, 1000])('stays at 60000 ms after %i failed attempts', (failedAttempts) => {
    expect(backoffMilliseconds(failedAttempts)).toBe(60000)
  })

  it('does not wait when nothing has failed yet', () => {
    expect(backoffMilliseconds(0)).toBe(0)
  })

  it('never waits less than the attempt before it', () => {
    const waits = Array.from({ length: 12 }, (_unused, index) => backoffMilliseconds(index))

    expect(waits).toEqual([...waits].sort((first, second) => first - second))
  })
})
