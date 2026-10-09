import { dehydrate, QueryClient, useQuery } from '@tanstack/react-query'
import {
  PersistQueryClientProvider,
  type PersistedClient,
  type Persister,
} from '@tanstack/react-query-persist-client'
import { render, screen, waitFor } from '@testing-library/react'
import { del, get, set } from 'idb-keyval'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter, type DataRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import { createI18n } from '@/core/i18n/i18n'
import { BottomBarLayout } from '@/core/router/BottomBarLayout'
import { RequireAuth } from '@/core/router/RequireAuth'
import { RouterAfterRestore } from '@/core/router/RouterAfterRestore'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { problemResponse, requestCountTo, stubFetch, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { createTestPersister } from '@/test/testPersister'
import { endpoints } from './endpoints'
import { createQueryClient } from './queryClient'
import {
  checkRestoredMeWithServer,
  createSavedQueryCachePersister,
  reportRestoreFailure,
  savedCopyReadTimeLimitMs,
  savedQueryCacheOptions,
  savedQueryCacheVersion,
  type SavedQueryCachePersister,
} from './savedQueryCache'

const routerHolder = vi.hoisted(() => ({ current: undefined as DataRouter | undefined }))

vi.mock('@/core/router/router', () => ({
  get router() {
    return routerHolder.current
  },
}))

vi.mock('idb-keyval', () => ({ get: vi.fn(), set: vi.fn(), del: vi.fn() }))

const groupsKey = ['groups']
const savedGroups = ['Greece trip']
const otherPerson: Me = { ...testMe, id: 'a7d1c2e4-0000-4000-8000-000000000002', displayName: 'Mila' }
const restoreFailureMessage =
  'Could not restore the saved data from this browser (IndexedDB); Kvit starts with an empty cache'
const restoreTimeoutMessage =
  'Reading the saved data from this browser (IndexedDB) took longer than 3000 ms; Kvit stopped waiting and starts with an empty cache'
const beforeThePageOpened = performance.timeOrigin - 60 * 60 * 1000
const updatingNote = translated('en', 'common.updating')

function GroupsOnScreen() {
  const groupsQuery = useQuery({
    queryKey: groupsKey,
    queryFn: () => new Promise<string[]>(() => {}),
    staleTime: Infinity,
  })
  return <p>{groupsQuery.data === undefined ? 'no groups' : groupsQuery.data.join(', ')}</p>
}

function savedCopy(
  me: Me,
  buster: string = savedQueryCacheVersion,
  savedAt: number = Date.now(),
): PersistedClient {
  const queryClient = new QueryClient()
  queryClient.setQueryData<Me | null>(meQueryKey, me, { updatedAt: savedAt })
  queryClient.setQueryData(groupsKey, savedGroups, { updatedAt: savedAt })
  return { timestamp: savedAt, buster, clientState: dehydrate(queryClient) }
}

interface SavedPersister {
  persister: SavedQueryCachePersister
  removeClient: Mock<Persister['removeClient']>
}

async function persisterHolding(copy: PersistedClient | undefined): Promise<SavedPersister> {
  const persister = createTestPersister()
  if (copy !== undefined) {
    await persister.persistClient(copy)
  }
  const removeClient = vi.spyOn(persister, 'removeClient')
  return { persister, removeClient }
}

async function startApp(persister: SavedQueryCachePersister): Promise<QueryClient> {
  routerHolder.current = createMemoryRouter(
    [
      { path: routes.welcome, element: <p>welcome page</p> },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <BottomBarLayout />,
            children: [{ path: routes.dashboard, element: <GroupsOnScreen /> }],
          },
        ],
      },
    ],
    { initialEntries: [routes.dashboard] },
  )
  const queryClient = createQueryClient(persister)
  const defaultOptions = queryClient.getDefaultOptions()
  queryClient.setDefaultOptions({
    ...defaultOptions,
    queries: { ...defaultOptions.queries, retry: false },
  })
  const i18n = await createI18n('en')
  render(
    <I18nextProvider i18n={i18n}>
      <PersistQueryClientProvider
        client={queryClient}
        persistOptions={savedQueryCacheOptions(queryClient, persister)}
        onSuccess={() => checkRestoredMeWithServer(queryClient)}
        onError={() => reportRestoreFailure(persister)}
      >
        <RouterAfterRestore />
      </PersistQueryClientProvider>
    </I18nextProvider>,
  )
  return queryClient
}

