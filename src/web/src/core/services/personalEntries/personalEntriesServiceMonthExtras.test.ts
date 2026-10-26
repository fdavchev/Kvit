import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubFetch } from '@/test/apiTestHelpers'
import {
  convertedToMkd,
  euroTaxiShare,
  foodLimitRow,
  hotelShare,
  octoberWithGroupShares,
  octoberWithGroupSharesConvertedToEur,
  octoberWithGroupSharesConvertedToMkd,
  wholeMonthLimitRow,
} from '@/test/groupShareTestData'
import { emptyMonth, monthAnswerBody, noLimits, octoberMonth } from '@/test/personalEntryTestData'
import { getPersonalEntries } from './personalEntriesService'

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function bodyWith(fields: Record<string, unknown>): Record<string, unknown> {
  return { ...monthAnswerBody(emptyMonth('2026-10')), ...fields }
}

const monthWithLimits = {
  ...octoberWithGroupShares,
  limits: { month: wholeMonthLimitRow, categories: [foodLimitRow] },
}

async function messageOf(promise: Promise<unknown>): Promise<string> {
  try {
    await promise
  } catch (error) {
    if (error instanceof Error) {
      return error.message
    }
  }
  throw new Error('Expected the call to throw an Error')
}

describe('getPersonalEntries, the group shares', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the group shares in the order of the answer, each in its own currency', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberWithGroupShares)))

    const month = await getPersonalEntries('2026-10')

    expect(month.groupShares).toEqual(octoberWithGroupShares.groupShares)
    expect(month.groupShares.map((share) => share.currency)).toEqual(['MKD', 'MKD', 'MKD', 'EUR', 'MKD'])
  })

  it('keeps a share with no title and no category as nulls', async () => {
    const share = { ...hotelShare, title: null, categoryId: null }
    stubFetch(Response.json(bodyWith({ groupShares: [share] })))

    expect((await getPersonalEntries('2026-10')).groupShares).toEqual([share])
  })

  it('returns an empty list when the month has no group shares', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    expect((await getPersonalEntries('2026-10')).groupShares).toEqual([])
  })

  it('throws an error naming "groupShares" when the answer lacks it', async () => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), 'groupShares')))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('groupShares')
  })

  it.each([null, 42, 'none', {}])('throws an error naming "groupShares" when it is %j instead of a list', async (value) => {
    stubFetch(Response.json(bodyWith({ groupShares: value })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('groupShares')
  })

  it.each(Object.keys(hotelShare))('throws an error naming "%s" when a group share lacks it', async (field) => {
    stubFetch(Response.json(bodyWith({ groupShares: [withoutField(hotelShare, field)] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['expenseId', 42],
    ['groupId', null],
    ['groupName', null],
    ['title', 42],
    ['categoryId', 42],
    ['entryDate', 'yesterday'],
    ['currency', 'USD'],
    ['shareMinor', 12.5],
    ['shareMinor', -1],
    ['shareMinor', '480000'],
  ])('throws an error naming "%s" when a group share has %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ groupShares: [{ ...euroTaxiShare, [field]: value }] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it('throws an error when a group share is not an object', async () => {
    stubFetch(Response.json(bodyWith({ groupShares: ['Hotel'] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(/group share/i)
  })
})

describe('getPersonalEntries, the converted totals', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    ['MKD', octoberWithGroupSharesConvertedToMkd],
    ['EUR', octoberWithGroupSharesConvertedToEur],
  ])('returns the converted totals in %s as an object', async (_currency, month) => {
    stubFetch(Response.json(monthAnswerBody(month)))

    expect((await getPersonalEntries('2026-10')).convertedTotals).toEqual(month.convertedTotals)
  })

  it('returns null when the budget total currency is off, so the server sent null', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    expect(monthAnswerBody(octoberMonth).convertedTotals).toBeNull()
    expect((await getPersonalEntries('2026-10')).convertedTotals).toBeNull()
  })

  it('keeps a negative Left', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberWithGroupSharesConvertedToMkd)))

    expect((await getPersonalEntries('2026-10')).convertedTotals?.leftMinor).toBe(-755100)
  })

  it('throws an error naming "convertedTotals" when the answer lacks it, because null must be sent on purpose', async () => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), 'convertedTotals')))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('convertedTotals')
  })

  it.each([42, 'MKD', true, []])('throws an error naming "convertedTotals" when it is %j', async (value) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: value })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('convertedTotals')
  })

  it.each(Object.keys(convertedToMkd))('throws an error naming "%s" when the converted totals lack it', async (field) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: withoutField(convertedToMkd, field) })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['currency', 'USD'],
    ['currency', null],
    ['spendingMinor', -1],
    ['spendingMinor', 1.5],
    ['incomeMinor', '0'],
    ['incomeMinor', -5],
    ['groupSharesMinor', -1],
    ['groupSharesMinor', null],
    ['leftMinor', 1.5],
    ['leftMinor', '10'],
    ['leftMinor', null],
  ])('throws an error naming "%s" when the converted totals have %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: { ...convertedToMkd, [field]: value } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })
})

