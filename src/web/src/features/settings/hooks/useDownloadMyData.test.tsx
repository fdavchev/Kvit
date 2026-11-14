import { act, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, stubFetch, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { stubFileDownload, type FileDownloadStub } from '@/test/fileDownloadTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { useDownloadMyData } from './useDownloadMyData'

function exportResponse(): Response {
  return new Response('{"profile":{}}', {
    status: 200,
    headers: {
      'content-type': 'application/json',
      'content-disposition': 'attachment; filename="kvit-my-data-2026-11-04.json"',
    },
  })
}

describe('useDownloadMyData', () => {
  let download: FileDownloadStub

  beforeEach(() => {
    download = stubFileDownload()
  })

  afterEach(() => {
    download.restore()
    vi.unstubAllGlobals()
  })

  it('saves the exported data as a file with the name the server gave, and resolves with nothing', async () => {
    stubFetch(exportResponse())
    const { result } = await renderHookWithProviders(() => useDownloadMyData(), { seedCache: seedMe(testMe) })

    const answer = await act(() => result.current.mutateAsync())

    expect(answer).toBeUndefined()
    expect(download.savedFiles).toHaveLength(1)
    expect(download.savedFiles[0].fileName).toBe('kvit-my-data-2026-11-04.json')
    expect(await download.createdBlobs[0].text()).toBe('{"profile":{}}')
  })

  it('sends no request until it is asked to', async () => {
    const fetchMock = stubFetch(exportResponse())

    await renderHookWithProviders(() => useDownloadMyData(), { seedCache: seedMe(testMe) })

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('reports isPending while the server has not answered, and creates no file yet', async () => {
    stubFetchThatNeverAnswers()
    const { result } = await renderHookWithProviders(() => useDownloadMyData(), { seedCache: seedMe(testMe) })

    act(() => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })
    expect(download.savedFiles).toEqual([])
  })

  it('exposes the ApiError and creates no file when the server answers RATE_LIMITED', async () => {
    stubFetch(problemResponse(429, 'RATE_LIMITED'))
    const { result } = await renderHookWithProviders(() => useDownloadMyData(), { seedCache: seedMe(testMe) })

    const error = await act(() => captureError(result.current.mutateAsync()))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 429, errorCode: 'RATE_LIMITED' })
    await waitFor(() => {
      expect(result.current.isPending).toBe(false)
    })
    expect(download.createdBlobs).toEqual([])
    expect(download.savedFiles).toEqual([])
  })

  it('can be used again after a failure', async () => {
    stubFetch(new TypeError('Failed to fetch'), exportResponse())
    const { result } = await renderHookWithProviders(() => useDownloadMyData(), { seedCache: seedMe(testMe) })
    await act(() => captureError(result.current.mutateAsync()))

    await act(() => result.current.mutateAsync())

    expect(download.savedFiles.map((file) => file.fileName)).toEqual(['kvit-my-data-2026-11-04.json'])
  })
})
