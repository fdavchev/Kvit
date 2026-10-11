import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  anaPaidFilipPending,
  markoPaidFilipPending,
  settlementIds,
  settlementOf,
  settlementPathOf,
  settlementsPath,
} from '@/test/balancesTestData'
import { testGroupId } from '@/test/groupTestData'
import { filipMember, markoMember } from '@/test/memberTestData'
import {
  cancelSettlement,
  confirmSettlement,
  deleteSettlement,
  getSettlements,
  recordSettlement,
  rejectSettlement,
  restoreSettlement,
  type SettlementInput,
} from './settlementsService'

const settlementId = settlementIds.markoToFilip
const settlementPath = settlementPathOf(settlementId)

const settlementInput: SettlementInput = {
  clientRequestId: '5b1f2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d',
  fromMemberId: markoMember.id,
  toMemberId: filipMember.id,
  amountMinor: 80000,
  currency: 'MKD',
}

const settlements = [markoPaidFilipPending, anaPaidFilipPending]
const settlementFields = Object.keys(markoPaidFilipPending)

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('getSettlements', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/settlements and returns the payments in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ settlements }))

    const result = await getSettlements(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(settlementsPath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(result).toEqual(settlements)
  })

  it('returns an empty list when nobody has recorded a payment', async () => {
    stubFetch(Response.json({ settlements: [] }))

    const result = await getSettlements(testGroupId)

    expect(result).toEqual([])
  })

  it.each(['Pending', 'Confirmed', 'Rejected', 'Cancelled'] as const)('accepts the status %s', async (status) => {
    stubFetch(Response.json({ settlements: [settlementOf({ status })] }))

    const result = await getSettlements(testGroupId)

    expect(result[0].status).toBe(status)
  })

  it('keeps null for a payment that is not resolved yet', async () => {
    stubFetch(Response.json({ settlements: [markoPaidFilipPending] }))

    const result = await getSettlements(testGroupId)

    expect(result[0].resolvedAt).toBeNull()
  })

  it('keeps the time a payment was resolved', async () => {
    const resolved = settlementOf({ status: 'Confirmed', resolvedAt: '2026-10-06T09:00:00Z' })
    stubFetch(Response.json({ settlements: [resolved] }))

    const result = await getSettlements(testGroupId)

    expect(result[0]).toMatchObject({ status: 'Confirmed', resolvedAt: '2026-10-06T09:00:00Z' })
  })

  it('keeps the currency of a payment in euros', async () => {
    stubFetch(Response.json({ settlements: [settlementOf({ currency: 'EUR', amountMinor: 2000 })] }))

    const result = await getSettlements(testGroupId)

    expect(result[0]).toMatchObject({ currency: 'EUR', amountMinor: 2000 })
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['a rate limit', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getSettlements(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getSettlements(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "settlements" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getSettlements(testGroupId)).rejects.toThrow('settlements')
  })

  it.each([
    ['a text', 'Marko'],
    ['null', null],
    ['an object', {}],
  ])('throws an error naming "settlements" when the list is %s', async (_name, value) => {
    stubFetch(Response.json({ settlements: value }))

    await expect(getSettlements(testGroupId)).rejects.toThrow('settlements')
  })

  it.each(settlementFields)('throws an error naming "%s" when a payment lacks it', async (field) => {
    stubFetch(Response.json({ settlements: [withoutField(markoPaidFilipPending, field)] }))

    await expect(getSettlements(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['fromMemberId', null],
    ['fromName', null],
    ['toMemberId', 5],
    ['toName', 5],
    ['amountMinor', '80000'],
    ['amountMinor', 800.5],
    ['amountMinor', -80000],
    ['amountMinor', null],
    ['currency', 'USD'],
    ['currency', 'mkd'],
    ['status', 'Done'],
    ['status', 'pending'],
    ['status', null],
    ['recordedByUserId', 5],
    ['recordedByUserId', null],
    ['createdAt', 'yesterday'],
    ['createdAt', null],
    ['resolvedAt', 5],
    ['resolvedAt', 'later'],
  ])('throws an error naming "%s" when its value in a payment is %j', async (field, value) => {
    stubFetch(Response.json({ settlements: [{ ...markoPaidFilipPending, [field]: value }] }))

    await expect(getSettlements(testGroupId)).rejects.toThrow(field)
  })

  it('checks every payment, not only the first', async () => {
    stubFetch(Response.json({ settlements: [markoPaidFilipPending, { ...anaPaidFilipPending, status: 'Done' }] }))

    await expect(getSettlements(testGroupId)).rejects.toThrow('status')
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getSettlements(testGroupId)).rejects.toThrow(/object/)
  })
})

describe('recordSettlement', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/settlements with the whole input as JSON, including the clientRequestId, and returns the payment', async () => {
    const fetchMock = stubFetch(Response.json(markoPaidFilipPending))

    const result = await recordSettlement(testGroupId, settlementInput)

    expect(result).toEqual(markoPaidFilipPending)
    expect(sentRequest(fetchMock)).toEqual({
      url: settlementsPath,
      method: 'POST',
      contentType: 'application/json',
      body: settlementInput,
    })
  })

  it('sends exactly the five fields of the input and nothing else', async () => {
    const fetchMock = stubFetch(Response.json(markoPaidFilipPending))

    await recordSettlement(testGroupId, settlementInput)

    expect(Object.keys(sentRequest(fetchMock).body as object).sort()).toEqual(
      ['amountMinor', 'clientRequestId', 'currency', 'fromMemberId', 'toMemberId'],
    )
  })

  it('sends a payment in euros with its currency and cents', async () => {
    const fetchMock = stubFetch(Response.json(settlementOf({ currency: 'EUR', amountMinor: 2000 })))
    const inEuros: SettlementInput = { ...settlementInput, currency: 'EUR', amountMinor: 2000 }

    await recordSettlement(testGroupId, inEuros)

    expect(sentRequest(fetchMock).body).toMatchObject({ currency: 'EUR', amountMinor: 2000 })
  })

  it('returns a payment that was confirmed at once', async () => {
    const confirmed = settlementOf({ status: 'Confirmed', resolvedAt: '2026-10-06T08:00:00Z' })
    stubFetch(Response.json(confirmed))

    const result = await recordSettlement(testGroupId, settlementInput)

    expect(result).toEqual(confirmed)
  })

  it.each([
    [400, 'SETTLEMENT_AMOUNT_NOT_POSITIVE'],
    [400, 'SETTLEMENT_SAME_MEMBER'],
    [400, 'SETTLEMENT_MEMBER_NOT_CURRENT'],
    [400, 'SETTLEMENT_CURRENCY_INVALID'],
    [400, 'SETTLEMENT_AMOUNT_TOO_LARGE'],
    [400, 'SETTLEMENT_CLIENT_REQUEST_ID_USED'],
    [403, 'SETTLEMENT_NOT_ALLOWED'],
    [404, 'GROUP_NOT_FOUND'],
    [404, 'MEMBER_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(recordSettlement(testGroupId, settlementInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(recordSettlement(testGroupId, settlementInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "id" when the answer lacks the id of the new payment', async () => {
    stubFetch(Response.json(withoutField(markoPaidFilipPending, 'id')))

    await expect(recordSettlement(testGroupId, settlementInput)).rejects.toThrow('id')
  })

  it('throws an error naming "status" when the status of the new payment is not known', async () => {
    stubFetch(Response.json({ ...markoPaidFilipPending, status: 'Done' }))

    await expect(recordSettlement(testGroupId, settlementInput)).rejects.toThrow('status')
  })

  it('throws an error saying an object was expected when the answer is a list', async () => {
    stubFetch(Response.json([]))

    await expect(recordSettlement(testGroupId, settlementInput)).rejects.toThrow(/object/)
  })
})