describe('restoring the saved data when the app starts', () => {
  beforeEach(() => {
    vi.spyOn(console, 'warn').mockImplementation(() => {})
  })

  afterEach(() => {
    routerHolder.current?.dispose()
    routerHolder.current = undefined
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.resetAllMocks()
    vi.restoreAllMocks()
  })

  it('shows the saved screen before /api/me has answered, then keeps it when the same person is confirmed', async () => {
    let answerMe: (response: Response) => void = () => {}
    const meAnswer = new Promise<Response>((resolve) => {
      answerMe = resolve
    })
    const fetchMock = vi.fn<typeof fetch>(() => meAnswer)
    vi.stubGlobal('fetch', fetchMock)
    const { persister, removeClient } = await persisterHolding(savedCopy(testMe))

    await startApp(persister)

    expect(await screen.findByText('Greece trip')).toBeTruthy()
    expect(screen.queryByRole('status')).toBeNull()
    await waitFor(() => {
      expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
    })

    answerMe(Response.json(testMe))

    await waitFor(() => {
      expect(screen.getByText('Greece trip')).toBeTruthy()
    })
    expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('shows the saved screen and no error when /api/me fails in the background', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 502 }))
    const { persister, removeClient } = await persisterHolding(savedCopy(testMe))

    const queryClient = await startApp(persister)

    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
    })
    expect(requestCountTo(fetchMock, endpoints.me)).toBe(1)
    expect(screen.getByText('Greece trip')).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('shows Welcome, wipes the groups and deletes the saved copy when /api/me answers 401', async () => {
    stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))
    const { persister, removeClient } = await persisterHolding(savedCopy(testMe))

    const queryClient = await startApp(persister)

    expect(await screen.findByText('welcome page')).toBeTruthy()
    expect(screen.queryByText('Greece trip')).toBeNull()
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    expect(removeClient).toHaveBeenCalledTimes(1)
  })

  it('resets the groups and deletes the saved copy when /api/me names a different person', async () => {
    stubFetch(Response.json(otherPerson))
    const { persister, removeClient } = await persisterHolding(savedCopy(testMe))

    const queryClient = await startApp(persister)

    await waitFor(() => {
      expect(queryClient.getQueryData(meQueryKey)).toEqual(otherPerson)
    })
    expect(await screen.findByText('no groups')).toBeTruthy()
    expect(screen.queryByText('Greece trip')).toBeNull()
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(removeClient).toHaveBeenCalledTimes(1)
  })

  it('logs the specific restore message and shows loading with an empty cache when the restore fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetchThatNeverAnswers()
    const persister: SavedQueryCachePersister = {
      ...createTestPersister(),
      restoreClient: () => Promise.reject(new Error('IndexedDB is broken')),
    }

    const queryClient = await startApp(persister)

    expect(await screen.findByRole('status')).toBeTruthy()
    expect(screen.queryByText('Greece trip')).toBeNull()
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toBeUndefined()
    const restoreMessages = consoleError.mock.calls.filter(
      ([message]) => message === restoreFailureMessage,
    )
    expect(restoreMessages).toHaveLength(1)
  })

  it('throws away a saved copy from another version and shows loading', async () => {
    stubFetchThatNeverAnswers()
    const { persister, removeClient } = await persisterHolding(savedCopy(testMe, 'an-older-version'))

    const queryClient = await startApp(persister)

    expect(await screen.findByRole('status')).toBeTruthy()
    expect(screen.queryByText('Greece trip')).toBeNull()
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(removeClient).toHaveBeenCalled()
  })

  it('shows loading and then the screen when nothing was saved', async () => {
    stubFetch(Response.json(testMe))
    const { persister } = await persisterHolding(undefined)

    await startApp(persister)

    expect(await screen.findByText('no groups')).toBeTruthy()
  })

  it('stops waiting for a saved copy that takes longer than 3 seconds to read, logs why and starts with an empty cache', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'], shouldAdvanceTime: true })
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(Response.json(testMe))
    vi.mocked(get).mockReturnValue(new Promise(() => {}))
    vi.mocked(del).mockReturnValue(new Promise(() => {}))
    vi.mocked(set).mockResolvedValue(undefined)

    await startApp(createSavedQueryCachePersister())
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs - 1)
    expect(screen.queryByText('no groups')).toBeNull()

    await vi.advanceTimersByTimeAsync(1)

    expect(await screen.findByText('no groups')).toBeTruthy()
    const timeoutMessages = consoleError.mock.calls.filter(
      ([message]) => message === restoreTimeoutMessage,
    )
    expect(timeoutMessages).toHaveLength(1)
  })

  it('never puts a saved copy into the app when it is read only after the time limit', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'], shouldAdvanceTime: true })
    vi.spyOn(console, 'error').mockImplementation(() => {})
    stubFetch(Response.json(testMe))
    let answerRead: (savedText: string) => void = () => {}
    vi.mocked(get).mockReturnValue(
      new Promise<string>((resolve) => {
        answerRead = resolve
      }),
    )
    vi.mocked(del).mockResolvedValue(undefined)
    vi.mocked(set).mockResolvedValue(undefined)
    const queryClient = await startApp(createSavedQueryCachePersister())
    await vi.advanceTimersByTimeAsync(savedCopyReadTimeLimitMs)
    expect(await screen.findByText('no groups')).toBeTruthy()

    answerRead(JSON.stringify(savedCopy(otherPerson)))
    await vi.advanceTimersByTimeAsync(0)

    expect(screen.queryByText('Greece trip')).toBeNull()
    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('shows Updating… over the bar while the saved person is checked with the server, and hides it once /api/me answers', async () => {
    let answerMe: (response: Response) => void = () => {}
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(
        () =>
          new Promise<Response>((resolve) => {
            answerMe = resolve
          }),
      ),
    )
    const { persister } = await persisterHolding(savedCopy(testMe, savedQueryCacheVersion, beforeThePageOpened))

    await startApp(persister)

    expect(await screen.findByText('Greece trip')).toBeTruthy()
    expect((await screen.findByRole('status')).textContent).toBe(updatingNote)

    answerMe(Response.json(testMe))

    await waitFor(() => {
      expect(screen.queryByText(updatingNote)).toBeNull()
    })
    expect(screen.getByText('Greece trip')).toBeTruthy()
  })

  it('hides Updating… once the check of the saved person fails', async () => {
    stubFetch(new Response(null, { status: 503 }))
    const { persister } = await persisterHolding(savedCopy(testMe, savedQueryCacheVersion, beforeThePageOpened))

    const queryClient = await startApp(persister)

    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
    })
    expect(screen.queryByText(updatingNote)).toBeNull()
    expect(screen.getByText('Greece trip')).toBeTruthy()
  })

  it('shows no Updating… on a first visit with nothing saved', async () => {
    stubFetchThatNeverAnswers()
    const { persister } = await persisterHolding(undefined)

    await startApp(persister)

    expect(await screen.findByRole('status')).toBeTruthy()
    expect(screen.queryByText(updatingNote)).toBeNull()
  })
})
