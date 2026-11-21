import type { QueryClient } from '@tanstack/react-query'
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import type { RouteObject } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { createI18n } from '@/core/i18n/i18n'
import { languages, type Language } from '@/core/i18n/language'
import type { Me } from '@/core/services/me/meService'
import { stubFetch } from '@/test/apiTestHelpers'
import { testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { routes } from './routes'

const wakeUrl = 'https://kvit-mk-api.onrender.com/wake'
const beforeThePageOpened = performance.timeOrigin - 60_000
const twoMinutes = 2 * 60 * 1000

let assign: Mock<(url: string) => void>

function asleepAnswer(): Response {
  return new Response('Kvit proxy: the API server is asleep', {
    status: 503,
    headers: { 'content-type': 'text/plain; charset=utf-8', 'x-kvit-server-asleep': '1', 'x-kvit-wake-url': wakeUrl },
  })
}

function stubLocation(pathname: string, search = ''): void {
  vi.stubGlobal('location', { pathname, search, assign })
}

function rememberWakeVisitJustNow(): void {
  window.sessionStorage.setItem('kvit.lastWakeVisitAt', String(Date.now()))
}

async function loadFreshModules() {
  vi.resetModules()
  const serverWake = await import('@/core/api/serverWake')
  const { ServerAsleepBar } = await import('./ServerAsleepBar')
  return { serverWake, ServerAsleepBar }
}

async function renderBar(language: Language = 'en') {
  const modules = await loadFreshModules()
  const i18n = await createI18n(language)
  render(
    <I18nextProvider i18n={i18n}>
      <modules.ServerAsleepBar />
    </I18nextProvider>,
  )
  return modules.serverWake
}

function wakeButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'wakingUp.wake') })
}

beforeEach(() => {
  assign = vi.fn<(url: string) => void>()
  window.sessionStorage.clear()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  window.sessionStorage.clear()
})

describe('ServerAsleepBar', () => {
  it('shows nothing while no answer said the server is asleep', async () => {
    stubLocation('/groups')

    await renderBar()

    expect(document.querySelector('[data-server-asleep-bar]')).toBeNull()
  })

  it.each(languages)('shows in %s that the server is asleep, with one Wake the server button', async (language) => {
    stubLocation('/groups')
    const serverWake = await renderBar(language)

    act(() => serverWake.followServerAnswer('POST', endpoints.groups, asleepAnswer()))

    expect(screen.getByRole('status').textContent).toContain(translated(language, 'wakingUp.asleep'))
    expect(screen.getAllByRole('button')).toHaveLength(1)
    expect(wakeButton(language)).toBeTruthy()
  })

  it('never leaves the page by itself when a later answer says the server is asleep', async () => {
    stubLocation('/groups/7/expenses/new')
    const serverWake = await renderBar()

    act(() => serverWake.followServerAnswer('POST', endpoints.groups, asleepAnswer()))

    expect(assign).not.toHaveBeenCalled()
  })

  it('sends the page through the wake address with the current path when the button is tapped', async () => {
    stubLocation('/groups/7/expenses/new', '?currency=EUR')
    const serverWake = await renderBar()
    act(() => serverWake.followServerAnswer('POST', endpoints.groups, asleepAnswer()))

    fireEvent.click(wakeButton())

    expect(assign).toHaveBeenCalledExactlyOnceWith(`${wakeUrl}?returnTo=${encodeURIComponent('/groups/7/expenses/new?currency=EUR')}`)
  })

  it('goes away when the server answers again', async () => {
    stubLocation('/groups')
    const serverWake = await renderBar()
    act(() => serverWake.followServerAnswer('POST', endpoints.groups, asleepAnswer()))

    act(() => serverWake.followServerAnswer('GET', endpoints.groups, new Response(null, { status: 204 })))

    expect(document.querySelector('[data-server-asleep-bar]')).toBeNull()
  })
})

