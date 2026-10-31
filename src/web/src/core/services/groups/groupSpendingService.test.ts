import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { foodCategory, transportCategory } from '@/test/expenseTestData'
import { testGroupId } from '@/test/groupTestData'
import {
  mkdAndEurSpending,
  noSpendingYet,
  planFields,
  spendingPath,
  spendingWithPlan,
} from '@/test/groupSpendingTestData'
import { getGroupSpending } from './groupSpendingService'

const answerFields = Object.keys(spendingWithPlan)
const currencyFields = Object.keys(spendingWithPlan.currencies[0])
const categoryFields = Object.keys(spendingWithPlan.currencies[0].categories[0])

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function withFirstCurrency(changes: object): object {
  return { ...spendingWithPlan, currencies: [{ ...spendingWithPlan.currencies[0], ...changes }] }
}

function withFirstCategory(changes: object): object {
  const [currency] = spendingWithPlan.currencies
  return withFirstCurrency({ categories: [{ ...currency.categories[0], ...changes }] })
}

function withPlan(changes: object): object {
  return { ...spendingWithPlan, plan: { ...spendingWithPlan.plan, ...changes } }
}

function withPlanWithout(field: string): object {
  return { ...spendingWithPlan, plan: withoutField(spendingWithPlan.plan, field) }
}

describe('getGroupSpending', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/spending and returns the answer unchanged', async () => {
    const fetchMock = stubFetch(Response.json(spendingWithPlan))

    const spending = await getGroupSpending(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(spendingPath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(spending).toEqual(spendingWithPlan)
  })

  it('returns no currencies and no plan for a group without expenses', async () => {
    stubFetch(Response.json(noSpendingYet))

    const spending = await getGroupSpending(testGroupId)

    expect(spending).toEqual({ currencies: [], plan: null })
  })

  it('keeps the currencies and the categories in the order of the answer', async () => {
    stubFetch(Response.json(mkdAndEurSpending))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.currencies.map((currency) => currency.currency)).toEqual(['MKD', 'EUR'])
    expect(spending.currencies[0].categories.map((row) => row.categoryId)).toEqual([
      foodCategory.id,
      transportCategory.id,
      null,
    ])
  })

  it('accepts a category row with the category id null', async () => {
    stubFetch(Response.json(withFirstCategory({ categoryId: null })))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.currencies[0].categories[0].categoryId).toBeNull()
  })

  it('accepts a currency with no category rows', async () => {
    stubFetch(Response.json(withFirstCurrency({ categories: [] })))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.currencies[0].categories).toEqual([])
  })

  it('accepts a plan with the amounts 0 where 0 is allowed', async () => {
    const zeros = { spentMinor: 0, perPersonMinor: 0, overMinor: 0, includedOtherCurrencyMinor: 0 }
    stubFetch(Response.json(withPlan(zeros)))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.plan).toMatchObject(zeros)
  })

  it('accepts a plan of a group with one member', async () => {
    stubFetch(Response.json(withPlan({ memberCount: 1 })))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.plan).toMatchObject({ memberCount: 1 })
  })

  it('returns the member count of the plan as the answer wrote it', async () => {
    stubFetch(Response.json(withPlan({ memberCount: 7 })))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.plan).toMatchObject({ memberCount: 7 })
  })

  it('accepts a plan that is over, in euros and with converted expenses', async () => {
    const overPlan = {
      amountMinor: 10000,
      currency: 'EUR',
      spentMinor: 12500,
      perPersonMinor: 2500,
      overMinor: 2500,
      includedOtherCurrencyMinor: 6156100,
      memberCount: 4,
    }
    stubFetch(Response.json(withPlan(overPlan)))

    const spending = await getGroupSpending(testGroupId)

    expect(spending.plan).toEqual(overPlan)
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getGroupSpending(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getGroupSpending(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(answerFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(spendingWithPlan, field)))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each(currencyFields)('throws an error naming "%s" when a currency lacks it', async (field) => {
    stubFetch(
      Response.json({
        ...spendingWithPlan,
        currencies: [withoutField(spendingWithPlan.currencies[0], field)],
      }),
    )

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each(categoryFields)('throws an error naming "%s" when a category row lacks it', async (field) => {
    stubFetch(
      Response.json(withFirstCurrency({ categories: [withoutField(spendingWithPlan.currencies[0].categories[0], field)] })),
    )

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each(planFields)('throws an error naming "%s" when the plan lacks it', async (field) => {
    stubFetch(Response.json(withPlanWithout(field)))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['currencies', 'MKD'],
    ['currencies', null],
    ['currencies', {}],
    ['plan', 'none'],
    ['plan', 7],
    ['plan', []],
  ])('throws an error naming "%s" when its value in the answer is %j', async (field, value) => {
    stubFetch(Response.json({ ...spendingWithPlan, [field]: value }))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['currency', 'USD'],
    ['currency', 'mkd'],
    ['currency', null],
    ['currency', 5],
    ['totalMinor', '135000'],
    ['totalMinor', null],
    ['totalMinor', -1],
    ['totalMinor', 1.5],
    ['categories', 'food'],
    ['categories', null],
    ['categories', {}],
  ])('throws an error naming "%s" when its value in a currency is %j', async (field, value) => {
    stubFetch(Response.json(withFirstCurrency({ [field]: value })))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['categoryId', 5],
    ['categoryId', true],
    ['categoryId', {}],
    ['totalMinor', '80000'],
    ['totalMinor', null],
    ['totalMinor', -1],
    ['totalMinor', 0.5],
  ])('throws an error naming "%s" when its value in a category row is %j', async (field, value) => {
    stubFetch(Response.json(withFirstCategory({ [field]: value })))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['amountMinor', 0],
    ['amountMinor', -100],
    ['amountMinor', 1.5],
    ['amountMinor', '200000'],
    ['amountMinor', null],
    ['currency', 'USD'],
    ['currency', 'eur'],
    ['currency', null],
    ['spentMinor', -1],
    ['spentMinor', 1.5],
    ['spentMinor', '135000'],
    ['spentMinor', null],
    ['perPersonMinor', -1],
    ['perPersonMinor', 0.5],
    ['perPersonMinor', '1'],
    ['perPersonMinor', null],
    ['overMinor', -1],
    ['overMinor', 2.5],
    ['overMinor', 'none'],
    ['overMinor', null],
    ['includedOtherCurrencyMinor', -1],
    ['includedOtherCurrencyMinor', 1.5],
    ['includedOtherCurrencyMinor', '0'],
    ['includedOtherCurrencyMinor', null],
    ['memberCount', 0],
    ['memberCount', -1],
    ['memberCount', 1.5],
    ['memberCount', '3'],
    ['memberCount', true],
    ['memberCount', null],
  ])('throws an error naming "%s" when its value in the plan is %j', async (field, value) => {
    stubFetch(Response.json(withPlan({ [field]: value })))

    await expect(getGroupSpending(testGroupId)).rejects.toThrow(field)
  })
})
