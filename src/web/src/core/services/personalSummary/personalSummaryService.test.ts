import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  convertedToEurSummary,
  convertedToMkdSummary,
  emptyOctoberSummary,
  eurSummary,
  mkdSummary,
  octoberSummary,
  summaryOf,
} from '@/test/personalSummaryTestData'
import { getPersonalSummary } from './personalSummaryService'

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function bodyWith(fields: Record<string, unknown>): Record<string, unknown> {
  return { ...octoberSummary, ...fields }
}

function bodyWithMkdRow(fields: Record<string, unknown>): Record<string, unknown> {
  return bodyWith({ currencies: [{ ...mkdSummary, ...fields }] })
}

describe('getPersonalSummary', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/personal/summary?month=2026-10 for the month', async () => {
    const fetchMock = stubFetch(Response.json(octoberSummary))

    await getPersonalSummary('2026-10')

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/summary?month=2026-10')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns the months, MKD then EUR with their category rows in the order of the answer, and null converted totals', async () => {
    stubFetch(Response.json(octoberSummary))

    expect(await getPersonalSummary('2026-10')).toEqual(octoberSummary)
  })

  it('returns an empty list of currencies for two empty months', async () => {
    stubFetch(Response.json(emptyOctoberSummary))

    const summary = await getPersonalSummary('2026-10')

    expect(summary.currencies).toEqual([])
    expect(summary.convertedTotals).toBeNull()
  })

  it.each([
    ['MKD', convertedToMkdSummary],
    ['EUR', convertedToEurSummary],
  ])('returns the converted totals in %s as an object', async (_currency, converted) => {
    stubFetch(Response.json(summaryOf('2026-10', '2026-09', [mkdSummary, eurSummary], converted)))

    expect((await getPersonalSummary('2026-10')).convertedTotals).toEqual(converted)
  })

  it('keeps the previous month 1999-12 of the month 2000-01, which lies before the range of the strip', async () => {
    stubFetch(Response.json(summaryOf('2000-01', '1999-12', [])))

    expect(await getPersonalSummary('2000-01')).toMatchObject({ month: '2000-01', previousMonth: '1999-12' })
  })

  it('keeps a category row that exists only in the previous month, with 0 for this month', async () => {
    stubFetch(Response.json(octoberSummary))

    const rows = (await getPersonalSummary('2026-10')).currencies[0].categories

    expect(rows.find((row) => row.thisMinor === 0)).toMatchObject({ previousMinor: 239000 })
  })

  it('throws an error naming "convertedTotals" when the answer lacks it, because null must be sent on purpose', async () => {
    stubFetch(Response.json(withoutField(octoberSummary, 'convertedTotals')))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow('convertedTotals')
  })

  it.each(['month', 'previousMonth', 'currencies'])('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(octoberSummary, field)))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['month', 202610],
    ['month', null],
    ['previousMonth', 202609],
    ['previousMonth', null],
    ['currencies', null],
    ['currencies', 42],
    ['currencies', {}],
    ['currencies', 'MKD'],
  ])('throws an error naming "%s" when it is %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ [field]: value })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it.each([42, 'MKD', true, []])('throws an error naming "convertedTotals" when it is %j', async (value) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: value })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow('convertedTotals')
  })

  it.each([
    'currency',
    'spendingThisMinor',
    'spendingPreviousMinor',
    'incomeThisMinor',
    'incomePreviousMinor',
    'categories',
  ])('throws an error naming "%s" when a currency row lacks it', async (field) => {
    stubFetch(Response.json(bodyWith({ currencies: [withoutField(mkdSummary, field)] })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['currency', 'USD'],
    ['currency', null],
    ['currency', 'mkd'],
    ['spendingThisMinor', 12.5],
    ['spendingThisMinor', -100],
    ['spendingThisMinor', '2920000'],
    ['spendingPreviousMinor', null],
    ['spendingPreviousMinor', -1],
    ['incomeThisMinor', 0.5],
    ['incomeThisMinor', '0'],
    ['incomePreviousMinor', -100],
    ['incomePreviousMinor', null],
    ['categories', null],
    ['categories', {}],
  ])('throws an error naming "%s" when a currency row has %j', async (field, value) => {
    stubFetch(Response.json(bodyWithMkdRow({ [field]: value })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it('throws an error when a currency row is not an object', async () => {
    stubFetch(Response.json(bodyWith({ currencies: ['MKD'] })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(/currency/i)
  })

  it.each(['categoryId', 'thisMinor', 'previousMinor'])('throws an error naming "%s" when a category row lacks it', async (field) => {
    const row = mkdSummary.categories[0]
    stubFetch(Response.json(bodyWithMkdRow({ categories: [withoutField(row, field)] })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['categoryId', 42],
    ['categoryId', null],
    ['thisMinor', 1.5],
    ['thisMinor', -1],
    ['thisMinor', '720000'],
    ['previousMinor', null],
    ['previousMinor', -100],
  ])('throws an error naming "%s" when a category row has %j', async (field, value) => {
    stubFetch(Response.json(bodyWithMkdRow({ categories: [{ ...mkdSummary.categories[0], [field]: value }] })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it('throws an error when a category row is not an object', async () => {
    stubFetch(Response.json(bodyWithMkdRow({ categories: ['Food'] })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(/category/i)
  })

  it.each([
    'currency',
    'spendingThisMinor',
    'spendingPreviousMinor',
    'incomeThisMinor',
    'incomePreviousMinor',
  ])('throws an error naming "%s" when the converted totals lack it', async (field) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: withoutField(convertedToMkdSummary, field) })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['currency', 'USD'],
    ['spendingThisMinor', 0.5],
    ['spendingPreviousMinor', -1],
    ['incomeThisMinor', '4000000'],
    ['incomePreviousMinor', null],
  ])('throws an error naming "%s" when the converted totals have %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ convertedTotals: { ...convertedToMkdSummary, [field]: value } })))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(field)
  })

  it('throws an error when the answer is not an object', async () => {
    stubFetch(Response.json(['2026-10']))

    await expect(getPersonalSummary('2026-10')).rejects.toThrow(/JSON object/)
  })

  it.each([
    ['the month is refused', 400, 'PERSONAL_ENTRY_MONTH_INVALID'],
    ['the person is a guest', 403, 'GUEST_NOT_ALLOWED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(getPersonalSummary('2026-10'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getPersonalSummary('2026-10'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
