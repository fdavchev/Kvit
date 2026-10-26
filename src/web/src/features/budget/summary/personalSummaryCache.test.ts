import { describe, expect, it } from 'vitest'
import { personalSummaryQueryKey } from './personalSummaryCache'

describe('the monthly summary cache', () => {
  it('keeps one month per key under personal, summary', () => {
    expect(personalSummaryQueryKey('2026-10')).toEqual(['personal', 'summary', '2026-10'])
  })

  it('gives each month its own key', () => {
    expect(personalSummaryQueryKey('2026-10')).not.toEqual(personalSummaryQueryKey('2026-09'))
  })

  it('stays apart from the key of the month answer of the Budget tab', () => {
    expect(personalSummaryQueryKey('2026-10')).not.toEqual(['personal', 'entries', '2026-10'])
  })
})
