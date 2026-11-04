import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { useIsAdmin } from './useIsAdmin'

describe('useIsAdmin', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is true for a signed-in person whose me says isAdmin is true', async () => {
    const { result } = await renderHookWithProviders(() => useIsAdmin(), { seedCache: seedMe(testAdminMe) })

    expect(result.current).toBe(true)
  })

  it('is false for a signed-in person who is not an admin', async () => {
    const { result } = await renderHookWithProviders(() => useIsAdmin(), { seedCache: seedMe(testMe) })

    expect(result.current).toBe(false)
  })

  it('is false for a guest', async () => {
    const { result } = await renderHookWithProviders(() => useIsAdmin(), { seedCache: seedMe(testGuestMe) })

    expect(result.current).toBe(false)
  })

  it('is false when nobody is signed in', async () => {
    const { result } = await renderHookWithProviders(() => useIsAdmin(), { seedCache: seedMe(null) })

    expect(result.current).toBe(false)
  })

  it('is false while me is still being asked', async () => {
    stubFetchThatNeverAnswers()

    const { result } = await renderHookWithProviders(() => useIsAdmin())

    expect(result.current).toBe(false)
  })
})
