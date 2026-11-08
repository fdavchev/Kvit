import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { deleteMyAccount } from './meService'

describe('the delete my account endpoint', () => {
  it('is /api/me/delete', () => {
    expect(endpoints.meDelete).toBe('/api/me/delete')
  })
})

describe('deleteMyAccount', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(['KeepName', 'FormerMember'] as const)('sends POST /api/me/delete with nameChoice %s as JSON and resolves on 204', async (nameChoice) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await deleteMyAccount(nameChoice)

    expect(result).toBeUndefined()
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/me/delete',
      method: 'POST',
      contentType: 'application/json',
      body: { nameChoice },
    })
  })

  it('sends the session cookie', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await deleteMyAccount('KeepName')

    expect(fetchMock.mock.calls[0][1]?.credentials).toBe('same-origin')
  })

  it.each([
    ['the server refuses the name choice', 400, 'ACCOUNT_DELETE_NAME_CHOICE_INVALID'],
    ['nobody is signed in', 401, 'AUTH_NOT_SIGNED_IN'],
    ['the person must change the password first', 403, 'AUTH_MUST_CHANGE_PASSWORD'],
    ['the server is busy', 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(deleteMyAccount('KeepName'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('throws an ApiError with the status when the server fails without a problem body', async () => {
    stubFetch(new Response(null, { status: 500 }))

    const error = await captureError(deleteMyAccount('FormerMember'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 500, errorCode: null })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(deleteMyAccount('KeepName'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
