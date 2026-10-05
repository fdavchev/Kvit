import { vi, type Mock } from 'vitest'
import { describeRequest, problemResponse, type SentRequest } from './apiTestHelpers'

export type AnswerFactory = () => Response | Error

export function jsonAnswer(body: unknown): AnswerFactory {
  return () => Response.json(body)
}

export function noContentAnswer(): AnswerFactory {
  return () => new Response(null, { status: 204 })
}

export function problemAnswer(status: number, errorCode: string): AnswerFactory {
  return () => problemResponse(status, errorCode)
}

export function networkFailureAnswer(): AnswerFactory {
  return () => new TypeError('Failed to fetch')
}

export function stubFetchByRequest(
  answers: Record<string, AnswerFactory>,
): Mock<typeof fetch> {
  const fetchMock = vi.fn<typeof fetch>(async (input, init) => {
    const request = `${init?.method ?? 'GET'} ${String(input)}`
    const answer: AnswerFactory | undefined = answers[request]
    if (answer === undefined) {
      throw new Error(`The test did not expect the request ${request}`)
    }
    const result = answer()
    if (result instanceof Error) {
      throw result
    }
    return result
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function requestsOf(
  fetchMock: Mock<typeof fetch>,
  method: string,
  url: string,
): SentRequest[] {
  return fetchMock.mock.calls
    .map((call) => describeRequest(call))
    .filter((request) => (request.method ?? 'GET') === method && request.url === url)
}

export function requestCount(
  fetchMock: Mock<typeof fetch>,
  method: string,
  url: string,
): number {
  return requestsOf(fetchMock, method, url).length
}
