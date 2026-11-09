import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError } from '@/test/apiTestHelpers'
import { anaPerson, logEntries, tripGroup } from '@/test/ownerToolsTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { useCloseGroup } from './useCloseGroup'
import { useEraseName } from './useEraseName'
import { useOwnerToolsGroup } from './useOwnerToolsGroup'
import { useOwnerToolsLog } from './useOwnerToolsLog'
import { useOwnerToolsPeople } from './useOwnerToolsPeople'
import { useReopenGroup } from './useReopenGroup'
import { useSuspendPerson } from './useSuspendPerson'
import { useUnsuspendPerson } from './useUnsuspendPerson'

const peopleGet = 'GET /api/admin/tools/people?query=ana'
const groupGet = `GET /api/admin/tools/groups/${tripGroup.id}`
const logGet = 'GET /api/admin/tools/log?limit=50'
const reason = 'Reported by three people'

const readAnswers: Record<string, AnswerFactory> = {
  [peopleGet]: jsonAnswer([anaPerson]),
  [groupGet]: jsonAnswer(tripGroup),
  [logGet]: jsonAnswer(logEntries),
}

interface ActionCase {
  name: string
  useAction: typeof useSuspendPerson
  post: string
  id: string
  refreshedGets: string[]
}

const personPost = (action: string) => `POST /api/admin/tools/people/${anaPerson.id}/${action}`
const groupPost = (action: string) => `POST /api/admin/tools/groups/${tripGroup.id}/${action}`

const actionCases: ActionCase[] = [
  { name: 'useSuspendPerson', useAction: useSuspendPerson, post: personPost('suspend'), id: anaPerson.id, refreshedGets: [peopleGet, logGet] },
  { name: 'useUnsuspendPerson', useAction: useUnsuspendPerson, post: personPost('unsuspend'), id: anaPerson.id, refreshedGets: [peopleGet, logGet] },
  { name: 'useEraseName', useAction: useEraseName, post: personPost('erase-name'), id: anaPerson.id, refreshedGets: [peopleGet, logGet] },
  { name: 'useCloseGroup', useAction: useCloseGroup, post: groupPost('close'), id: tripGroup.id, refreshedGets: [groupGet, logGet] },
  { name: 'useReopenGroup', useAction: useReopenGroup, post: groupPost('reopen'), id: tripGroup.id, refreshedGets: [groupGet, logGet] },
]

function urlOf(request: string): string {
  return request.slice(request.indexOf(' ') + 1)
}

describe.each(actionCases)('$name', ({ useAction, post, id, refreshedGets }) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  async function mountWithReadsOpen(postAnswer: AnswerFactory) {
    const fetchMock = stubFetchByRequest({ ...readAnswers, [post]: postAnswer })
    const rendered = await renderHookWithProviders(() => ({
      action: useAction(),
      people: useOwnerToolsPeople('ana'),
      group: useOwnerToolsGroup(tripGroup.id),
      log: useOwnerToolsLog(),
    }))
    await waitFor(() => {
      expect(rendered.result.current.people.isSuccess).toBe(true)
      expect(rendered.result.current.group.isSuccess).toBe(true)
      expect(rendered.result.current.log.isSuccess).toBe(true)
    })
    return { fetchMock, ...rendered }
  }

  it('is idle at first and asks nothing until it is run', async () => {
    const { result, fetchMock } = await mountWithReadsOpen(noContentAnswer())

    expect(result.current.action.isIdle).toBe(true)
    expect(requestCount(fetchMock, 'POST', urlOf(post))).toBe(0)
  })

  it('sends one POST with the reason as JSON when it is run', async () => {
    const { result, fetchMock } = await mountWithReadsOpen(noContentAnswer())

    await act(() => result.current.action.mutateAsync({ id, reason }))

    const posts = requestsOf(fetchMock, 'POST', urlOf(post))
    expect(posts).toHaveLength(1)
    expect(posts[0].body).toEqual({ reason })
  })

  it('asks again for the open lists it changes, once each, after the server answered', async () => {
    const { result, fetchMock } = await mountWithReadsOpen(noContentAnswer())

    await act(() => result.current.action.mutateAsync({ id, reason }))

    await waitFor(() => {
      for (const get of refreshedGets) {
        expect(requestCount(fetchMock, 'GET', urlOf(get)), get).toBe(2)
      }
    })
  })

  it('does not ask again when the server refuses', async () => {
    const { result, fetchMock } = await mountWithReadsOpen(problemAnswer(400, 'OWNER_REASON_INVALID'))

    const error = await captureError(act(() => result.current.action.mutateAsync({ id, reason })))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'OWNER_REASON_INVALID' })
    for (const get of [peopleGet, groupGet, logGet]) {
      expect(requestCount(fetchMock, 'GET', urlOf(get)), get).toBe(1)
    }
  })

  it('holds the error with its code, so the screen can show it', async () => {
    const { result } = await mountWithReadsOpen(problemAnswer(403, 'OWNER_CANNOT_ACT_ON_ADMIN'))

    await captureError(act(() => result.current.action.mutateAsync({ id, reason })))

    await waitFor(() => {
      expect(result.current.action.isError).toBe(true)
    })
    expect(result.current.action.error).toMatchObject({ httpStatus: 403, errorCode: 'OWNER_CANNOT_ACT_ON_ADMIN' })
  })
})
