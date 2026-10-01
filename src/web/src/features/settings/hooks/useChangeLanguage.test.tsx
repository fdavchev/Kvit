import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'
import { captureError, problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { testMe } from '@/test/testMe'
import { useChangeLanguage } from './useChangeLanguage'

function seedSignedInPerson(queryClient: QueryClient): void {
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
}

describe('useChangeLanguage', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks the server before it changes the screen language', async () => {
    const { result, i18n } = await renderHookWithProviders(() => useChangeLanguage(), {
      seedCache: seedSignedInPerson,
    })
    let screenLanguageWhenAsked = ''
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(async () => {
        screenLanguageWhenAsked = i18n.language
        return new Response(null, { status: 204 })
      }),
    )

    await act(() => result.current.mutateAsync('mk'))

    expect(screenLanguageWhenAsked).toBe('en')
    expect(i18n.language).toBe('mk')
  })

  it('remembers the new language on the phone once the server accepted it', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useChangeLanguage(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync('mk'))

    expect(window.localStorage.getItem('kvit.language')).toBe('mk')
  })

  it('updates the language of the person in the me cache once the server accepted it', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeLanguage(), {
      seedCache: seedSignedInPerson,
    })

    await act(() => result.current.mutateAsync('mk'))

    expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, language: 'mk' })
  })

  it('keeps the screen language, the cache and the saved choice and exposes the ApiError when the server refuses', async () => {
    stubFetch(problemResponse(400, 'LANGUAGE_INVALID'))
    const { result, queryClient, i18n } = await renderHookWithProviders(
      () => useChangeLanguage(),
      { seedCache: seedSignedInPerson },
    )

    const error = await act(() => captureError(result.current.mutateAsync('mk')))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(i18n.language).toBe('en')
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    expect(window.localStorage.getItem('kvit.language')).toBeNull()
  })
})