describe.each([
  ['confirmSettlement', confirmSettlement, 'confirm'],
  ['rejectSettlement', rejectSettlement, 'reject'],
  ['cancelSettlement', cancelSettlement, 'cancel'],
  ['restoreSettlement', restoreSettlement, 'restore'],
] as const)('%s', (_name, action, pathEnd) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it(`sends POST /api/groups/{id}/settlements/{settlementId}/${pathEnd} with no body and resolves on 204`, async () => {
    const fetchMock = stubFetch(noContent())

    const result = await action(testGroupId, settlementId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `${settlementPath}/${pathEnd}`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    [400, 'SETTLEMENT_NOT_PENDING'],
    [400, 'SETTLEMENT_NOT_DELETED'],
    [403, 'SETTLEMENT_NOT_ALLOWED'],
    [404, 'SETTLEMENT_NOT_FOUND'],
    [404, 'GROUP_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(action(testGroupId, settlementId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(action(testGroupId, settlementId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('deleteSettlement', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/groups/{id}/settlements/{settlementId} with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await deleteSettlement(testGroupId, settlementId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: settlementPath,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    [400, 'SETTLEMENT_NOT_CONFIRMED'],
    [403, 'SETTLEMENT_NOT_ALLOWED'],
    [404, 'SETTLEMENT_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(deleteSettlement(testGroupId, settlementId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})
