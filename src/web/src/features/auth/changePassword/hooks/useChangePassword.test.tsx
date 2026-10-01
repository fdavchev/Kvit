import { QueryObserver, type QueryClient } from '@tanstack/react-query'
import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { useChangePassword } from './useChangePassword'

const mustChangePasswordMe: Me = { ...testMe, mustChangePassword: true }
const passwords = { currentPassword: 'Temp0rary1', newPassword: 'Passw0rdOk' }

function seedPersonWhoMustChangePassword(queryClient: QueryClient): void {
  queryClient.setQueryData<Me | null>(meQueryKey, mustChangePasswordMe)
}

describe('useChangePassword', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('marks the person in the me cache as no longer having to change the password', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(
      () => useChangePassword(),
      { seedCache: seedPersonWhoMustChangePassword },
    )

    await act(() => result.current.mutateAsync(passwords))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('does not ask the server for me again, so the screen does not flicker', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(
      () => useChangePassword(),
      { seedCache: seedPersonWhoMustChangePassword },
    )
    const askForMe = vi.fn(async (): Promise<Me | null> => testMe)
    const observer = new QueryObserver(queryClient, {
      queryKey: meQueryKey,
      queryFn: askForMe,
      staleTime: Infinity,
    })
    const unsubscribe = observer.subscribe(() => {})

    await act(() => result.current.mutateAsync(passwords))

    expect(askForMe).not.toHaveBeenCalled()
    expect(fetchMock).toHaveBeenCalledTimes(1)
    unsubscribe()
  })

  it('exposes the ApiError and keeps the cache when the current password is wrong', async () => {
    stubFetch(problemResponse(400, 'AUTH_CURRENT_PASSWORD_WRONG'))
    const { result, queryClient } = await renderHookWithProviders(
      () => useChangePassword(),
      { seedCache: seedPersonWhoMustChangePassword },
    )

    const error = await act(() => captureError(result.current.mutateAsync(passwords)))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(meQueryKey)).toEqual(mustChangePasswordMe)
  })
})
