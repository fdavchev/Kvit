import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderSettings(me: Me, language: Language = 'en') {
  return renderRoutesWithProviders(
    [
      { path: routes.settings, element: <SettingsScreen /> },
      { path: routes.makeAccount, element: <p>make account page</p> },
    ],
    routes.settings,
    { language, seedCache: seedMe({ ...me, language }) },
  )
}

function logOutButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'settings.logOut') })
}

function logOutSheet(language: Language = 'en'): HTMLElement {
  return screen.getByRole('dialog', { name: translated(language, 'settings.guest.logOutTitle') })
}

function openLogOutSheet(language: Language = 'en'): HTMLElement {
  fireEvent.click(logOutButton(language))
  return logOutSheet(language)
}

function logOutAnywayButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: translated(language, 'settings.guest.logOutAnyway') })
}

describe('SettingsScreen for a guest', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('the guest card', () => {
    it.each(languages)('shows "You\'re a guest" with the line about making an account (%s)', async (language) => {
      await renderSettings(testGuestMe, language)

      expect(screen.getByRole('heading', { name: translated(language, 'settings.guest.title') })).toBeTruthy()
      expect(screen.getByText(translated(language, 'settings.guest.body'))).toBeTruthy()
    })

    it.each(languages)('has a Make an account button that leads to the make-account screen (%s)', async (language) => {
      await renderSettings(testGuestMe, language)

      const link = screen.getByRole('link', { name: translated(language, 'settings.guest.makeAccount') })

      expect(link.getAttribute('href')).toBe(routes.makeAccount)
    })

    it('opens the make-account screen with one tap on Make an account', async () => {
      const { router } = await renderSettings(testGuestMe)

      fireEvent.click(screen.getByRole('link', { name: translated('en', 'settings.guest.makeAccount') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.makeAccount)
      })
    })

    it('shows neither Change password nor Set a password to a guest', async () => {
      await renderSettings(testGuestMe)

      expect(screen.queryByRole('link', { name: translated('en', 'auth.changePassword.title') })).toBeNull()
      expect(screen.queryByRole('link', { name: translated('en', 'auth.setPassword.title') })).toBeNull()
    })

    it('still has the Privacy link for a guest', async () => {
      await renderSettings(testGuestMe)

      expect(screen.getByRole('link', { name: translated('en', 'common.privacy') }).getAttribute('href')).toBe(routes.privacy)
    })

    it.each([true, false])('shows no guest card to a person with an account when hasPassword is %s', async (hasPassword) => {
      await renderSettings({ ...testMe, hasPassword })

      expect(screen.queryByText(translated('en', 'settings.guest.title'))).toBeNull()
      expect(screen.queryByRole('link', { name: translated('en', 'settings.guest.makeAccount') })).toBeNull()
    })
  })

  describe('Log out', () => {
    it('opens the warning sheet instead of logging out at once, and sends no request', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      await renderSettings(testGuestMe)

      openLogOutSheet()

      expect(logOutSheet()).toBeTruthy()
      expect(fetchMock).not.toHaveBeenCalled()
    })

    it.each(languages)('shows the warning text, Make an account, Log out anyway and Cancel in the sheet (%s)', async (language) => {
      await renderSettings(testGuestMe, language)

      const sheet = openLogOutSheet(language)

      expect(within(sheet).getByText(translated(language, 'settings.guest.logOutBody'))).toBeTruthy()
      expect(
        within(sheet).getByRole('link', { name: translated(language, 'settings.guest.makeAccount') }).getAttribute('href'),
      ).toBe(routes.makeAccount)
      expect(logOutAnywayButton(sheet, language)).toBeTruthy()
      expect(within(sheet).getByRole('button', { name: translated(language, 'settlements.cancel') })).toBeTruthy()
    })

    it('logs out after two taps, Log out and Log out anyway, and forgets the guest', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      const { queryClient } = await renderSettings(testGuestMe)
      const sheet = openLogOutSheet()

      fireEvent.click(logOutAnywayButton(sheet))

      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
      expect(sentRequest(fetchMock)).toMatchObject({ url: endpoints.logOut, method: 'POST' })
      await waitFor(() => {
        expect(queryClient.getQueryData(meQueryKey)).toBeNull()
      })
    })

    it('closes the sheet with Cancel and stays signed in, with no request', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      const { queryClient } = await renderSettings(testGuestMe)
      const sheet = openLogOutSheet()

      fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'settlements.cancel') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(fetchMock).not.toHaveBeenCalled()
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testGuestMe)
    })

    it('opens the make-account screen from the sheet without logging out', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      const { router } = await renderSettings(testGuestMe)
      const sheet = openLogOutSheet()

      fireEvent.click(within(sheet).getByRole('link', { name: translated('en', 'settings.guest.makeAccount') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.makeAccount)
      })
      expect(fetchMock).not.toHaveBeenCalled()
    })

    it('shows the error toast and keeps the guest signed in when the log-out fails', async () => {
      stubFetch(new Response(null, { status: 500 }))
      const { queryClient } = await renderSettings(testGuestMe)
      const sheet = openLogOutSheet()

      fireEvent.click(logOutAnywayButton(sheet))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testGuestMe)
    })

    it('disables Log out anyway while the log-out waits for an answer', async () => {
      stubFetch(new Response(null, { status: 204 }))
      await renderSettings(testGuestMe)
      vi.stubGlobal('fetch', vi.fn<typeof fetch>(() => new Promise<Response>(() => {})))
      const sheet = openLogOutSheet()

      fireEvent.click(logOutAnywayButton(sheet))

      await waitFor(() => {
        expect(logOutAnywayButton(sheet).matches(':disabled')).toBe(true)
      })
    })

    it('still logs a person with an account out with one tap and no sheet', async () => {
      const fetchMock = stubFetch(new Response(null, { status: 204 }))
      await renderSettings(testMe)

      fireEvent.click(logOutButton())

      expect(screen.queryByRole('dialog')).toBeNull()
      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
    })
  })
})
