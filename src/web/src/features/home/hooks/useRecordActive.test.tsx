import { fireEvent, screen, waitFor } from '@testing-library/react'
import { useState } from 'react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { activePath } from '@/test/dashboardTestData'
import { renderElementWithProviders, renderHookWithProviders } from '@/test/renderWithProviders'
import {
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { useRecordActive } from './useRecordActive'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function Probe() {
  const [renders, setRenders] = useState<number>(0)
  useRecordActive()
  return (
    <button type="button" onClick={() => setRenders(renders + 1)}>
      {`render ${renders}`}
    </button>
  )
}

function stubActive(answer: AnswerFactory) {
  return stubFetchByRequest({ [`POST ${activePath}`]: answer })
}

describe('useRecordActive', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('sends POST /api/me/active with no body once when it is mounted', async () => {
    const fetchMock = stubActive(noContentAnswer())

    await renderHookWithProviders(() => useRecordActive())

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', activePath)[0].body).toBeUndefined()
  })

  it('sends nothing before it is mounted', async () => {
    const fetchMock = stubActive(noContentAnswer())

    await renderElementWithProviders(<p>no hook here</p>, '/')

    expect(requestCount(fetchMock, 'POST', activePath)).toBe(0)
  })

  it('does not send it again when the component renders again', async () => {
    const fetchMock = stubActive(noContentAnswer())
    await renderElementWithProviders(<Probe />, '/')
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })

    fireEvent.click(screen.getByRole('button', { name: 'render 0' }))
    fireEvent.click(await screen.findByRole('button', { name: 'render 1' }))
    await screen.findByRole('button', { name: 'render 2' })

    expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
  })

  it('keeps the component working and writes nothing to the console when the server answers 204', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const fetchMock = stubActive(noContentAnswer())

    await renderElementWithProviders(<Probe />, '/')
    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
    })

    expect(screen.getByRole('button', { name: 'render 0' })).toBeTruthy()
    expect(consoleError).not.toHaveBeenCalled()
  })

  it.each([
    ['a server error', problemAnswer(500, 'SOMETHING_BROKE')],
    ['a refused session', problemAnswer(401, 'AUTH_NOT_SIGNED_IN')],
    ['a network failure', () => new TypeError('Failed to fetch')],
  ])('logs the ApiError with console.error and shows no toast after %s', async (_name, answer) => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    stubActive(answer)

    await renderElementWithProviders(<Probe />, '/')

    await waitFor(() => {
      expect(consoleError).toHaveBeenCalled()
    })
    expect(consoleError.mock.calls.flat().some((value) => value instanceof ApiError)).toBe(true)
    expect(vi.mocked(toast).mock.calls).toEqual([])
    expect(vi.mocked(toast.error).mock.calls).toEqual([])
  })

  it('does not crash the component and does not try again after a failure', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const fetchMock = stubActive(problemAnswer(500, 'SOMETHING_BROKE'))
    await renderElementWithProviders(<Probe />, '/')
    await waitFor(() => {
      expect(consoleError).toHaveBeenCalled()
    })

    fireEvent.click(screen.getByRole('button', { name: 'render 0' }))
    await screen.findByRole('button', { name: 'render 1' })

    expect(requestCount(fetchMock, 'POST', activePath)).toBe(1)
  })
})
