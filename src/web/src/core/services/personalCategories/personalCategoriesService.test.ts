import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import indexCss from '@/index.css?raw'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  archivedGymCategory,
  coffeeCategory,
  personalCategoriesWithArchived,
  personalFoodCategory,
} from '@/test/personalCategoryTestData'
import {
  archivePersonalCategory,
  createPersonalCategory,
  getPersonalCategories,
  getPersonalCategoriesWithArchived,
  personalCategoryColors,
  unarchivePersonalCategory,
  updatePersonalCategory,
} from './personalCategoriesService'

const typedFields = { name: 'Coffee', emoji: '\u{2615}', color: 'teal' } as const

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('the personal category colours', () => {
  it('are the ten colour names the server accepts, in the order of the app', () => {
    expect(personalCategoryColors).toEqual([
      'orange',
      'green',
      'yellow',
      'blue',
      'pink',
      'purple',
      'slate',
      'red',
      'teal',
      'gray',
    ])
  })

  it.each(personalCategoryColors)('has the colour token --category-%s in index.css', (color) => {
    expect(indexCss).toContain(`--category-${color}:`)
  })
})

describe('getPersonalCategories', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/personal/categories and returns the rows in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ categories: [personalFoodCategory, coffeeCategory] }))

    const categories = await getPersonalCategories()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/categories')
    expect(request.method ?? 'GET').toBe('GET')
    expect(categories).toEqual([personalFoodCategory, coffeeCategory])
  })

  it('keeps a built-in row with its key and no name, and a custom row with its name and no key', async () => {
    stubFetch(Response.json({ categories: [personalFoodCategory, coffeeCategory] }))

    const [builtIn, custom] = await getPersonalCategories()

    expect(builtIn).toMatchObject({ key: 'food', name: null, isCustom: false })
    expect(custom).toMatchObject({ key: null, name: 'Coffee', isCustom: true })
  })

  it('returns an empty list when the server has no categories', async () => {
    stubFetch(Response.json({ categories: [] }))

    expect(await getPersonalCategories()).toEqual([])
  })

  it.each(Object.keys(personalFoodCategory))('throws an error naming "%s" when a row lacks it', async (field) => {
    stubFetch(Response.json({ categories: [withoutField(personalFoodCategory, field)] }))

    await expect(getPersonalCategories()).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['emoji', null],
    ['color', 'brown'],
    ['color', null],
    ['sortOrder', '1'],
    ['isCustom', 'yes'],
    ['archived', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ categories: [{ ...personalFoodCategory, [field]: value }] }))

    await expect(getPersonalCategories()).rejects.toThrow(field)
  })

  it.each([
    ['a built-in row with a name', { ...personalFoodCategory, name: 'Food' }, 'name'],
    ['a built-in row without a key', { ...personalFoodCategory, key: null }, 'key'],
    ['a custom row with a key', { ...coffeeCategory, key: 'food' }, 'key'],
    ['a custom row without a name', { ...coffeeCategory, name: null }, 'name'],
  ])('throws an error naming the field for %s', async (_name, row, field) => {
    stubFetch(Response.json({ categories: [row] }))

    await expect(getPersonalCategories()).rejects.toThrow(field)
  })

  it('throws an error naming "categories" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getPersonalCategories()).rejects.toThrow('categories')
  })

  it.each([
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
    ['a guest', problemResponse(403, 'GUEST_NOT_ALLOWED'), 403, 'GUEST_NOT_ALLOWED'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getPersonalCategories())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('getPersonalCategoriesWithArchived', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/personal/categories?includeArchived=true and keeps the archived rows', async () => {
    const fetchMock = stubFetch(Response.json({ categories: personalCategoriesWithArchived }))

    const categories = await getPersonalCategoriesWithArchived()

    expect(sentRequest(fetchMock).url).toBe('/api/personal/categories?includeArchived=true')
    expect(categories).toEqual(personalCategoriesWithArchived)
    expect(categories.at(-1)).toMatchObject({ id: archivedGymCategory.id, archived: true })
  })
})

describe('createPersonalCategory', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/personal/categories with the name, the emoji and the colour as JSON', async () => {
    const fetchMock = stubFetch(Response.json(coffeeCategory))

    await createPersonalCategory(typedFields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/categories')
    expect(request.method).toBe('POST')
    expect(request.contentType).toBe('application/json')
    expect(request.body).toEqual(typedFields)
  })

  it('returns the new row from the answer', async () => {
    stubFetch(Response.json(coffeeCategory))

    expect(await createPersonalCategory(typedFields)).toEqual(coffeeCategory)
  })

  it('throws an error naming "name" when the answer has no name', async () => {
    stubFetch(Response.json(withoutField(coffeeCategory, 'name')))

    await expect(createPersonalCategory(typedFields)).rejects.toThrow('name')
  })

  it('rethrows the refusal with its error code', async () => {
    stubFetch(problemResponse(400, 'CATEGORY_NAME_TAKEN'))

    const error = await captureError(createPersonalCategory(typedFields))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'CATEGORY_NAME_TAKEN' })
  })
})

describe('updatePersonalCategory', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/categories/{id} with the name, the emoji and the colour', async () => {
    const fetchMock = stubFetch(noContent())

    await updatePersonalCategory(coffeeCategory.id, typedFields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/categories/${coffeeCategory.id}`)
    expect(request.method).toBe('PUT')
    expect(request.body).toEqual(typedFields)
  })

  it('rethrows the refusal with its error code', async () => {
    stubFetch(problemResponse(404, 'CATEGORY_NOT_FOUND'))

    const error = await captureError(updatePersonalCategory(coffeeCategory.id, typedFields))

    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'CATEGORY_NOT_FOUND' })
  })
})

describe('archivePersonalCategory and unarchivePersonalCategory', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('archives with POST /api/personal/categories/{id}/archive and no body', async () => {
    const fetchMock = stubFetch(noContent())

    await archivePersonalCategory(coffeeCategory.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/categories/${coffeeCategory.id}/archive`)
    expect(request.method).toBe('POST')
    expect(request.body).toBeUndefined()
  })

  it('brings back with POST /api/personal/categories/{id}/unarchive and no body', async () => {
    const fetchMock = stubFetch(noContent())

    await unarchivePersonalCategory(archivedGymCategory.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/categories/${archivedGymCategory.id}/unarchive`)
    expect(request.method).toBe('POST')
    expect(request.body).toBeUndefined()
  })

  it('rethrows a bring-back refused because an active category has the name now', async () => {
    stubFetch(problemResponse(400, 'CATEGORY_NAME_TAKEN'))

    const error = await captureError(unarchivePersonalCategory(archivedGymCategory.id))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'CATEGORY_NAME_TAKEN' })
  })
})
