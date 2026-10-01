import { afterEach, describe, expect, it, vi } from 'vitest'
import { captureError, stubFetch } from '@/test/apiTestHelpers'
import { ApiError, apiRequest } from './apiClient'

describe('apiRequest', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('parses a JSON body', async () => {
    stubFetch(Response.json({ name: 'Greece trip' }))

    const body = await apiRequest('/api/groups/1')

    expect(body).toEqual({ name: 'Greece trip' })
  })

  it('returns a plain text body as a string', async () => {
    stubFetch(
      new Response('Healthy', { headers: { 'content-type': 'text/plain' } }),
    )

    const body = await apiRequest('/api/health')

    expect(body).toBe('Healthy')
  })

  it('returns undefined for 204 No Content', async () => {
    stubFetch(new Response(null, { status: 204 }))

    const body = await apiRequest('/api/logout', { method: 'POST' })

    expect(body).toBeUndefined()
  })

  it('sends same-origin credentials and keeps the request options', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await apiRequest('/api/logout', { method: 'POST' })

    expect(fetchMock).toHaveBeenCalledWith('/api/logout', {
      method: 'POST',
      credentials: 'same-origin',
    })
  })

  it('throws an ApiError with the ProblemDetails error code and detail', async () => {
    stubFetch(
      new Response(
        JSON.stringify({
          status: 404,
          title: 'Not Found',
          detail: 'Group was not found.',
          errorCode: 'GROUP_NOT_FOUND',
        }),
        {
          status: 404,
          headers: { 'content-type': 'application/problem+json; charset=utf-8' },
        },
      ),
    )

    const error = await captureError(apiRequest('/api/groups/1'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      httpStatus: 404,
      errorCode: 'GROUP_NOT_FOUND',
      detail: 'Group was not found.',
      message: 'GET /api/groups/1 failed with status 404',
    })
  })

  it('throws an ApiError without an error code for an empty error body', async () => {
    stubFetch(new Response(null, { status: 500 }))

    const error = await captureError(apiRequest('/api/groups'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      httpStatus: 500,
      errorCode: null,
      detail: null,
    })
  })

  it('throws an ApiError with no HTTP status when the network fails', async () => {
    const networkFailure = new TypeError('Failed to fetch')
    stubFetch(networkFailure)

    const error = await captureError(apiRequest('/api/health'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      httpStatus: null,
      message: 'GET /api/health could not reach the server',
      cause: networkFailure,
    })
  })

  it('throws an ApiError when a JSON body cannot be parsed', async () => {
    stubFetch(
      new Response('{not json', {
        headers: { 'content-type': 'application/json' },
      }),
    )

    const error = await captureError(apiRequest('/api/groups'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      httpStatus: 200,
      message: 'GET /api/groups returned a body that could not be read',
    })
  })
})
