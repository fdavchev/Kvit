import { MutationObserver, type QueryClient } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import { captureError } from '@/test/apiTestHelpers'
import { createTestPersister } from '@/test/testPersister'
import { testMe } from '@/test/testMe'
import { ApiError } from './apiClient'
import { createQueryClient, isGroupStateChangedError, reactToFailedRequest } from './queryClient'

const groupListKey = ['groups'] as const
const groupKey = ['groups', 'group-1'] as const

function apiError(httpStatus: number, errorCode: string): ApiError {
  return new ApiError('request failed', { httpStatus, errorCode })
}

function createClientWithGroupsCached(): QueryClient {
  const queryClient = createQueryClient(createTestPersister())
  queryClient.setQueryData(meQueryKey, testMe)
  queryClient.setQueryData(groupListKey, [])
  queryClient.setQueryData(groupKey, {})
  return queryClient
}

describe('a 403 ACCOUNT_SUSPENDED', () => {
  it('refreshes the me cache, so a person suspended while using Kvit sees the suspended screen', () => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(403, 'ACCOUNT_SUSPENDED'))

    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(true)
  })

  it('does not sign the person out and leaves the groups alone', () => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(403, 'ACCOUNT_SUSPENDED'))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryState(groupListKey)?.isInvalidated).toBe(false)
    expect(queryClient.getQueryState(groupKey)?.isInvalidated).toBe(false)
  })

  it('refreshes the me cache when a mutation of the query client fails with it', async () => {
    const queryClient = createClientWithGroupsCached()
    const fail = (): Promise<never> => Promise.reject(apiError(403, 'ACCOUNT_SUSPENDED'))

    await captureError(new MutationObserver(queryClient, { mutationFn: fail }).mutate())

    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(true)
  })
})

describe('a 403 GROUP_CLOSED_BY_ADMIN', () => {
  it('refreshes the list of groups and every group, so a screen that was open when the group was closed shows the bar', () => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(403, 'GROUP_CLOSED_BY_ADMIN'))

    expect(queryClient.getQueryState(groupListKey)?.isInvalidated).toBe(true)
    expect(queryClient.getQueryState(groupKey)?.isInvalidated).toBe(true)
  })

  it('does not sign the person out', () => {
    const queryClient = createClientWithGroupsCached()

    reactToFailedRequest(queryClient, apiError(403, 'GROUP_CLOSED_BY_ADMIN'))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(queryClient.getQueryState(meQueryKey)?.isInvalidated).toBe(false)
  })

  it('counts as a refusal because the state of the group changed, like GROUP_FINISHED', () => {
    expect(isGroupStateChangedError(apiError(403, 'GROUP_CLOSED_BY_ADMIN'))).toBe(true)
  })
})
