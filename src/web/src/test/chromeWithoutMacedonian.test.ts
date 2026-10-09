import { afterEach, describe, expect, it } from 'vitest'
import { makePluralRulesLikeChromeWithoutMacedonian } from './chromeWithoutMacedonian'

const noon = new Date('2026-10-03T12:00:00Z')

describe('the test setup that makes Intl behave like Chrome without Macedonian data', () => {
  it.each([
    ['DateTimeFormat', () => Intl.DateTimeFormat.supportedLocalesOf(['mk'])],
    ['NumberFormat', () => Intl.NumberFormat.supportedLocalesOf(['mk'])],
    ['RelativeTimeFormat', () => Intl.RelativeTimeFormat.supportedLocalesOf(['mk'])],
  ])('says that Intl.%s does not support mk', (_name, supportedLocales) => {
    expect(supportedLocales()).toEqual([])
  })

  it.each([
    ['DateTimeFormat', () => Intl.DateTimeFormat.supportedLocalesOf(['en-US'])],
    ['NumberFormat', () => Intl.NumberFormat.supportedLocalesOf(['en-US'])],
  ])('still says that Intl.%s supports en-US', (_name, supportedLocales) => {
    expect(supportedLocales()).toEqual(['en-US'])
  })

  it('writes a date for mk in English, as Chrome does', () => {
    const text = new Intl.DateTimeFormat('mk', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(noon)

    expect(text).toBe('October 3, 2026')
  })

  it('writes a number for mk with a comma for thousands and a dot for decimals, as Chrome does', () => {
    const text = new Intl.NumberFormat('mk', {
      useGrouping: 'always',
      minimumFractionDigits: 2,
    }).format(1200.5)

    expect(text).toBe('1,200.50')
  })

  it('writes a relative time for mk in English, as Chrome does', () => {
    expect(new Intl.RelativeTimeFormat('mk').format(-5, 'minute')).toBe('5 minutes ago')
  })

  it('takes the next language of the list when the first one is mk', () => {
    const text = new Intl.DateTimeFormat(['mk', 'en-GB'], {
      day: 'numeric',
      month: 'long',
      timeZone: 'UTC',
    }).format(noon)

    expect(text).toBe('3 October')
  })

  it('treats mk-MK as unsupported too', () => {
    expect(Intl.NumberFormat.supportedLocalesOf(['mk-MK'])).toEqual([])
  })

  it('leaves English dates exactly as they were', () => {
    const text = new Intl.DateTimeFormat('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(noon)

    expect(text).toBe('Oct 3, 2026')
  })

  it('keeps instances usable with instanceof, the way the production code and other tests rely on', () => {
    expect(new Intl.DateTimeFormat('en-US') instanceof Intl.DateTimeFormat).toBe(true)
  })
})

describe('the plural rules that make Intl.PluralRules behave like Chrome without Macedonian data', () => {
  let restore: (() => void) | null = null

  afterEach(() => {
    restore?.()
    restore = null
  })

  it('has Macedonian plural rules until they are hidden: 21 is "one"', () => {
    expect(new Intl.PluralRules('mk').select(21)).toBe('one')
  })

  it('uses the English rules for mk while they are hidden: 21 is "other"', () => {
    restore = makePluralRulesLikeChromeWithoutMacedonian()

    expect(new Intl.PluralRules('mk').select(21)).toBe('other')
    expect(new Intl.PluralRules('mk').select(1)).toBe('one')
  })

  it('puts the Macedonian plural rules back when it is restored', () => {
    const undo = makePluralRulesLikeChromeWithoutMacedonian()

    undo()

    expect(new Intl.PluralRules('mk').select(21)).toBe('one')
  })
})
