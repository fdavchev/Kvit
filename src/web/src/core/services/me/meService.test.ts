import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { changeLanguage, getMe } from './meService'

const meFields = Object.keys(testMe)

describe('getMe', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/me and returns the signed-in person', async () => {
    const fetchMock = stubFetch(Response.json(testMe))

    const me = await getMe()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/me')
    expect(request.method ?? 'GET').toBe('GET')
    expect(me).toEqual(testMe)
  })

  it('keeps the picture address of an account with a Google picture', async () => {
    const pictureUrl = 'https://lh3.googleusercontent.com/a/filip-picture=s96-c'
    stubFetch(Response.json({ ...testMe, pictureUrl }))

    const me = await getMe()

    expect(me).toMatchObject({ pictureUrl })
  })

  it('keeps null for an account without a picture', async () => {
    stubFetch(Response.json({ ...testMe, pictureUrl: null }))

    const me = await getMe()

    expect(me).toMatchObject({ pictureUrl: null })
  })

  it.each([5, true, {}])('throws an error naming "pictureUrl" when its value is %j', async (value) => {
    stubFetch(Response.json({ ...testMe, pictureUrl: value }))

    await expect(getMe()).rejects.toThrow('pictureUrl')
  })

  it('returns null when the server answers 401 because nobody is signed in', async () => {
    stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))

    const me = await getMe()

    expect(me).toBeNull()
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    [
      'a forced password change',
      problemResponse(403, 'AUTH_MUST_CHANGE_PASSWORD'),
      403,
      'AUTH_MUST_CHANGE_PASSWORD',
    ],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getMe())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getMe())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(meFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    const incomplete = Object.fromEntries(
      Object.entries(testMe).filter(([name]) => name !== field),
    )
    stubFetch(Response.json(incomplete))

    await expect(getMe()).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['displayName', null],
    ['email', 7],
    ['language', 'fr'],
    ['language', 5],
    ['timeZone', false],
    ['mustChangePassword', 'false'],
    ['hasPassword', 'true'],
    ['hasPassword', 1],
    ['hasPassword', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...testMe, [field]: value }))

    await expect(getMe()).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getMe()).rejects.toThrow(/object/)
  })
})

describe('changeLanguage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/me/language with the language as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await changeLanguage('mk')

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/me/language',
      method: 'PUT',
      contentType: 'application/json',
      body: { language: 'mk' },
    })
  })

  it('rethrows the ApiError unchanged when the server refuses the language', async () => {
    stubFetch(problemResponse(400, 'LANGUAGE_INVALID'))

    const error = await captureError(changeLanguage('mk'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'LANGUAGE_INVALID' })
  })
})
