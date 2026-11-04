import {
  focusManager,
  MutationObserver,
  onlineManager,
  QueryObserver,
  type QueryClient,
} from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError } from '@/test/apiTestHelpers'
import { createTestPersister } from '@/test/testPersister'
import { testMe } from '@/test/testMe'
import { ApiError } from './apiClient'
import { createQueryClient, groupStateChangedCodes, reactToFailedRequest } from './queryClient'

type FailingCall = 'query' | 'mutation'

function letRealTimeGoBy(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds))
}

function apiError(httpStatus: number | null, errorCode: string | null): ApiError {
  return new ApiError('request failed', { httpStatus, errorCode })
}

async function failWith(
  queryClient: QueryClient,
  call: FailingCall,
  error: Error,
): Promise<void> {
  const fail = (): Promise<never> => Promise.reject(error)
  if (call === 'query') {
    await captureError(queryClient.fetchQuery({ queryKey: ['groups'], queryFn: fail, retry: false }))
    return
  }
  await captureError(new MutationObserver(queryClient, { mutationFn: fail }).mutate())
}

function defaultQueryRetry(): (failureCount: number, error: Error) => boolean {
  const retry = createQueryClient(createTestPersister()).getDefaultOptions().queries?.retry
  if (typeof retry !== 'function') {
    throw new Error(`The default query retry option is not a function: ${String(retry)}`)
  }
  return retry
}

function createClientWithSignedInPerson(): QueryClient {
  const queryClient = createQueryClient(createTestPersister())
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
  return queryClient
}

describe('createQueryClient', () => {
  afterEach(() => {
    focusManager.setFocused(undefined)
  })

  describe.each(['query', 'mutation'] as const)('when a %s fails', (call) => {
    it.each([
      ['a 401 with an error code', apiError(401, 'AUTH_NOT_SIGNED_IN')],
      ['a 401 without an error code', apiError(401, null)],
    ])('signs the person out of the me cache on %s', async (_name, error) => {
      const queryClient = createClientWithSignedInPerson()

      await failWith(queryClient, call, error)

      expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    })

    it('refreshes the me cache on a 403 AUTH_MUST_CHANGE_PASSWORD', async () => {
      const queryClient = createClientWithSignedInPerson()

      await failWith(queryClient, call, apiError(403, 'AUTH_MUST_CHANGE_PASSWORD'))

      expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(true)
    })

    it.each([
      ['a wrong password at log-in (401 AUTH_INVALID_CREDENTIALS)', apiError(401, 'AUTH_INVALID_CREDENTIALS')],
      ['another 403', apiError(403, 'SOMETHING_ELSE')],
      ['a 500', apiError(500, null)],
      ['a network failure', apiError(null, null)],
      ['an error that is not an ApiError', new Error('boom')],
    ])('leaves the me cache alone on %s', async (_name, error) => {
      const queryClient = createClientWithSignedInPerson()

      await failWith(queryClient, call, error)

      expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
      expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(false)
    })
  })

  describe('the default query retry', () => {
    it('does not retry a 404 answer', () => {
      const retry = defaultQueryRetry()

      expect(retry(0, apiError(404, 'GROUP_NOT_FOUND'))).toBe(false)
    })

    it('does not retry a 429 answer', () => {
      const retry = defaultQueryRetry()

      expect(retry(0, apiError(429, 'RATE_LIMITED'))).toBe(false)
    })

    it.each([
      ['a 400 answer', apiError(400, 'PERSONAL_ENTRY_MONTH_INVALID')],
      ['a 401 answer', apiError(401, 'AUTH_NOT_SIGNED_IN')],
      ['a 403 answer', apiError(403, 'GUEST_NOT_ALLOWED')],
      ['a 409 answer', apiError(409, 'SOMETHING_CHANGED')],
      ['a 422 answer', apiError(422, null)],
    ])('does not retry %s, because asking again cannot change it', (_name, error) => {
      const retry = defaultQueryRetry()

      expect(retry(0, error)).toBe(false)
    })

    it.each([
      ['a 408 answer', apiError(408, null)],
      ['a 425 answer', apiError(425, null)],
    ])('retries %s up to three times and then gives up, because asking again can work', (_name, error) => {
      const retry = defaultQueryRetry()

      expect([0, 1, 2, 3].map((failureCount) => retry(failureCount, error))).toEqual([
        true,
        true,
        true,
        false,
      ])
    })

    it.each([
      ['a 407 answer', apiError(407, null)],
      ['a 410 answer', apiError(410, null)],
      ['a 426 answer', apiError(426, null)],
      ['a 499 answer', apiError(499, null)],
    ])('does not retry %s, the 4xx answers next to the ones worth retrying', (_name, error) => {
      const retry = defaultQueryRetry()

      expect(retry(0, error)).toBe(false)
    })

    it.each([
      ['a 500 answer', apiError(500, null)],
      ['a 503 answer', apiError(503, null)],
      ['a network failure', apiError(null, null)],
      ['an error that is not an ApiError', new Error('boom')],
    ])('retries %s up to three times and then gives up', (_name, error) => {
      const retry = defaultQueryRetry()

      expect([0, 1, 2, 3].map((failureCount) => retry(failureCount, error))).toEqual([
        true,
        true,
        true,
        false,
      ])
    })
  })

  it('does not retry a failed mutation', async () => {
    const queryClient = createQueryClient(createTestPersister())
    const mutationFn = vi.fn((): Promise<never> => Promise.reject(apiError(500, null)))

    await captureError(new MutationObserver(queryClient, { mutationFn }).mutate())

    expect(mutationFn).toHaveBeenCalledTimes(1)
  })

  describe('when the browser is offline', () => {
    afterEach(() => {
      onlineManager.setOnline(true)
    })

    it('has the network mode always as the default of every mutation', () => {
      const mutationDefaults = createQueryClient(createTestPersister()).getDefaultOptions().mutations

      expect(mutationDefaults?.networkMode).toBe('always')
      expect(mutationDefaults?.retry).toBe(false)
    })

    it('leaves the network mode of the queries alone, so a query still waits for the network', () => {
      const queryDefaults = createQueryClient(createTestPersister()).getDefaultOptions().queries

      expect(queryDefaults?.networkMode).toBeUndefined()
    })

    it('runs a mutation at once instead of holding it', async () => {
      const queryClient = createQueryClient(createTestPersister())
      onlineManager.setOnline(false)
      const mutationFn = vi.fn(async () => 'saved')

      const settled = new MutationObserver(queryClient, { mutationFn }).mutate().then(() => 'saved', () => 'failed')
      await letRealTimeGoBy(20)

      expect(mutationFn).toHaveBeenCalledTimes(1)
      expect(await settled).toBe('saved')
    })

    it('fails a mutation whose request cannot reach the server at once, and does not run it again when the browser is online again', async () => {
      const queryClient = createQueryClient(createTestPersister())
      onlineManager.setOnline(false)
      const failure = apiError(null, null)
      const mutationFn = vi.fn((): Promise<never> => Promise.reject(failure))

      const settled = new MutationObserver(queryClient, { mutationFn }).mutate().then(() => undefined, (error: unknown) => error)
      await letRealTimeGoBy(20)
      expect(mutationFn).toHaveBeenCalledTimes(1)
      onlineManager.setOnline(true)
      await letRealTimeGoBy(20)

      expect(await settled).toBe(failure)
      expect(mutationFn).toHaveBeenCalledTimes(1)
    })

    it('keeps no mutation paused', async () => {
      const queryClient = createQueryClient(createTestPersister())
      onlineManager.setOnline(false)

      void new MutationObserver(queryClient, { mutationFn: async () => 'saved' }).mutate().catch(() => undefined)
      await letRealTimeGoBy(20)

      expect(queryClient.getMutationCache().getAll().filter((mutation) => mutation.state.isPaused)).toEqual([])
    })
  })

  it('does not refetch a query when the window regains focus', async () => {
    const queryClient = createQueryClient(createTestPersister())
    queryClient.mount()
    const queryFn = vi.fn(async () => 'groups')
    const observer = new QueryObserver(queryClient, { queryKey: ['groups'], queryFn })
    const unsubscribe = observer.subscribe(() => {})
    await vi.waitFor(() => {
      expect(observer.getCurrentResult().isSuccess).toBe(true)
    })

    focusManager.setFocused(false)
    focusManager.setFocused(true)
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(queryFn).toHaveBeenCalledTimes(1)
    unsubscribe()
    queryClient.unmount()
  })
})

