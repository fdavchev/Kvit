import {
  focusManager,
  MutationObserver,
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
import { createQueryClient } from './queryClient'

type FailingCall = 'query' | 'mutation'

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
