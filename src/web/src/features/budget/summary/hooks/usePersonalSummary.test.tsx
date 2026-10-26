import { waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { problemResponse, requestCountTo, stubFetch, stubFetchByPath } from '@/test/apiTestHelpers'
import { octoberSummary, summaryOf } from '@/test/personalSummaryTestData'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { personalSummaryQueryKey } from '../personalSummaryCache'
import { usePersonalSummary } from './usePersonalSummary'

const octoberUrl = '/api/personal/summary?month=2026-10'
const septemberUrl = '/api/personal/summary?month=2026-09'

describe('usePersonalSummary', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is pending first and then holds the summary of the month', async () => {
    stubFetch(Response.json(octoberSummary))

    const { result } = await renderHookWithProviders(() => usePersonalSummary('2026-10'))

    expect(result.current.isPending).toBe(true)
    await waitFor(() => {
      expect(result.current.data).toEqual(octoberSummary)
    })
  })

  it('asks the server once, for the month it is given', async () => {
    const fetchMock = stubFetchByPath({
      [octoberUrl]: Response.json(octoberSummary),
      [septemberUrl]: Response.json(summaryOf('2026-09', '2026-08', [])),
    })

    const { result } = await renderHookWithProviders(() => usePersonalSummary('2026-09'))
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(requestCountTo(fetchMock, septemberUrl)).toBe(1)
    expect(requestCountTo(fetchMock, octoberUrl)).toBe(0)
  })

  it('saves the answer under the key of its month and under no other', async () => {
    stubFetch(Response.json(octoberSummary))

    const { result, queryClient } = await renderHookWithProviders(() => usePersonalSummary('2026-10'))
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(queryClient.getQueryData(personalSummaryQueryKey('2026-10'))).toEqual(octoberSummary)
    expect(queryClient.getQueryData(personalSummaryQueryKey('2026-09'))).toBeUndefined()
  })

  it('shows the saved summary of the month at once while it asks again', async () => {
    stubFetch(Response.json(octoberSummary))

    const { result } = await renderHookWithProviders(() => usePersonalSummary('2026-10'), {
      seedCache: (queryClient) => queryClient.setQueryData(personalSummaryQueryKey('2026-10'), octoberSummary),
    })

    expect(result.current.data).toEqual(octoberSummary)
    await waitFor(() => {
      expect(result.current.isFetching).toBe(false)
    })
  })

  it('holds the error when the answer is refused', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_MONTH_INVALID'))

    const { result } = await renderHookWithProviders(() => usePersonalSummary('2026-10'))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_MONTH_INVALID' })
  })

  it('holds the error naming the field when the answer cannot be read', async () => {
    stubFetch(Response.json({ ...octoberSummary, currencies: null }))

    const { result } = await renderHookWithProviders(() => usePersonalSummary('2026-10'))

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
    expect(result.current.error?.message).toContain('currencies')
  })
})