describe('getPersonalEntries, the limits', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns no limits as a null month limit and an empty list', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    expect((await getPersonalEntries('2026-10')).limits).toEqual(noLimits)
  })

  it('returns the limit of the whole month and the limits of the categories', async () => {
    stubFetch(Response.json(monthAnswerBody(monthWithLimits)))

    const { limits } = await getPersonalEntries('2026-10')

    expect(limits.month).toEqual(wholeMonthLimitRow)
    expect(limits.categories).toEqual([foodLimitRow])
  })

  it('keeps a month with a category limit and no limit for the whole month', async () => {
    stubFetch(Response.json(bodyWith({ limits: { month: null, categories: [foodLimitRow] } })))

    const { limits } = await getPersonalEntries('2026-10')

    expect(limits.month).toBeNull()
    expect(limits.categories).toEqual([foodLimitRow])
  })

  it('throws an error naming "limits" when the answer lacks it', async () => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), 'limits')))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('limits')
  })

  it.each([null, 42, 'none', []])('throws an error naming "limits" when it is %j instead of an object', async (value) => {
    stubFetch(Response.json(bodyWith({ limits: value })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('limits')
  })

  it('throws an error naming the month limit when "month" is missing from the limits', async () => {
    stubFetch(Response.json(bodyWith({ limits: { categories: [] } })))

    const message = await messageOf(getPersonalEntries('2026-10'))

    expect(message).toMatch(/month/)
    expect(message).toMatch(/limit/i)
  })

  it('throws an error naming "categories" when the limits lack the list', async () => {
    stubFetch(Response.json(bodyWith({ limits: { month: null } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('categories')
  })

  it.each([42, 'x', null])('throws an error naming "categories" when the limits have %j instead of a list', async (value) => {
    stubFetch(Response.json(bodyWith({ limits: { month: null, categories: value } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('categories')
  })

  it.each(Object.keys(foodLimitRow))('throws an error naming "%s" when the month limit lacks it', async (field) => {
    stubFetch(Response.json(bodyWith({ limits: { month: withoutField(wholeMonthLimitRow, field), categories: [] } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(Object.keys(foodLimitRow))('throws an error naming "%s" when a category limit lacks it', async (field) => {
    stubFetch(Response.json(bodyWith({ limits: { month: null, categories: [withoutField(foodLimitRow, field)] } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['categoryId', 42],
    ['limitMinor', -1],
    ['limitMinor', 1.5],
    ['spentMinor', '100'],
    ['spentMinor', -1],
    ['overMinor', -1],
    ['overMinor', null],
    ['includedEurMinor', 0.5],
    ['includedEurMinor', null],
  ])('throws an error naming "%s" when a category limit has %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ limits: { month: null, categories: [{ ...foodLimitRow, [field]: value }] } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['limitMinor', -1],
    ['spentMinor', 2.5],
    ['overMinor', 'x'],
    ['includedEurMinor', -3],
  ])('throws an error naming "%s" when the month limit has %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ limits: { month: { ...wholeMonthLimitRow, [field]: value }, categories: [] } })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })
})
