import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { pingHealth } from '@/core/services/health/healthService'
import { useApiHealth } from './useApiHealth'

vi.mock('@/core/services/health/healthService', () => ({
  pingHealth: vi.fn(),
}))

function renderUseApiHealth(): void {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
  renderHook(() => useApiHealth(), { wrapper: Wrapper })
}

describe('useApiHealth', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('logs a failed health ping with its cause', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const failure = new Error('GET /api/health could not reach the server')
    vi.mocked(pingHealth).mockRejectedValue(failure)

    renderUseApiHealth()

    await waitFor(() => {
      expect(consoleError).toHaveBeenCalledWith('The health ping failed', failure)
    })
  })

  it('does not log when the health ping succeeds', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.mocked(pingHealth).mockResolvedValue('Healthy')

    renderUseApiHealth()

    await waitFor(() => {
      expect(pingHealth).toHaveBeenCalled()
    })
    expect(consoleError).not.toHaveBeenCalled()
  })
})
