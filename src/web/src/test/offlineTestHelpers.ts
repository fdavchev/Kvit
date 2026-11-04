import { onlineManager } from '@tanstack/react-query'
import type { Mock } from 'vitest'
import { requestsOf, type AnswerFactory } from './requestTestHelpers'

export function goOffline(): void {
  onlineManager.setOnline(false)
}

export function goOnline(): void {
  onlineManager.setOnline(true)
}

export function answerOnlyWhileOnline(answerWhenOnline: AnswerFactory): AnswerFactory {
  return () => (onlineManager.isOnline() ? answerWhenOnline() : new TypeError('Failed to fetch'))
}

export function postedRequestIds(fetchMock: Mock<typeof fetch>, url: string, firstCallIndex: number = 0): string[] {
  const callsOfTheUrl = fetchMock.mock.calls.slice(firstCallIndex)
  const postedAfter = callsOfTheUrl
    .filter(([input, init]) => String(input) === url && init?.method === 'POST')
    .map(([, init]) => JSON.parse(String(init?.body)) as { clientRequestId: string })
  return postedAfter.map((body) => body.clientRequestId)
}

export function postCount(fetchMock: Mock<typeof fetch>, url: string): number {
  return requestsOf(fetchMock, 'POST', url).length
}
