import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { exportMyData } from './meService'

const exportBody = '{\n  "profile": { "displayName": "Филип" },\n  "groups": []\n}'

function exportResponse(contentDisposition: string | null): Response {
  const headers = new Headers({ 'content-type': 'application/json' })
  if (contentDisposition !== null) {
    headers.set('content-disposition', contentDisposition)
  }
  return new Response(exportBody, { status: 200, headers })
}

describe('exportMyData', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends GET /api/me/export with the session cookie', async () => {
    const fetchMock = stubFetch(exportResponse('attachment; filename="kvit-my-data-2026-11-04.json"'))

    await exportMyData()

    expect(fetchMock).toHaveBeenCalledOnce()
    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/me/export')
    expect(request.method ?? 'GET').toBe('GET')
    expect(fetchMock.mock.calls[0][1]?.credentials).toBe('same-origin')
  })

  it('takes the file name from a quoted filename in the Content-Disposition header', async () => {
    stubFetch(exportResponse('attachment; filename="kvit-my-data-2026-11-04.json"'))

    const file = await exportMyData()

    expect(file.fileName).toBe('kvit-my-data-2026-11-04.json')
  })

  it('takes the file name from an unquoted filename in the Content-Disposition header', async () => {
    stubFetch(exportResponse('attachment; filename=kvit-my-data-2026-11-04.json'))

    const file = await exportMyData()

    expect(file.fileName).toBe('kvit-my-data-2026-11-04.json')
  })

  it('takes the plain filename when the header also carries a filename* parameter, as ASP.NET writes it', async () => {
    stubFetch(
      exportResponse("attachment; filename=kvit-my-data-2026-11-04.json; filename*=UTF-8''kvit-my-data-2026-11-04.json"),
    )

    const file = await exportMyData()

    expect(file.fileName).toBe('kvit-my-data-2026-11-04.json')
  })

  it('falls back to kvit-my-data.json when the Content-Disposition header is missing, and still returns the data', async () => {
    stubFetch(exportResponse(null))

    const file = await exportMyData()

    expect(file.fileName).toBe('kvit-my-data.json')
    expect(await file.blob.text()).toBe(exportBody)
  })

  it('returns the body exactly as the server sent it, without parsing and re-writing the JSON', async () => {
    stubFetch(exportResponse('attachment; filename="kvit-my-data-2026-11-04.json"'))

    const file = await exportMyData()

    expect(await file.blob.text()).toBe(exportBody)
  })

  it('returns a blob of type application/json', async () => {
    stubFetch(exportResponse('attachment; filename="kvit-my-data-2026-11-04.json"'))

    const file = await exportMyData()

    expect(file.blob.type).toContain('application/json')
  })

  it.each([
    ['nobody is signed in', 401, 'AUTH_NOT_SIGNED_IN'],
    ['the server is busy', 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(exportMyData())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('throws an ApiError with the status when the server fails without a problem body', async () => {
    stubFetch(new Response(null, { status: 500 }))

    const error = await captureError(exportMyData())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 500, errorCode: null })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(exportMyData())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