describe('opening the app while the server is asleep', () => {
  function restoredMe(me: Me): (queryClient: QueryClient) => void {
    return (queryClient) => {
      queryClient.setQueryData<Me | null>(meQueryKey, me, { updatedAt: beforeThePageOpened })
    }
  }

  async function openTheApp(path: string, seedCache?: (queryClient: QueryClient) => void) {
    vi.resetModules()
    const { RequireAuth } = await import('./RequireAuth')
    const { ServerAsleepBar } = await import('./ServerAsleepBar')
    const { checkRestoredMeWithServer } = await import('@/core/api/savedQueryCache')
    const { renderRoutesWithProviders } = await import('@/test/renderWithProviders')
    const signedInRoutes: RouteObject[] = [
      {
        element: (
          <>
            <RequireAuth />
            <ServerAsleepBar />
          </>
        ),
        children: [{ path, element: <p>saved groups page</p> }],
      },
    ]
    return renderRoutesWithProviders(signedInRoutes, path, {
      seedCache: (queryClient) => {
        seedCache?.(queryClient)
        void checkRestoredMeWithServer(queryClient)
      },
    })
  }

  async function waitForTheMeCheckToFail(queryClient: QueryClient): Promise<void> {
    await waitFor(() => {
      expect(queryClient.getQueryState(meQueryKey)?.status).toBe('error')
    })
  }

  function rememberWakeVisitEndingIn(milliseconds: number): void {
    window.sessionStorage.setItem('kvit.lastWakeVisitAt', String(Date.now() - twoMinutes + milliseconds))
  }

  function tryAgainButton(): HTMLElement | null {
    return screen.queryByRole('button', { name: translated('en', 'common.retry') })
  }

  function asleepBar(): Element | null {
    return document.querySelector('[data-server-asleep-bar]')
  }

  it('shows the saved data and sends the whole page through the wake address at once', async () => {
    stubLocation(routes.groups)
    stubFetch(asleepAnswer())

    await openTheApp(routes.groups, restoredMe(testMe))

    expect(screen.getByText('saved groups page')).toBeTruthy()
    await waitFor(() => {
      expect(assign).toHaveBeenCalledExactlyOnceWith(`${wakeUrl}?returnTo=${encodeURIComponent(routes.groups)}`)
    })
  })

  it('sends a person without saved data through the wake address at once too', async () => {
    stubLocation(routes.dashboard)
    stubFetch(asleepAnswer())

    await openTheApp(routes.dashboard)

    await waitFor(() => {
      expect(assign).toHaveBeenCalledExactlyOnceWith(`${wakeUrl}?returnTo=${encodeURIComponent(routes.dashboard)}`)
    })
  })

  it('comes back from the wake page still asleep within 2 minutes: stays, keeps the saved data and waits without the Wake the server bar', async () => {
    stubLocation(routes.groups)
    rememberWakeVisitJustNow()
    stubFetch(asleepAnswer())

    const { queryClient } = await openTheApp(routes.groups, restoredMe(testMe))
    await waitForTheMeCheckToFail(queryClient)

    expect(screen.getByText('saved groups page')).toBeTruthy()
    expect(asleepBar()).toBeNull()
    expect(assign).not.toHaveBeenCalled()
  })

  it('comes back from the wake page still asleep within 2 minutes without saved data: keeps showing Waking up instead of Try again', async () => {
    stubLocation(routes.dashboard)
    rememberWakeVisitJustNow()
    stubFetch(asleepAnswer())

    const { queryClient } = await openTheApp(routes.dashboard)
    await waitForTheMeCheckToFail(queryClient)

    expect(screen.getByRole('status')).toBeTruthy()
    expect(tryAgainButton()).toBeNull()
    expect(asleepBar()).toBeNull()
    expect(assign).not.toHaveBeenCalled()
  })

  it('shows the Wake the server bar and Try again as today once the 2 minutes after the wake visit are over with the server still asleep', async () => {
    stubLocation(routes.dashboard)
    rememberWakeVisitEndingIn(200)
    stubFetch(asleepAnswer())

    const { queryClient } = await openTheApp(routes.dashboard)
    await waitForTheMeCheckToFail(queryClient)

    await waitFor(
      () => {
        expect(asleepBar()).not.toBeNull()
        expect(tryAgainButton()).not.toBeNull()
      },
      { timeout: 3000 },
    )
    expect(assign).not.toHaveBeenCalled()
  })

  it('shows Try again and the bar at once when there was no wake visit and the automatic one cannot be remembered', async () => {
    stubLocation(routes.dashboard)
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is full', 'QuotaExceededError')
    })
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    stubFetch(asleepAnswer())

    const { queryClient } = await openTheApp(routes.dashboard)
    await waitForTheMeCheckToFail(queryClient)

    expect(await screen.findByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    expect(asleepBar()).not.toBeNull()
    expect(assign).not.toHaveBeenCalled()
  })
})
