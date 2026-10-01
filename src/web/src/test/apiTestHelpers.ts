import { vi, type Mock } from 'vitest'

export interface SentRequest {
  url: string
  method: string | undefined
  contentType: string | null
  body: unknown
}

export function stubFetch(...results: (Response | Error)[]): Mock<typeof fetch> {
  let callCount = 0
  const fetchMock = vi.fn<typeof fetch>(async () => {
    const result = results[Math.min(callCount, results.length - 1)]
    callCount += 1
    if (result instanceof Error) {
      throw result
    }
    return result
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function stubFetchThatNeverAnswers(): void {
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof fetch>(() => new Promise<Response>(() => {})),
  )
}

export function problemResponse(status: number, errorCode: string): Response {
  return new Response(JSON.stringify({ status, errorCode }), {
    status,
    headers: { 'content-type': 'application/problem+json' },
  })
}

export function sentRequest(fetchMock: Mock<typeof fetch>): SentRequest {
  const [input, init] = fetchMock.mock.calls[0]
  const body = init?.body
  return {
    url: String(input),
    method: init?.method,
    contentType: new Headers(init?.headers).get('content-type'),
    body: typeof body === 'string' ? JSON.parse(body) : body,
  }
}

export async function captureError(promise: Promise<unknown>): Promise<unknown> {
  try {
    await promise
  } catch (error) {
    return error
  }
  throw new Error('Expected the request to fail, but it succeeded')
}
