import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { foodCategory, testCategories } from '@/test/expenseTestData'
import { getCategories } from './categoriesService'

const categoryFields = Object.keys(foodCategory)

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

describe('getCategories', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/categories and returns the ten categories in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ categories: testCategories }))

    const categories = await getCategories()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/categories')
    expect(request.method ?? 'GET').toBe('GET')
    expect(categories).toEqual(testCategories)
    expect(categories).toHaveLength(10)
  })

  it('returns an empty list when the server has no categories', async () => {
    stubFetch(Response.json({ categories: [] }))

    const categories = await getCategories()

    expect(categories).toEqual([])
  })

  it('keeps the key, the emoji and the colour name of every category', async () => {
    stubFetch(Response.json({ categories: testCategories }))

    const categories = await getCategories()

    expect(categories.map((category) => category.key)).toEqual([
      'food',
      'groceries',
      'transport',
      'accommodation',
      'fun',
      'shopping',
      'bills',
      'health',
      'gifts',
      'other',
    ])
    expect(categories[0]).toMatchObject({ emoji: foodCategory.emoji, color: 'orange' })
  })

  it.each([
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
    ['a server error', new Response(null, { status: 500 }), 500, null],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getCategories())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getCategories())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "categories" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getCategories()).rejects.toThrow('categories')
  })

  it.each([
    ['a text', 'Food'],
    ['null', null],
    ['an object', {}],
  ])('throws an error naming "categories" when the list is %s', async (_name, value) => {
    stubFetch(Response.json({ categories: value }))

    await expect(getCategories()).rejects.toThrow('categories')
  })

  it.each(categoryFields)('throws an error naming "%s" when a category lacks it', async (field) => {
    stubFetch(Response.json({ categories: [withoutField(foodCategory, field)] }))

    await expect(getCategories()).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['key', 5],
    ['key', null],
    ['emoji', 7],
    ['color', null],
    ['color', 3],
    ['sortOrder', '1'],
    ['sortOrder', null],
    ['sortOrder', 1.5],
  ])('throws an error naming "%s" when its value in a category is %j', async (field, value) => {
    stubFetch(Response.json({ categories: [{ ...foodCategory, [field]: value }] }))

    await expect(getCategories()).rejects.toThrow(field)
  })

  it('checks every category, not only the first', async () => {
    stubFetch(Response.json({ categories: [foodCategory, { ...testCategories[1], sortOrder: 'two' }] }))

    await expect(getCategories()).rejects.toThrow('sortOrder')
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getCategories()).rejects.toThrow(/object/)
  })
})
