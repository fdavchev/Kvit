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

type PathAnswer = Response | Error

export function stubFetchByPath(
  answers: Record<string, PathAnswer | PathAnswer[]>,
): Mock<typeof fetch> {
  const callCounts = new Map<string, number>()
  const fetchMock = vi.fn<typeof fetch>(async (input) => {
    const path = String(input)
    const listed: PathAnswer | PathAnswer[] | undefined = answers[path]
    if (listed === undefined) {
      throw new Error(`The test did not expect a request to ${path}`)
    }
    const callCount = callCounts.get(path) ?? 0
    callCounts.set(path, callCount + 1)
    const answer = Array.isArray(listed)
      ? listed[Math.min(callCount, listed.length - 1)]
      : listed
    if (answer instanceof Error) {
      throw answer
    }
    return answer
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
  return describeRequest(fetchMock.mock.calls[0])
}

export function sentRequestTo(fetchMock: Mock<typeof fetch>, url: string): SentRequest {
  const call = fetchMock.mock.calls.find(([input]) => String(input) === url)
  if (call === undefined) {
    throw new Error(`No request was sent to ${url}`)
  }
  return describeRequest(call)
}

export function requestCountTo(fetchMock: Mock<typeof fetch>, url: string): number {
  return fetchMock.mock.calls.filter(([input]) => String(input) === url).length
}

export function describeRequest([input, init]: Parameters<typeof fetch>): SentRequest {
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
