import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import type { Persister } from '@tanstack/react-query-persist-client'
import { keepCacheToOnePerson } from '@/core/auth/keepCacheToOnePerson'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { refreshEveryGroup } from '@/features/groups/shared/groupCache'
import { ApiError, forbiddenStatus, unauthorizedStatus } from './apiClient'
import { savedQueryCacheMaxAge } from './savedQueryCache'
import { readIsWaitingAfterWakeVisit, retryWhileServerWakesMilliseconds } from './serverWake'

const codesOfFailedSignInAttempts: readonly string[] = ['AUTH_INVALID_CREDENTIALS', 'AUTH_GOOGLE_TOKEN_INVALID']
const codesThatChangeMe: readonly string[] = ['AUTH_MUST_CHANGE_PASSWORD', 'ACCOUNT_SUSPENDED']
export const groupStateChangedCodes: readonly string[] = [
  'GROUP_CLOSING',
  'GROUP_FINISHED',
  'GROUP_NOT_CLOSING',
  'GROUP_NOT_OPEN',
  'GROUP_NOT_FINISHED',
  'GROUP_CLOSED_BY_ADMIN',
]
const maxQueryRetries = 3
const firstRetryDelayMilliseconds = 1000
const longestRetryDelayMilliseconds = 30_000
const firstClientErrorStatus = 400
const firstServerErrorStatus = 500
const requestTimeoutStatus = 408
const tooEarlyStatus = 425
const retryableClientErrorStatuses: readonly number[] = [requestTimeoutStatus, tooEarlyStatus]

export function createQueryClient(persister: Persister): QueryClient {
  const queryClient: QueryClient = new QueryClient({
    queryCache: new QueryCache({
      onError: (error) => {
        updateMeAfterFailure(queryClient, error)
      },
    }),
    mutationCache: new MutationCache({
      onError: (error) => {
        reactToFailedRequest(queryClient, error)
      },
    }),
    defaultOptions: {
      queries: {
        refetchOnWindowFocus: false,
        retry: shouldRetryQuery,
        retryDelay: queryRetryDelay,
        gcTime: savedQueryCacheMaxAge,
      },
      mutations: { retry: false, networkMode: 'always' },
    },
  })
  keepCacheToOnePerson(queryClient, persister)
  return queryClient
}

function shouldRetryQuery(failureCount: number, error: Error): boolean {
  if (error instanceof ApiError && isClientErrorStatus(error.httpStatus) && !retryableClientErrorStatuses.includes(error.httpStatus)) {
    return false
  }
  if (readIsWaitingAfterWakeVisit()) {
    return true
  }
  return failureCount < maxQueryRetries
}

export function shouldRetryQueryOnlyWhileServerWakes(failureCount: number, error: Error): boolean {
  return readIsWaitingAfterWakeVisit() && shouldRetryQuery(failureCount, error)
}

function queryRetryDelay(failureCount: number): number {
  if (readIsWaitingAfterWakeVisit()) {
    return retryWhileServerWakesMilliseconds
  }
  return Math.min(firstRetryDelayMilliseconds * 2 ** failureCount, longestRetryDelayMilliseconds)
}

function isClientErrorStatus(httpStatus: number | null): httpStatus is number {
  return httpStatus !== null && httpStatus >= firstClientErrorStatus && httpStatus < firstServerErrorStatus
}

export function reactToFailedRequest(queryClient: QueryClient, error: unknown): void {
  updateMeAfterFailure(queryClient, error)
  refreshGroupsAfterStateRefusal(queryClient, error)
}

function updateMeAfterFailure(queryClient: QueryClient, error: unknown): void {
  if (!(error instanceof ApiError)) {
    return
  }
  if (
    error.httpStatus === unauthorizedStatus &&
    (error.errorCode === null || !codesOfFailedSignInAttempts.includes(error.errorCode))
  ) {
    queryClient.setQueryData<Me | null>(meQueryKey, null)
    return
  }
  if (
    error.httpStatus === forbiddenStatus &&
    error.errorCode !== null &&
    codesThatChangeMe.includes(error.errorCode)
  ) {
    void queryClient.invalidateQueries({ queryKey: meQueryKey })
  }
}

export function isGroupStateChangedError(error: unknown): boolean {
  return error instanceof ApiError && error.errorCode !== null && groupStateChangedCodes.includes(error.errorCode)
}

function refreshGroupsAfterStateRefusal(queryClient: QueryClient, error: unknown): void {
  if (isGroupStateChangedError(error)) {
    void refreshEveryGroup(queryClient)
  }
}
