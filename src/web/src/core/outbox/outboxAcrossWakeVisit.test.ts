import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiRequest, jsonRequest } from '@/core/api/apiClient'
import { endpoints } from '@/core/api/endpoints'
import { stubFetch } from '@/test/apiTestHelpers'
import { storedIds } from '@/test/outboxTestHelpers'
import { testOutboxItem } from '@/test/outboxTestData'
import type { OutboxItem } from './outboxItem'
import { createOutboxSender } from './outboxSender'
import { saveOrQueue } from './saveOrQueue'

const personId = 'person-1'
const item = testOutboxItem('request-1')

function asleepAnswer(): Response {
  return new Response('Kvit proxy: the API server is asleep', {
    status: 503,
    headers: {
      'content-type': 'text/plain; charset=utf-8',
      'x-kvit-server-asleep': '1',
      'x-kvit-wake-url': 'https://kvit-mk-api.onrender.com/wake',
    },
  })
}

function savedAnswer(): Response {
  return new Response('{"id":"expense-1"}', { status: 200, headers: { 'content-type': 'application/json' } })
}

function sendToTheServer(queued: OutboxItem): Promise<unknown> {
  return apiRequest(endpoints.groups, jsonRequest('POST', queued.payload))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('a save made while the server is asleep, across the visit to the wake page', () => {
  it('is queued in this browser and is sent by the next page once the server answers', async () => {
    vi.stubGlobal('location', { pathname: '/groups/group-1', search: '', assign: vi.fn() })
    const fetchMock = stubFetch(asleepAnswer(), savedAnswer())
    const pageBeforeTheVisit = createOutboxSender({ personId, sendItem: sendToTheServer, afterSent: async () => undefined })

    const outcome = await saveOrQueue({
      personId,
      item,
      save: () => sendToTheServer(item),
      enqueue: (queued) => pageBeforeTheVisit.enqueue(queued),
      onLateSuccess: async () => undefined,
    })
    pageBeforeTheVisit.pause()

    expect(outcome).toEqual({ queued: true })
    expect(storedIds(personId)).toEqual([item.id])

    const pageAfterTheVisit = createOutboxSender({ personId, sendItem: sendToTheServer, afterSent: async () => undefined })
    await pageAfterTheVisit.run()

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(storedIds(personId)).toEqual([])
    expect(pageAfterTheVisit.getSnapshot().items).toEqual([])
  })

  it('stays queued when the next page still gets the asleep answer', async () => {
    vi.stubGlobal('location', { pathname: '/groups/group-1', search: '', assign: vi.fn() })
    stubFetch(asleepAnswer())
    const pageBeforeTheVisit = createOutboxSender({ personId, sendItem: sendToTheServer, afterSent: async () => undefined })
    await saveOrQueue({
      personId,
      item,
      save: () => sendToTheServer(item),
      enqueue: (queued) => pageBeforeTheVisit.enqueue(queued),
      onLateSuccess: async () => undefined,
    })
    pageBeforeTheVisit.pause()

    const pageAfterTheVisit = createOutboxSender({ personId, sendItem: sendToTheServer, afterSent: async () => undefined })
    await pageAfterTheVisit.run()
    pageAfterTheVisit.pause()

    expect(storedIds(personId)).toEqual([item.id])
    expect(pageAfterTheVisit.getSnapshot().items.map((waiting) => [waiting.id, waiting.state])).toEqual([[item.id, 'waiting']])
  })
})
