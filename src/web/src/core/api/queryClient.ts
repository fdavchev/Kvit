import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import {
  ApiError,
  forbiddenStatus,
  notFoundStatus,
  tooManyRequestsStatus,
  unauthorizedStatus,
} from './apiClient'

const wrongPasswordAtLogInCode = 'AUTH_INVALID_CREDENTIALS'
const mustChangePasswordCode = 'AUTH_MUST_CHANGE_PASSWORD'
const maxQueryRetries = 3

export function createQueryClient(): QueryClient {
  const queryClient: QueryClient = new QueryClient({
    queryCache: new QueryCache({
      onError: (error) => {
        updateMeAfterFailure(queryClient, error)
      },
    }),
    mutationCache: new MutationCache({
      onError: (error) => {
        updateMeAfterFailure(queryClient, error)
      },
    }),
    defaultOptions: {
      queries: { refetchOnWindowFocus: false, retry: shouldRetryQuery },
      mutations: { retry: false },
    },
  })
  return queryClient
}

function shouldRetryQuery(failureCount: number, error: Error): boolean {
  if (
    error instanceof ApiError &&
    (error.httpStatus === notFoundStatus ||
      error.httpStatus === tooManyRequestsStatus)
  ) {
    return false
  }
  return failureCount < maxQueryRetries
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
