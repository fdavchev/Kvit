import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import {
  removeCategoryLimit,
  removeMonthLimit,
  setCategoryLimit,
  setMonthLimit,
} from './budgetLimitsService'

const monthUrl = '/api/personal/limits/month'
const coffeeUrl = `/api/personal/limits/categories/${coffeeCategory.id}`

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('setMonthLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/limits/month with limitMinor as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await setMonthLimit(2500000)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: monthUrl,
      method: 'PUT',
      contentType: 'application/json',
      body: { limitMinor: 2500000 },
    })
  })

  it.each([
    ['the amount is refused', 400, 'BUDGET_LIMIT_AMOUNT_INVALID'],
    ['the person is a guest', 403, 'GUEST_NOT_ALLOWED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(setMonthLimit(2500000))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(setMonthLimit(2500000))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('setCategoryLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/limits/categories/{categoryId} with limitMinor as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await setCategoryLimit(coffeeCategory.id, 450000)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: coffeeUrl,
      method: 'PUT',
      contentType: 'application/json',
      body: { limitMinor: 450000 },
    })
  })

  it.each([
    ['the amount is refused', 400, 'BUDGET_LIMIT_AMOUNT_INVALID'],
    ['the category is unknown', 404, 'CATEGORY_NOT_FOUND'],
    ['the person is a guest', 403, 'GUEST_NOT_ALLOWED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(setCategoryLimit(coffeeCategory.id, 450000))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(setCategoryLimit(coffeeCategory.id, 450000))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('removeMonthLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/personal/limits/month with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await removeMonthLimit()

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: monthUrl,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the person is a guest', async () => {
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))

    const error = await captureError(removeMonthLimit())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GUEST_NOT_ALLOWED' })
  })
})

describe('removeCategoryLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/personal/limits/categories/{categoryId} with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await removeCategoryLimit(coffeeCategory.id)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: coffeeUrl,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(removeCategoryLimit(coffeeCategory.id))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
