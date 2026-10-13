import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  balancesPath,
  greeceTripBalances,
  greeceTripInTwoCurrencies,
  noExpensesYetBalances,
} from '@/test/balancesTestData'
import { testGroupId } from '@/test/groupTestData'
import { getBalances } from './balancesService'

const answerFields = Object.keys(greeceTripBalances)
const currencyFields = Object.keys(greeceTripBalances.currencies[0])
const memberFields = Object.keys(greeceTripBalances.currencies[0].balances[0])
const paymentFields = Object.keys(greeceTripBalances.currencies[0].payments[0])

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function withFirstCurrency(changes: object): object {
  return { ...greeceTripBalances, currencies: [{ ...greeceTripBalances.currencies[0], ...changes }] }
}

function withFirstBalance(changes: object): object {
  const [currency] = greeceTripBalances.currencies
  return withFirstCurrency({ balances: [{ ...currency.balances[0], ...changes }] })
}

function withFirstPayment(changes: object): object {
  const [currency] = greeceTripBalances.currencies
  return withFirstCurrency({ payments: [{ ...currency.payments[0], ...changes }] })
}

describe('getBalances', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/balances and returns the answer unchanged', async () => {
    const fetchMock = stubFetch(Response.json(greeceTripBalances))

    const balances = await getBalances(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(balancesPath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(balances).toEqual(greeceTripBalances)
  })

  it('keeps the currencies in the order of the answer', async () => {
    stubFetch(Response.json(greeceTripInTwoCurrencies))

    const balances = await getBalances(testGroupId)

    expect(balances.currencies.map((currency) => currency.currency)).toEqual(['MKD', 'EUR'])
  })

  it('keeps the members in the order of the answer and the payments in the order of the answer', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const [mkd] = (await getBalances(testGroupId)).currencies

    expect(mkd.balances.map((row) => row.name)).toEqual(['Filip', 'Ana', 'Marko', 'Grandma'])
    expect(mkd.payments.map((payment) => payment.amountMinor)).toEqual([80000, 70000])
  })

  it('returns no currencies for a group without expenses', async () => {
    stubFetch(Response.json(noExpensesYetBalances))

    const balances = await getBalances(testGroupId)

    expect(balances).toEqual({ isEveryoneKvit: true, currencies: [] })
  })

  it('keeps a negative balance as a negative whole number', async () => {
    stubFetch(Response.json(greeceTripBalances))

    const [mkd] = (await getBalances(testGroupId)).currencies

    expect(mkd.balances.map((row) => row.balanceMinor)).toEqual([150000, -70000, -80000, 0])
  })

  it('keeps isCurrent false for a person who is no longer in the group', async () => {
    stubFetch(Response.json(withFirstBalance({ isCurrent: false })))

    const [mkd] = (await getBalances(testGroupId)).currencies

    expect(mkd.balances[0].isCurrent).toBe(false)
  })

  it('keeps a currency that has no payments with an empty list of payments', async () => {
    stubFetch(Response.json(withFirstCurrency({ payments: [] })))

    const [mkd] = (await getBalances(testGroupId)).currencies

    expect(mkd.payments).toEqual([])
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['a rate limit', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
    ['a session that ended', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getBalances(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getBalances(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(answerFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(greeceTripBalances, field)))

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['isEveryoneKvit', 'yes'],
    ['isEveryoneKvit', 1],
    ['isEveryoneKvit', null],
    ['currencies', 'MKD'],
    ['currencies', null],
    ['currencies', {}],
  ])('throws an error naming "%s" when its value in the answer is %j', async (field, value) => {
    stubFetch(Response.json({ ...greeceTripBalances, [field]: value }))

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each(currencyFields)('throws an error naming "%s" when a currency lacks it', async (field) => {
    stubFetch(
      Response.json({
        ...greeceTripBalances,
        currencies: [withoutField(greeceTripBalances.currencies[0], field)],
      }),
    )

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['currency', 'USD'],
    ['currency', 'mkd'],
    ['currency', null],
    ['balances', 'Filip'],
    ['balances', null],
    ['payments', 'Marko'],
    ['payments', null],
  ])('throws an error naming "%s" when its value in a currency is %j', async (field, value) => {
    stubFetch(Response.json(withFirstCurrency({ [field]: value })))

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each(memberFields)('throws an error naming "%s" when a member lacks it', async (field) => {
    const [currency] = greeceTripBalances.currencies
    stubFetch(
      Response.json(withFirstCurrency({ balances: [withoutField(currency.balances[0], field)] })),
    )

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['memberId', 5],
    ['memberId', null],
    ['name', null],
    ['name', 5],
    ['balanceMinor', '150000'],
    ['balanceMinor', 1500.5],
    ['balanceMinor', null],
    ['balanceMinor', 2 ** 53],
    ['isCurrent', 'true'],
    ['isCurrent', 1],
  ])('throws an error naming "%s" when its value in a member is %j', async (field, value) => {
    stubFetch(Response.json(withFirstBalance({ [field]: value })))

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each(paymentFields)('throws an error naming "%s" when a payment lacks it', async (field) => {
    const [currency] = greeceTripBalances.currencies
    stubFetch(
      Response.json(withFirstCurrency({ payments: [withoutField(currency.payments[0], field)] })),
    )

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['fromMemberId', null],
    ['fromMemberId', 5],
    ['toMemberId', null],
    ['toMemberId', 5],
    ['amountMinor', '80000'],
    ['amountMinor', 800.5],
    ['amountMinor', null],
    ['amountMinor', -80000],
  ])('throws an error naming "%s" when its value in a payment is %j', async (field, value) => {
    stubFetch(Response.json(withFirstPayment({ [field]: value })))

    await expect(getBalances(testGroupId)).rejects.toThrow(field)
  })

  it('checks every member and every payment, not only the first', async () => {
    const [currency] = greeceTripBalances.currencies
    stubFetch(
      Response.json(
        withFirstCurrency({
          balances: [currency.balances[0], { ...currency.balances[1], balanceMinor: '70000' }],
        }),
      ),
    )

    await expect(getBalances(testGroupId)).rejects.toThrow('balanceMinor')
  })

  it('checks every currency, not only the first', async () => {
    stubFetch(
      Response.json({
        ...greeceTripInTwoCurrencies,
        currencies: [greeceTripInTwoCurrencies.currencies[0], { ...greeceTripInTwoCurrencies.currencies[1], currency: 'USD' }],
      }),
    )

    await expect(getBalances(testGroupId)).rejects.toThrow('currency')
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getBalances(testGroupId)).rejects.toThrow(/object/)
  })
})
