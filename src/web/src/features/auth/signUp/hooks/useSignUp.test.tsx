import { act, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { useSignUp } from './useSignUp'

const newAccount = {
  displayName: 'Filip',
  email: 'filip@example.com',
  password: 'Passw0rdOk',
}

describe('useSignUp', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('registers with the language the screen is showing', async () => {
    const fetchMock = stubFetch(Response.json(testMe))
    const { result } = await renderHookWithProviders(() => useSignUp(), {
      language: 'mk',
    })

    await act(() => result.current.mutateAsync(newAccount))

    expect(sentRequest(fetchMock).body).toMatchObject({ ...newAccount, language: 'mk' })
  })

  it('puts the new person into the me cache', async () => {
    stubFetch(Response.json(testMe))
    const { result, queryClient } = await renderHookWithProviders(() => useSignUp())

    await act(() => result.current.mutateAsync(newAccount))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('exposes the ApiError and leaves the me cache empty when the email is taken', async () => {
    stubFetch(problemResponse(409, 'AUTH_EMAIL_TAKEN'))
    const { result, queryClient } = await renderHookWithProviders(() => useSignUp())

    const error = await act(() => captureError(result.current.mutateAsync(newAccount)))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(meQueryKey)).toBeUndefined()
  })
})
