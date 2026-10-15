import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  closingConfirmPath,
  closingGroup,
  closingObjectPath,
  closingPath,
  unlockPath,
} from '@/test/closingTestData'
import { greeceGroupRow, groupListOf, testGroup, testGroupId } from '@/test/groupTestData'
import { ownerViewMembers } from '@/test/memberTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { useConfirmClosing } from './useConfirmClosing'
import { useObjectToClosing } from './useObjectToClosing'
import { useStartClosing } from './useStartClosing'
import { useUnlockGroup } from './useUnlockGroup'

interface Mounted {
  queryClient: QueryClient
  run: () => Promise<unknown>
}

type Mount = () => Promise<Mounted>

interface HookCase {
  name: string
  mount: Mount
  url: string
  body: unknown
  failure: [number, string]
}

const refreshedKeys: [string, readonly string[]][] = [
  ['the group', ['groups', testGroupId]],
  ['the group list', ['groups']],
  ['the activity of the group', ['groups', testGroupId, 'activity']],
  ['the balances of the group', ['groups', testGroupId, 'balances']],
  ['the expenses of the group', ['groups', testGroupId, 'expenses']],
  ['the members of the group', ['groups', testGroupId, 'members']],
  ['the payments of the group', ['groups', testGroupId, 'settlements']],
]

function seedEverything(queryClient: QueryClient): void {
  queryClient.setQueryData(['groups', testGroupId], testGroup)
  queryClient.setQueryData(['groups'], groupListOf({ groups: [greeceGroupRow] }))
  queryClient.setQueryData(['groups', testGroupId, 'activity'], [])
  queryClient.setQueryData(['groups', testGroupId, 'balances'], { isEveryoneKvit: true, currencies: [] })
  queryClient.setQueryData(['groups', testGroupId, 'expenses'], [])
  queryClient.setQueryData(['groups', testGroupId, 'members'], ownerViewMembers)
  queryClient.setQueryData(['groups', testGroupId, 'settlements'], [])
  queryClient.setQueryData(['other'], [])
}

const mountStart: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useStartClosing(testGroupId), {
    seedCache: seedEverything,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountConfirm: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useConfirmClosing(testGroupId), {
    seedCache: seedEverything,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const mountObject: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useObjectToClosing(testGroupId), {
    seedCache: seedEverything,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync('Not yet')) }
}

const mountUnlock: Mount = async () => {
  const { result, queryClient } = await renderHookWithProviders(() => useUnlockGroup(testGroupId), {
    seedCache: seedEverything,
  })
  return { queryClient, run: () => act(() => result.current.mutateAsync()) }
}

const hookCases: HookCase[] = [
  { name: 'useStartClosing', mount: mountStart, url: closingPath, body: undefined, failure: [400, 'GROUP_NOT_KVIT'] },
  { name: 'useConfirmClosing', mount: mountConfirm, url: closingConfirmPath, body: undefined, failure: [400, 'CLOSING_ALREADY_ANSWERED'] },
  { name: 'useObjectToClosing', mount: mountObject, url: closingObjectPath, body: { reason: 'Not yet' }, failure: [400, 'GROUP_NOT_CLOSING'] },
  { name: 'useUnlockGroup', mount: mountUnlock, url: unlockPath, body: undefined, failure: [400, 'GROUP_NOT_FINISHED'] },
]

describe.each(hookCases)('$name', ({ mount, url, body, failure }) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends one POST to the closing address of the group', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { run } = await mount()

    await run()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(sentRequest(fetchMock)).toMatchObject({ url, method: 'POST', body })
  })

  it.each(refreshedKeys)('marks %s as out of date after it worked', async (_name, queryKey) => {
    stubFetch(new Response(null, { status: 204 }))
    const { run, queryClient } = await mount()

    await run()

    expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true)
  })

  it('does not touch a query that is not about groups', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { run, queryClient } = await mount()

    await run()

    expect(queryClient.getQueryState(['other'])?.isInvalidated).toBe(false)
  })

  it('resolves only after the group that is on screen was asked again', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { run, queryClient } = await mount()
    const loadGroup = vi.fn(() => Promise.resolve(closingGroup))
    const watcher = new QueryObserver(queryClient, {
      queryKey: ['groups', testGroupId],
      queryFn: loadGroup,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    await run()

    expect(loadGroup).toHaveBeenCalledTimes(1)
    expect(queryClient.getQueryData(['groups', testGroupId])).toEqual(closingGroup)
    stopWatching()
  })

  it('exposes the ApiError of the server', async () => {
    const [httpStatus, errorCode] = failure
    stubFetch(problemResponse(httpStatus, errorCode))
    const { run } = await mount()

    const error = await captureError(run())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('useObjectToClosing without a reason', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends a JSON body that has no reason', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useObjectToClosing(testGroupId), {
      seedCache: seedEverything,
    })

    await act(() => result.current.mutateAsync(undefined))

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(closingObjectPath)
    expect(request.contentType).toBe('application/json')
    expect((request.body as { reason?: unknown }).reason ?? null).toBeNull()
  })
})
