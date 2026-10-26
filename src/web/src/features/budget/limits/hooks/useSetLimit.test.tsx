import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, requestCountTo, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { personalEntriesQueryKey } from '../../entries/personalEntryCache'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useSetLimit } from './useSetLimit'

const octoberKey = personalEntriesQueryKey('2026-10')
const septemberKey = personalEntriesQueryKey('2026-09')

function seedTwoMonths(queryClient: QueryClient): void {
  queryClient.setQueryData(octoberKey, 'the October answer')
  queryClient.setQueryData(septemberKey, 'the September answer')
}

function freeToSave(): SaveOnce {
  return { tryStart: vi.fn<() => boolean>(() => true), finish: vi.fn<() => void>() }
}

function isInvalidated(queryClient: QueryClient, key: readonly unknown[]): boolean {
  return queryClient.getQueryState(key)?.isInvalidated === true
}

describe('useSetLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/limits/month with limitMinor when the category is null', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useSetLimit(freeToSave()), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: '/api/personal/limits/month',
      method: 'PUT',
      body: { limitMinor: 2500000 },
    })
  })

  it('sends PUT /api/personal/limits/categories/{id} with limitMinor when a category is given', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useSetLimit(freeToSave()), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: coffeeCategory.id, limitMinor: 450000 }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/personal/limits/categories/${coffeeCategory.id}`,
      method: 'PUT',
      body: { limitMinor: 450000 },
    })
  })

  it('marks the cached month answers of every month as out of date once the server accepted the limit', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useSetLimit(freeToSave()), {
      seedCache: seedTwoMonths,
    })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: coffeeCategory.id, limitMinor: 450000 }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(isInvalidated(queryClient, octoberKey)).toBe(true)
    expect(isInvalidated(queryClient, septemberKey)).toBe(true)
  })

  it('leaves the cached month answers alone before the server answered', async () => {
    const { result, queryClient } = await renderHookWithProviders(() => useSetLimit(freeToSave()), {
      seedCache: seedTwoMonths,
    })
    let wasInvalidatedWhenAsked: boolean | null = null
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(async () => {
        wasInvalidatedWhenAsked = isInvalidated(queryClient, octoberKey)
        return new Response(null, { status: 204 })
      }),
    )
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(wasInvalidatedWhenAsked).toBe(false)
  })

  it('reports isPending while the request is out', async () => {
    const { result } = await renderHookWithProviders(() => useSetLimit(freeToSave()), { seedCache: seedTwoMonths })
    vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, vi.fn(), vi.fn())
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })
  })

  it('calls onRefused with the ApiError, keeps the cache and exposes the error when the server refuses', async () => {
    stubFetch(problemResponse(400, 'BUDGET_LIMIT_AMOUNT_INVALID'))
    const { result, queryClient } = await renderHookWithProviders(() => useSetLimit(freeToSave()), {
      seedCache: seedTwoMonths,
    })
    const onDone = vi.fn<() => void>()
    const onRefused = vi.fn<(error: Error) => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, onDone, onRefused)
    })

    await waitFor(() => {
      expect(onRefused).toHaveBeenCalledOnce()
    })
    expect(onRefused.mock.calls[0][0]).toBeInstanceOf(ApiError)
    expect(onRefused.mock.calls[0][0]).toMatchObject({ httpStatus: 400, errorCode: 'BUDGET_LIMIT_AMOUNT_INVALID' })
    expect(onDone).not.toHaveBeenCalled()
    expect(isInvalidated(queryClient, octoberKey)).toBe(false)
    await waitFor(() => {
      expect(result.current.error).toBe(onRefused.mock.calls[0][0])
    })
  })

  it('calls onRefused with an ApiError that has no status when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))
    const { result } = await renderHookWithProviders(() => useSetLimit(freeToSave()), { seedCache: seedTwoMonths })
    const onRefused = vi.fn<(error: Error) => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, vi.fn(), onRefused)
    })

    await waitFor(() => {
      expect(onRefused).toHaveBeenCalledOnce()
    })
    expect(onRefused.mock.calls[0][0]).toMatchObject({ httpStatus: null })
  })

  it('sends nothing and calls neither callback while another save holds the one-at-a-time lock', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const busy: SaveOnce = { tryStart: vi.fn<() => boolean>(() => false), finish: vi.fn<() => void>() }
    const { result } = await renderHookWithProviders(() => useSetLimit(busy), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()
    const onRefused = vi.fn<(error: Error) => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, onDone, onRefused)
    })

    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(requestCountTo(fetchMock, '/api/personal/limits/month')).toBe(0)
    expect(onDone).not.toHaveBeenCalled()
    expect(onRefused).not.toHaveBeenCalled()
  })

  it('lets the lock go once after a success', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const lock = freeToSave()
    const { result } = await renderHookWithProviders(() => useSetLimit(lock), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(lock.finish).toHaveBeenCalledOnce()
    })
    expect(lock.tryStart).toHaveBeenCalledOnce()
  })

  it('lets the lock go once after a refusal', async () => {
    stubFetch(problemResponse(400, 'BUDGET_LIMIT_AMOUNT_INVALID'))
    const lock = freeToSave()
    const { result } = await renderHookWithProviders(() => useSetLimit(lock), { seedCache: seedTwoMonths })

    act(() => {
      result.current.run({ categoryId: null, limitMinor: 2500000 }, vi.fn(), vi.fn())
    })

    await waitFor(() => {
      expect(lock.finish).toHaveBeenCalledOnce()
    })
  })
})
