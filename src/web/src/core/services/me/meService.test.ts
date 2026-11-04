import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { acceptTerms, changeBudgetTotalCurrency, changeLanguage, getMe } from './meService'

const termsFields = ['termsVersion', 'termsUpToDate', 'termsUpdateIsMaterial']
const meFields = Object.keys(testMe).filter((field) => !termsFields.includes(field))

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

  it('keeps a null email and isGuest true for a guest', async () => {
    stubFetch(Response.json({ ...testMe, email: null, isGuest: true }))

    const me = await getMe()

    expect(me).toMatchObject({ email: null, isGuest: true })
  })

  it.each([true, false])('keeps isAdmin %j as the answer sent it', async (isAdmin) => {
    stubFetch(Response.json({ ...testMe, isAdmin }))

    const me = await getMe()

    expect(me).toMatchObject({ isAdmin })
  })

  it.each(['2026-11-04', null])('keeps termsVersion %j as the answer sent it', async (termsVersion) => {
    stubFetch(Response.json({ ...testMe, termsVersion }))

    const me = await getMe()

    expect(me).toMatchObject({ termsVersion })
  })

  it.each([true, false])('keeps termsUpToDate %j as the answer sent it', async (termsUpToDate) => {
    stubFetch(Response.json({ ...testMe, termsUpToDate }))

    const me = await getMe()

    expect(me).toMatchObject({ termsUpToDate })
  })

  it.each([true, false])('keeps termsUpdateIsMaterial %j as the answer sent it', async (termsUpdateIsMaterial) => {
    stubFetch(Response.json({ ...testMe, termsUpdateIsMaterial }))

    const me = await getMe()

    expect(me).toMatchObject({ termsUpdateIsMaterial })
  })

  it('reads a person who must agree to a material change as it was sent', async () => {
    const body = { ...testMe, termsVersion: null, termsUpToDate: false, termsUpdateIsMaterial: true }
    stubFetch(Response.json(body))

    const me = await getMe()

    expect(me).toEqual(body)
  })

  it('reads an older answer without any terms field as: no version, up to date, not material, so no notice is shown before a fresh answer', async () => {
    const olderAnswer = Object.fromEntries(
      Object.entries(testMe).filter(([name]) => !termsFields.includes(name)),
    )
    stubFetch(Response.json(olderAnswer))

    const me = await getMe()

    expect(me).toEqual({
      ...olderAnswer,
      termsVersion: null,
      termsUpToDate: true,
      termsUpdateIsMaterial: false,
    })
  })

  it.each(termsFields)('fills only "%s" when it is the one field missing from the answer', async (field) => {
    const withoutField = Object.fromEntries(
      Object.entries({ ...testMe, termsVersion: '2026-01-01', termsUpToDate: false, termsUpdateIsMaterial: true }).filter(
        ([name]) => name !== field,
      ),
    )
    const defaults = { termsVersion: null, termsUpToDate: true, termsUpdateIsMaterial: false }
    stubFetch(Response.json(withoutField))

    const me = await getMe()

    expect(me).toEqual({
      ...testMe,
      termsVersion: '2026-01-01',
      termsUpToDate: false,
      termsUpdateIsMaterial: true,
      [field]: defaults[field as keyof typeof defaults],
    })
  })

  it.each(['MKD', 'EUR', null])('keeps budgetTotalCurrency %j as the answer sent it', async (budgetTotalCurrency) => {
    stubFetch(Response.json({ ...testMe, budgetTotalCurrency }))

    const me = await getMe()

    expect(me).toMatchObject({ budgetTotalCurrency })
  })

  it('keeps null budgetTotalCurrency for a guest', async () => {
    stubFetch(Response.json({ ...testMe, email: null, isGuest: true, budgetTotalCurrency: null }))

    const me = await getMe()

    expect(me).toMatchObject({ isGuest: true, budgetTotalCurrency: null })
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
    ['email', {}],
    ['isGuest', 'true'],
    ['isGuest', null],
    ['isAdmin', 'true'],
    ['isAdmin', 1],
    ['isAdmin', null],
    ['isAdmin', {}],
    ['termsVersion', 5],
    ['termsVersion', true],
    ['termsVersion', {}],
    ['termsUpToDate', 'true'],
    ['termsUpToDate', 1],
    ['termsUpToDate', null],
    ['termsUpdateIsMaterial', 'false'],
    ['termsUpdateIsMaterial', 0],
    ['termsUpdateIsMaterial', null],
    ['budgetTotalCurrency', 'USD'],
    ['budgetTotalCurrency', 'mkd'],
    ['budgetTotalCurrency', ''],
    ['budgetTotalCurrency', 5],
    ['budgetTotalCurrency', false],
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

describe('acceptTerms', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/me/terms/accept and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await acceptTerms()

    expect(result).toBeUndefined()
    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/me/terms/accept')
    expect(request.method).toBe('POST')
  })

  it.each([
    ['nobody is signed in', 401, 'AUTH_NOT_SIGNED_IN'],
    ['the server is busy', 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(acceptTerms())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(acceptTerms())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
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

describe('changeBudgetTotalCurrency', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(['MKD', 'EUR', null] as const)('sends PUT /api/me/budget-currency with budgetTotalCurrency %j as JSON and resolves on 204', async (budgetTotalCurrency) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await changeBudgetTotalCurrency(budgetTotalCurrency)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/me/budget-currency',
      method: 'PUT',
      contentType: 'application/json',
      body: { budgetTotalCurrency },
    })
  })

  it.each([
    ['the server refuses the currency', 400, 'ME_BUDGET_CURRENCY_INVALID'],
    ['the person is a guest', 403, 'GUEST_NOT_ALLOWED'],
    ['nobody is signed in', 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(changeBudgetTotalCurrency('EUR'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(changeBudgetTotalCurrency('EUR'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
