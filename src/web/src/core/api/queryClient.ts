import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import type { Persister } from '@tanstack/react-query-persist-client'
import { keepCacheToOnePerson } from '@/core/auth/keepCacheToOnePerson'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { refreshEveryGroup } from '@/features/groups/shared/groupCache'
import { ApiError, forbiddenStatus, unauthorizedStatus } from './apiClient'
import { savedQueryCacheMaxAge } from './savedQueryCache'

const wrongPasswordAtLogInCode = 'AUTH_INVALID_CREDENTIALS'
const mustChangePasswordCode = 'AUTH_MUST_CHANGE_PASSWORD'
export const groupStateChangedCodes: readonly string[] = [
  'GROUP_CLOSING',
  'GROUP_FINISHED',
  'GROUP_NOT_CLOSING',
  'GROUP_NOT_OPEN',
  'GROUP_NOT_FINISHED',
]
const maxQueryRetries = 3
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
        updateMeAfterFailure(queryClient, error)
        refreshGroupsAfterStateRefusal(queryClient, error)
      },
    }),
    defaultOptions: {
      queries: {
        refetchOnWindowFocus: false,
        retry: shouldRetryQuery,
        gcTime: savedQueryCacheMaxAge,
      },
      mutations: { retry: false },
    },
  })
  keepCacheToOnePerson(queryClient, persister)
  return queryClient
}

function shouldRetryQuery(failureCount: number, error: Error): boolean {
  if (error instanceof ApiError && isClientErrorStatus(error.httpStatus) && !retryableClientErrorStatuses.includes(error.httpStatus)) {
    return false
  }
  return failureCount < maxQueryRetries
}

function isClientErrorStatus(httpStatus: number | null): httpStatus is number {
  return httpStatus !== null && httpStatus >= firstClientErrorStatus && httpStatus < firstServerErrorStatus
}

function updateMeAfterFailure(queryClient: QueryClient, error: Error): void {
  if (!(error instanceof ApiError)) {
    return
  }
  if (
    error.httpStatus === unauthorizedStatus &&
    error.errorCode !== wrongPasswordAtLogInCode
  ) {
    queryClient.setQueryData<Me | null>(meQueryKey, null)
    return
  }
  if (
    error.httpStatus === forbiddenStatus &&
    error.errorCode === mustChangePasswordCode
  ) {
    void queryClient.invalidateQueries({ queryKey: meQueryKey })
  }
}

export function isGroupStateChangedError(error: unknown): boolean {
  return error instanceof ApiError && error.errorCode !== null && groupStateChangedCodes.includes(error.errorCode)
}

function refreshGroupsAfterStateRefusal(queryClient: QueryClient, error: Error): void {
  if (isGroupStateChangedError(error)) {
    void refreshEveryGroup(queryClient)
  }
}
