import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { fullDashboard, newUserDashboard } from '@/test/dashboardTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { dashboardKey } from '../shared/dashboardCache'
import { useDashboard } from './useDashboard'

describe('useDashboard', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the dashboard from GET /api/dashboard', async () => {
    const fetchMock = stubFetch(Response.json(fullDashboard))

    const { result } = await renderHookWithProviders(() => useDashboard())

    await waitFor(() => {
      expect(result.current.data).toEqual(fullDashboard)
    })
    expect(sentRequest(fetchMock).url).toBe('/api/dashboard')
  })

  it('keeps the dashboard in the cache under the exported key', async () => {
    stubFetch(Response.json(fullDashboard))

    const { result, queryClient } = await renderHookWithProviders(() => useDashboard())

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
    expect(queryClient.getQueryData(dashboardKey)).toEqual(fullDashboard)
    expect(queryClient.getQueryData(['dashboard'])).toEqual(fullDashboard)
  })

  it('is pending before the server has answered', async () => {
    stubFetch(Response.json(newUserDashboard))

    const { result } = await renderHookWithProviders(() => useDashboard())

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('shows the dashboard that is already in the cache straight away', async () => {
    stubFetch(Response.json(newUserDashboard))

    const { result } = await renderHookWithProviders(() => useDashboard(), {
      seedCache: (client) => client.setQueryData(dashboardKey, fullDashboard),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.data).toEqual(fullDashboard)
  })

  it('replaces the saved dashboard with the fresh one when the server answers', async () => {
    stubFetch(Response.json(newUserDashboard))

    const { result } = await renderHookWithProviders(() => useDashboard(), {
      seedCache: (client) => client.setQueryData(dashboardKey, fullDashboard),
    })

    await waitFor(() => {
      expect(result.current.data).toEqual(newUserDashboard)
    })
  })

  it('exposes the ApiError when the server fails', async () => {
    stubFetch(problemResponse(500, 'SOMETHING_BROKE'))

    const { result } = await renderHookWithProviders(() => useDashboard())

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.data).toBeUndefined()
  })

  it('exposes an error naming the field when the answer is malformed', async () => {
    stubFetch(Response.json({ totals: fullDashboard.totals, groups: [], needsYou: [], plusTarget: null }))

    const { result } = await renderHookWithProviders(() => useDashboard())

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('recentActivity')
  })
})
