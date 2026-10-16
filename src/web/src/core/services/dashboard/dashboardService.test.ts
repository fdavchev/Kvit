import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  anaClosingItem,
  dashboardOf,
  fullDashboard,
  markoPaymentItem,
  newUserDashboard,
  taxiAddedRow,
} from '@/test/dashboardTestData'
import { getDashboard, recordActive } from './dashboardService'

const topFields = Object.keys(fullDashboard)
const totalsFields = Object.keys(fullDashboard.totals)
const groupFields = Object.keys(fullDashboard.groups[0])
const amountFields = Object.keys(fullDashboard.totals.owed[0])
const paymentFields = Object.keys(markoPaymentItem)
const closingFields = Object.keys(anaClosingItem)
const activityFields = Object.keys(taxiAddedRow)

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

async function answerFailing(body: unknown): Promise<unknown> {
  stubFetch(Response.json(body))
  return captureError(getDashboard())
}

function messageContaining(text: string) {
  return expect.objectContaining({ message: expect.stringContaining(text) })
}

describe('getDashboard', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/dashboard and returns the dashboard as the server wrote it', async () => {
    const fetchMock = stubFetch(Response.json(fullDashboard))

    const dashboard = await getDashboard()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/dashboard')
    expect(request.method ?? 'GET').toBe('GET')
    expect(dashboard).toEqual(fullDashboard)
  })

  it('returns empty lists and no plus target for a person without groups', async () => {
    stubFetch(Response.json(newUserDashboard))

    const dashboard = await getDashboard()

    expect(dashboard).toEqual(newUserDashboard)
    expect(dashboard.plusTarget).toBeNull()
  })

  it('keeps a plus target as an object with the group id', async () => {
    stubFetch(Response.json(dashboardOf({ plusTarget: { groupId: 'g-1' } })))

    const dashboard = await getDashboard()

    expect(dashboard.plusTarget).toEqual({ groupId: 'g-1' })
  })

  it('keeps a negative balance of a group as a negative whole number', async () => {
    stubFetch(Response.json(fullDashboard))

    const dashboard = await getDashboard()

    expect(dashboard.groups[1].balances).toEqual([{ currency: 'MKD', amountMinor: -60000 }])
  })

  it('tells a payment from a closing item by the type field', async () => {
    stubFetch(Response.json(fullDashboard))

    const dashboard = await getDashboard()

    expect(dashboard.needsYou.map((item) => item.type)).toEqual(['closing', 'payment'])
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getDashboard())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getDashboard())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each([null, 'dashboard', 7, [fullDashboard]])(
    'throws a plain error when the answer is %j instead of an object',
    async (body) => {
      const error = await answerFailing(body)

      expect(error).toBeInstanceOf(Error)
      expect(error).not.toBeInstanceOf(ApiError)
    },
  )

  it.each(topFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    const error = await answerFailing(withoutField(fullDashboard, field))

    expect(error).toEqual(messageContaining(field))
  })

  it.each(['groups', 'needsYou', 'recentActivity'])('throws an error naming "%s" when it is not a list', async (field) => {
    const error = await answerFailing({ ...fullDashboard, [field]: 'none' })

    expect(error).toEqual(messageContaining(field))
  })

  it.each(totalsFields)('throws an error naming "%s" when the totals lack it', async (field) => {
    const error = await answerFailing({ ...fullDashboard, totals: withoutField(fullDashboard.totals, field) })

    expect(error).toEqual(messageContaining(field))
  })

  it.each(totalsFields)('throws an error naming "%s" when it is not a list', async (field) => {
    const error = await answerFailing({ ...fullDashboard, totals: { ...fullDashboard.totals, [field]: 600 } })

    expect(error).toEqual(messageContaining(field))
  })

  it.each(amountFields)('throws an error naming "%s" when a total lacks it', async (field) => {
    const owed = [withoutField(fullDashboard.totals.owed[0], field)]
    const error = await answerFailing({ ...fullDashboard, totals: { ...fullDashboard.totals, owed } })

    expect(error).toEqual(messageContaining(field))
  })

  it.each([1.5, '600', null])('throws an error naming "amountMinor" when a total is %j', async (value) => {
    const owe = [{ currency: 'MKD', amountMinor: value }]
    const error = await answerFailing({ ...fullDashboard, totals: { ...fullDashboard.totals, owe } })

    expect(error).toEqual(messageContaining('amountMinor'))
  })

  it('throws an error naming "currency" when a total is in a currency Kvit does not know', async () => {
    const owe = [{ currency: 'USD', amountMinor: 600 }]
    const error = await answerFailing({ ...fullDashboard, totals: { ...fullDashboard.totals, owe } })

    expect(error).toEqual(messageContaining('currency'))
  })

  it.each(groupFields)('throws an error naming "%s" when a group lacks it', async (field) => {
    const error = await answerFailing({
      ...fullDashboard,
      groups: [withoutField(fullDashboard.groups[0], field)],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it.each([
    ['kind', 'Team'],
    ['status', 'Archived'],
  ])('throws an error naming "%s" when a group has the unknown value %s', async (field, value) => {
    const error = await answerFailing({
      ...fullDashboard,
      groups: [{ ...fullDashboard.groups[0], [field]: value }],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it.each([1.5, '1850', null])('throws an error naming "amountMinor" when a group balance is %j', async (value) => {
    const error = await answerFailing({
      ...fullDashboard,
      groups: [{ ...fullDashboard.groups[0], balances: [{ currency: 'MKD', amountMinor: value }] }],
    })

    expect(error).toEqual(messageContaining('amountMinor'))
  })

  it('throws an error naming "balances" when the balances of a group are not a list', async () => {
    const error = await answerFailing({
      ...fullDashboard,
      groups: [{ ...fullDashboard.groups[0], balances: 'none' }],
    })

    expect(error).toEqual(messageContaining('balances'))
  })

  it.each(paymentFields)('throws an error naming "%s" when a payment item lacks it', async (field) => {
    const error = await answerFailing({
      ...fullDashboard,
      needsYou: [withoutField(markoPaymentItem, field)],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it.each(closingFields)('throws an error naming "%s" when a closing item lacks it', async (field) => {
    const error = await answerFailing({
      ...fullDashboard,
      needsYou: [withoutField(anaClosingItem, field)],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it('throws an error naming the type when a needs-you item has a type Kvit does not know', async () => {
    const error = await answerFailing({
      ...fullDashboard,
      needsYou: [{ ...markoPaymentItem, type: 'refund' }],
    })

    expect(error).toEqual(messageContaining('type'))
    expect(error).toEqual(messageContaining('refund'))
  })

  it.each([1.5, '600'])('throws an error naming "amountMinor" when a payment amount is %j', async (value) => {
    const error = await answerFailing({
      ...fullDashboard,
      needsYou: [{ ...markoPaymentItem, amountMinor: value }],
    })

    expect(error).toEqual(messageContaining('amountMinor'))
  })

  it('throws an error naming "currency" when a payment is in a currency Kvit does not know', async () => {
    const error = await answerFailing({
      ...fullDashboard,
      needsYou: [{ ...markoPaymentItem, currency: 'USD' }],
    })

    expect(error).toEqual(messageContaining('currency'))
  })

  it.each(activityFields)('throws an error naming "%s" when an activity row lacks it', async (field) => {
    const error = await answerFailing({
      ...fullDashboard,
      recentActivity: [withoutField(taxiAddedRow, field)],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it.each(['groupId', 'groupName'])('throws an error naming "%s" when an activity row has it as a number', async (field) => {
    const error = await answerFailing({
      ...fullDashboard,
      recentActivity: [{ ...taxiAddedRow, [field]: 7 }],
    })

    expect(error).toEqual(messageContaining(field))
  })

  it.each([5, 'group-1', []])('throws an error naming "plusTarget" when it is %j', async (value) => {
    const error = await answerFailing({ ...fullDashboard, plusTarget: value })

    expect(error).toEqual(messageContaining('plusTarget'))
  })

  it('throws an error naming "groupId" when the plus target lacks it', async () => {
    const error = await answerFailing({ ...fullDashboard, plusTarget: {} })

    expect(error).toEqual(messageContaining('groupId'))
  })
})

describe('recordActive', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/me/active with no body', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await recordActive()

    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/me/active',
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('resolves with nothing when the server answers 204', async () => {
    stubFetch(new Response(null, { status: 204 }))

    await expect(recordActive()).resolves.toBeUndefined()
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(recordActive())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(recordActive())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
