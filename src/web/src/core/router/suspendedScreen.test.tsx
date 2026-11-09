import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import type { Me } from '@/core/services/me/meService'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  elementShowingText,
  mailtoLinks,
  supportEmailText,
  supportMailtoHref,
} from '@/test/legalScreenTestHelpers'
import { parseMailto } from '@/test/mailtoTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  neverAnswer,
  networkFailureAnswer,
  noContentAnswer,
  requestCount,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import {
  testGuestMe,
  testMe,
  testMeWithMaterialTerms,
  testMeWithOldTerms,
  testSuspendedMe,
} from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const logOutRequest = 'POST /api/auth/logout'
const groupPath = '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'

interface RenderOptions {
  me?: Me
  language?: Language
  path?: string
  answers?: Record<string, AnswerFactory>
}

async function renderSuspended(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const me: Me = { ...(options.me ?? testSuspendedMe), language }
  const fetchMock = stubFetchByRequest({
    'GET /api/me': jsonAnswer(me),
    'POST /api/me/active': noContentAnswer(),
    [logOutRequest]: noContentAnswer(),
    'GET /api/health': () => new Response('Healthy'),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(routeObjects, options.path ?? routes.settings, { language })
  return { fetchMock, ...rendered }
}

function suspendedHeading(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('heading', { level: 1, name: translated(language, 'suspended.title') })
}

function logOutButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'settings.logOut') })
}

