import { act, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, requestCountTo, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { personalEntriesQueryKey } from '../../entries/personalEntryCache'
import type { SaveOnce } from '../../categories/hooks/useSaveOnce'
import { useRemoveLimit } from './useRemoveLimit'

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

describe('useRemoveLimit', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/personal/limits/month when the category is null', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useRemoveLimit(freeToSave()), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: null }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({ url: '/api/personal/limits/month', method: 'DELETE' })
  })

  it('sends DELETE /api/personal/limits/categories/{id} when a category is given', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const { result } = await renderHookWithProviders(() => useRemoveLimit(freeToSave()), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: coffeeCategory.id }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(sentRequest(fetchMock)).toMatchObject({
      url: `/api/personal/limits/categories/${coffeeCategory.id}`,
      method: 'DELETE',
    })
  })

  it('marks the cached month answers of every month as out of date once the server removed the limit', async () => {
    stubFetch(new Response(null, { status: 204 }))
    const { result, queryClient } = await renderHookWithProviders(() => useRemoveLimit(freeToSave()), {
      seedCache: seedTwoMonths,
    })
    const onDone = vi.fn<() => void>()

    act(() => {
      result.current.run({ categoryId: coffeeCategory.id }, onDone, vi.fn())
    })

    await waitFor(() => {
      expect(onDone).toHaveBeenCalledOnce()
    })
    expect(isInvalidated(queryClient, octoberKey)).toBe(true)
    expect(isInvalidated(queryClient, septemberKey)).toBe(true)
  })

  it('calls onRefused with the ApiError, keeps the cache and exposes the error when the server refuses', async () => {
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))
    const { result, queryClient } = await renderHookWithProviders(() => useRemoveLimit(freeToSave()), {
      seedCache: seedTwoMonths,
    })
    const onDone = vi.fn<() => void>()
    const onRefused = vi.fn<(error: Error) => void>()

    act(() => {
      result.current.run({ categoryId: null }, onDone, onRefused)
    })

    await waitFor(() => {
      expect(onRefused).toHaveBeenCalledOnce()
    })
    expect(onRefused.mock.calls[0][0]).toBeInstanceOf(ApiError)
    expect(onRefused.mock.calls[0][0]).toMatchObject({ httpStatus: 403, errorCode: 'GUEST_NOT_ALLOWED' })
    expect(onDone).not.toHaveBeenCalled()
    expect(isInvalidated(queryClient, octoberKey)).toBe(false)
    await waitFor(() => {
      expect(result.current.error).toBe(onRefused.mock.calls[0][0])
    })
  })

  it('reports isPending while the request is out', async () => {
    const { result } = await renderHookWithProviders(() => useRemoveLimit(freeToSave()), { seedCache: seedTwoMonths })
    vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))

    act(() => {
      result.current.run({ categoryId: null }, vi.fn(), vi.fn())
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })
  })

  it('sends nothing and calls neither callback while another save holds the one-at-a-time lock', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const busy: SaveOnce = { tryStart: vi.fn<() => boolean>(() => false), finish: vi.fn<() => void>() }
    const { result } = await renderHookWithProviders(() => useRemoveLimit(busy), { seedCache: seedTwoMonths })
    const onDone = vi.fn<() => void>()
    const onRefused = vi.fn<(error: Error) => void>()

    act(() => {
      result.current.run({ categoryId: null }, onDone, onRefused)
    })

    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(requestCountTo(fetchMock, '/api/personal/limits/month')).toBe(0)
    expect(onDone).not.toHaveBeenCalled()
    expect(onRefused).not.toHaveBeenCalled()
  })

  it('lets the lock go once after a success and once after a refusal', async () => {
    const successLock = freeToSave()
    stubFetch(new Response(null, { status: 204 }))
    const success = await renderHookWithProviders(() => useRemoveLimit(successLock), { seedCache: seedTwoMonths })
    act(() => {
      success.result.current.run({ categoryId: null }, vi.fn(), vi.fn())
    })
    await waitFor(() => {
      expect(successLock.finish).toHaveBeenCalledOnce()
    })

    const refusalLock = freeToSave()
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))
    const refusal = await renderHookWithProviders(() => useRemoveLimit(refusalLock), { seedCache: seedTwoMonths })
    act(() => {
      refusal.result.current.run({ categoryId: null }, vi.fn(), vi.fn())
    })
    await waitFor(() => {
      expect(refusalLock.finish).toHaveBeenCalledOnce()
    })
  })
})