describe('reactToFailedRequest', () => {
  const groupKey = ['groups', 'group-1'] as const
  const groupListKey = ['groups'] as const

  function createClientWithGroupsCached(): QueryClient {
    const queryClient = createClientWithSignedInPerson()
    queryClient.setQueryData(groupListKey, [])
    queryClient.setQueryData(groupKey, {})
    return queryClient
  }

  it.each([
    ['a 401 with an error code', apiError(401, 'AUTH_NOT_SIGNED_IN')],
    ['a 401 without an error code', apiError(401, null)],
  ])('signs the person out of the me cache on %s', (_name, error) => {
    const queryClient = createClientWithSignedInPerson()

    reactToFailedRequest(queryClient, error)

    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
  })

  it('refreshes the me cache on a 403 AUTH_MUST_CHANGE_PASSWORD', () => {
    const queryClient = createClientWithSignedInPerson()

    reactToFailedRequest(queryClient, apiError(403, 'AUTH_MUST_CHANGE_PASSWORD'))

    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(true)
  })

  it.each([
    ['a wrong password at log-in (401 AUTH_INVALID_CREDENTIALS)', apiError(401, 'AUTH_INVALID_CREDENTIALS')],
    ['another 403', apiError(403, 'SOMETHING_ELSE')],
    ['a 400 refusal', apiError(400, 'EXPENSE_AMOUNT_NOT_POSITIVE')],
    ['a 404 refusal', apiError(404, 'GROUP_NOT_FOUND')],
    ['a 500', apiError(500, null)],
    ['a network failure', apiError(null, null)],
    ['an error that is not an ApiError', new Error('boom')],
  ])('leaves the me cache and the groups alone on %s', (_name, error) => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, error)

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(groupListKey)?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(groupKey)?.isInvalidated).toBe(false)
  })

  it.each(groupStateChangedCodes)('refreshes the list of groups and every group on %s', (errorCode) => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(400, errorCode))

    expect(queryClient.getQueryState(groupListKey)?.isInvalidated).toBe(true)
    expect(queryClient.getQueryState(groupKey)?.isInvalidated).toBe(true)
  })

  it('does not sign the person out on a group state refusal', () => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(400, 'GROUP_FINISHED'))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('returns nothing, so the caller does not wait for the refresh of the groups', () => {
    const queryClient = createClientWithGroupsCached()

    const result: unknown = reactToFailedRequest(queryClient, apiError(400, 'GROUP_CLOSING'))

    expect(result).toBeUndefined()
  })

  it('does the same for a failed mutation of the query client as for a direct call', async () => {
    const queryClient = createClientWithGroupsCached()

    await failWith(queryClient, 'mutation', apiError(400, 'GROUP_FINISHED'))

    expect(queryClient.getQueryState(groupListKey)?.isInvalidated).toBe(true)
    expect(queryClient.getQueryState(groupKey)?.isInvalidated).toBe(true)
  })
})