describe('the screen of a suspended person', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('what it shows', () => {
    it.each(languages)('shows the title as the one level 1 heading and the body below it (%s)', async (language) => {
      await renderSuspended({ language })

      const title = await suspendedHeading(language)

      expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
      const body = elementShowingText(translated(language, 'suspended.body'))
      expect(Boolean(title.compareDocumentPosition(body) & Node.DOCUMENT_POSITION_FOLLOWING)).toBe(true)
    })

    it('makes the email address in the body a mailto link, and the only link on the screen', async () => {
      await renderSuspended()
      await suspendedHeading()

      const links = screen.queryAllByRole('link')
      const [link] = mailtoLinks()

      expect(links).toEqual([link])
      expect(link.getAttribute('href')).toBe(supportMailtoHref)
      expect(link.textContent).toBe(supportEmailText)
      expect(parseMailto(link.getAttribute('href') as string)).toEqual({
        address: supportEmailText,
        parameterNames: [],
        parameters: {},
      })
    })

    it('keeps the link inside the body text', async () => {
      await renderSuspended()
      await suspendedHeading()

      const body = elementShowingText(translated('en', 'suspended.body'))

      expect(body.contains(mailtoLinks()[0])).toBe(true)
      expect(body.textContent).toBe('Contact kvit.support@gmail.com.')
    })

    it('has one button, Log out, and nothing else to press', async () => {
      await renderSuspended()
      await suspendedHeading()

      expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual([
        translated('en', 'settings.logOut'),
      ])
    })

    it.each(languages)('writes the Log out button in the language of the person (%s)', async (language) => {
      await renderSuspended({ language })
      await suspendedHeading(language)

      expect(logOutButton(language)).toBeTruthy()
    })

    it('shows no tab bar, no outbox bar, no notice and no dialog', async () => {
      await renderSuspended()
      await suspendedHeading()

      expect(screen.queryByRole('navigation')).toBeNull()
      expect(document.querySelector('[data-outbox-bar]')).toBeNull()
      expect(screen.queryByRole('status')).toBeNull()
      expect(screen.queryByRole('dialog')).toBeNull()
    })
  })

  describe('instead of every signed-in screen', () => {
    it.each([
      routes.dashboard,
      routes.groups,
      routes.settings,
      routes.admin,
      '/admin/tools',
      routes.budget,
      routes.newGroup,
      groupPath,
      routes.changePassword,
    ])('shows it at %s and keeps the address', async (path) => {
      const { router } = await renderSuspended({ path })

      expect(await suspendedHeading()).toBeTruthy()
      expect(router.state.location.pathname).toBe(path)
      expect(router.state.historyAction).toBe('POP')
    })

    it('shows it to a suspended person who must also change the password, without sending them to the change-password page', async () => {
      const { router } = await renderSuspended({ me: { ...testSuspendedMe, mustChangePassword: true }, path: routes.dashboard })

      expect(await suspendedHeading()).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })

    it('shows it to a suspended guest', async () => {
      await renderSuspended({ me: { ...testGuestMe, isSuspended: true } })

      expect(await suspendedHeading()).toBeTruthy()
    })

    it('shows it to a suspended person who is an admin, with no Statistics or Owner tools', async () => {
      await renderSuspended({ me: { ...testSuspendedMe, isAdmin: true }, path: routes.admin })

      expect(await suspendedHeading()).toBeTruthy()
      expect(screen.queryByRole('heading', { name: translated('en', 'adminStats.title') })).toBeNull()
    })

    it('shows the screen of the page itself to a person who is not suspended', async () => {
      await renderSuspended({ me: testMe })

      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.title') })).toBeTruthy()
      expect(screen.queryByRole('heading', { name: translated('en', 'suspended.title') })).toBeNull()
    })

    it('shows the page itself when isSuspended is explicitly false', async () => {
      await renderSuspended({ me: { ...testMe, isSuspended: false } })

      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.title') })).toBeTruthy()
      expect(screen.queryByRole('heading', { name: translated('en', 'suspended.title') })).toBeNull()
    })
  })

  describe('what it asks the server', () => {
    it('asks only who is signed in, and does not record that the person was active or load anything else', async () => {
      const { fetchMock } = await renderSuspended({ path: routes.dashboard })
      await suspendedHeading()

      const urls = [...new Set(fetchMock.mock.calls.map(([input]) => String(input)))]

      expect(urls).toEqual(['/api/me'])
    })
  })

  describe('the terms notice', () => {
    it('shows no thin notice to a suspended person whose terms are out of date', async () => {
      await renderSuspended({ me: { ...testMeWithOldTerms, isSuspended: true } })
      await suspendedHeading()

      expect(screen.queryByText(translated('en', 'legal.termsUpdated'))).toBeNull()
    })

    it('shows no blocking sheet to a suspended person when the terms changed in a way that matters', async () => {
      await renderSuspended({ me: { ...testMeWithMaterialTerms, isSuspended: true } })
      await suspendedHeading()

      expect(screen.queryByRole('dialog')).toBeNull()
      expect(screen.queryByText(translated('en', 'legal.termsMaterial.title'))).toBeNull()
    })
  })

  describe('Log out', () => {
    it('sends one POST to the log-out address when it is pressed', async () => {
      const { fetchMock } = await renderSuspended()
      await suspendedHeading()

      fireEvent.click(logOutButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', '/api/auth/logout')).toBe(1)
      })
    })

    it('takes the person to the welcome page when the server answered', async () => {
      const { router } = await renderSuspended()
      await suspendedHeading()

      fireEvent.click(logOutButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.welcome)
      })
      expect(screen.queryByRole('heading', { name: translated('en', 'suspended.title') })).toBeNull()
    })

    it('disables the button while the server has not answered and sends one request when it is pressed again', async () => {
      const { fetchMock } = await renderSuspended({ answers: { [logOutRequest]: neverAnswer() } })
      await suspendedHeading()
      const button = logOutButton()

      fireEvent.click(button)
      await waitFor(() => {
        expect(button.matches(':disabled')).toBe(true)
      })
      fireEvent.click(button)

      expect(requestCount(fetchMock, 'POST', '/api/auth/logout')).toBe(1)
    })

    it('shows the error toast and stays on the suspended screen when the server cannot be reached', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderSuspended({ answers: { [logOutRequest]: networkFailureAnswer() } })
      await suspendedHeading()

      fireEvent.click(logOutButton())

      await waitFor(() => {
        expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
      expect(screen.getByRole('heading', { level: 1, name: translated('en', 'suspended.title') })).toBeTruthy()
      expect(logOutButton().matches(':disabled')).toBe(false)
    })
  })

  describe('when the person is suspended or unsuspended while the app is open', () => {
    it('replaces the screen they were using with the suspended screen when the cached person becomes suspended', async () => {
      const { queryClient } = await renderSuspended({ me: testMe })
      await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.title') })

      act(() => {
        queryClient.setQueryData<Me>(meQueryKey, testSuspendedMe)
      })

      expect(await suspendedHeading()).toBeTruthy()
      expect(screen.queryByRole('heading', { level: 1, name: translated('en', 'settings.title') })).toBeNull()
    })

    it('brings the app back when the cached person is no longer suspended', async () => {
      const { queryClient } = await renderSuspended()
      await suspendedHeading()

      act(() => {
        queryClient.setQueryData<Me>(meQueryKey, testMe)
      })

      expect(await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.title') })).toBeTruthy()
      expect(screen.queryByRole('heading', { name: translated('en', 'suspended.title') })).toBeNull()
    })
  })
})
