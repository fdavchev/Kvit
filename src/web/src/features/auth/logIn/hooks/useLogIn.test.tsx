import { act, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { useLogIn } from './useLogIn'

const credentials = { email: 'filip@example.com', password: 'Passw0rdOk' }

describe('useLogIn', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('puts the returned person into the me cache', async () => {
    stubFetch(Response.json(testMe))
    const { result, queryClient } = await renderHookWithProviders(() => useLogIn())

    await act(() => result.current.mutateAsync(credentials))

    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })

  it('switches the screen to the language of the account and remembers it', async () => {
    stubFetch(Response.json({ ...testMe, language: 'mk' }))
    const { result, i18n } = await renderHookWithProviders(() => useLogIn(), {
      language: 'en',
    })

    await act(() => result.current.mutateAsync(credentials))

    expect(i18n.language).toBe('mk')
    expect(window.localStorage.getItem('kvit.language')).toBe('mk')
  })

  it('exposes the ApiError and changes nothing when the log-in is refused', async () => {
    stubFetch(problemResponse(401, 'AUTH_INVALID_CREDENTIALS'))
    const { result, queryClient, i18n } = await renderHookWithProviders(() => useLogIn(), {
      language: 'mk',
    })

    const error = await act(() => captureError(result.current.mutateAsync(credentials)))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(meQueryKey)).toBeUndefined()
    expect(i18n.language).toBe('mk')
  })
})
