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
import { seedMe, testMe } from '@/test/testMe'
import { useChangeBudgetCurrency } from './useChangeBudgetCurrency'

const meWithEuroTotal = { ...testMe, budgetTotalCurrency: 'EUR' } as const

describe('useChangeBudgetCurrency', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/me/budget-currency with the chosen currency', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(testMe),
    })

    await act(() => result.current.mutateAsync('EUR'))

    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/me/budget-currency',
      method: 'PUT',
      body: { budgetTotalCurrency: 'EUR' },
    })
  })

  it('sends null when the converted total is turned off', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(meWithEuroTotal),
    })

    await act(() => result.current.mutateAsync(null))

    expect(sentRequest(fetchMock)).toMatchObject({ body: { budgetTotalCurrency: null } })
  })

  it.each(['MKD', 'EUR'] as const)('puts %s in the me cache once the server accepted it, leaving the other fields alone', async (currency) => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(testMe),
    })

    await act(() => result.current.mutateAsync(currency))

    expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, budgetTotalCurrency: currency })
  })

  it('puts null in the me cache when the converted total is turned off', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(meWithEuroTotal),
    })

    await act(() => result.current.mutateAsync(null))

    expect(queryClient.getQueryData(meQueryKey)).toEqual({ ...testMe, budgetTotalCurrency: null })
  })

  it('leaves the me cache as it was before the server answered', async () => {
    const { result, queryClient } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(testMe),
    })
    let cachedWhenAsked: unknown
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(async () => {
        cachedWhenAsked = queryClient.getQueryData(meQueryKey)
        return new Response(null, { status: 204 })
      }),
    )

    await act(() => result.current.mutateAsync('EUR'))

    expect(cachedWhenAsked).toEqual(testMe)
  })

  it('keeps the me cache and exposes the ApiError when the server refuses', async () => {
    stubFetch(problemResponse(400, 'ME_BUDGET_CURRENCY_INVALID'))
    const { result, queryClient } = await renderHookWithProviders(() => useChangeBudgetCurrency(), {
      seedCache: seedMe(testMe),
    })

    const error = await act(() => captureError(result.current.mutateAsync('EUR')))

    expect(error).toBeInstanceOf(ApiError)
    await waitFor(() => {
      expect(result.current.error).toBe(error)
    })
    expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
  })
})
