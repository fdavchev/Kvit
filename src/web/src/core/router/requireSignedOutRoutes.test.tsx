import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { joinPath } from '@/core/invites/joinRoundTrip'
import { problemResponse, stubFetch } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testGroup, testGroupId, testInviteToken } from '@/test/groupTestData'
import { openInvitePreview, testInviteGroupName } from '@/test/inviteTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, problemAnswer, requestCount, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: vi.fn(),
}))

const authScreens = [
  [routes.welcome, 'welcome'],
  [routes.signUp, 'sign-up'],
  [routes.logIn, 'log-in'],
] as const

const meRequest = '/api/me'

function homeGreeting(): string {
  return translated('en', 'home.greeting', { name: testMe.displayName })
}

function pressLink(key: string): void {
  fireEvent.click(screen.getByRole('link', { name: translated('en', key) }))
}

async function fillAndSubmitSignUp(): Promise<void> {
  await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })
  typeInto(translated('en', 'auth.signUp.name'), testMe.displayName)
  typeInto(translated('en', 'auth.email'), testMe.email)
  typeInto(translated('en', 'auth.password'), 'Passw0rdOk')
  fireEvent.click(screen.getByRole('button', { name: translated('en', 'auth.signUp.title') }))
}

async function fillAndSubmitLogIn(): Promise<void> {
  await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })
  typeInto(translated('en', 'auth.email'), testMe.email)
  typeInto(translated('en', 'auth.password'), 'Passw0rdOk')
  fireEvent.click(screen.getByRole('button', { name: translated('en', 'auth.logIn.title') }))
}

async function browserBack(router: { navigate: (delta: number) => Promise<void> }): Promise<void> {
  await act(() => router.navigate(-1))
}

describe('the signed-out guard of the welcome, sign-up and log-in screens', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('for a signed-in person', () => {
    it.each(authScreens)('sends them from the %s address to the home screen, replacing the entry', async (path) => {
      stubFetch(Response.json(testMe))

      const { router } = await renderRoutesWithProviders(routeObjects, path, { seedCache: seedMe(testMe) })

      expect(await screen.findByText(homeGreeting())).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.dashboard)
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it.each(authScreens)('does not ask who is signed in when they open the %s address', async (path) => {
      const fetchMock = stubFetch(Response.json(testMe))

      await renderRoutesWithProviders(routeObjects, path, { seedCache: seedMe(testMe) })
      await screen.findByText(homeGreeting())

      expect(requestCount(fetchMock, 'GET', meRequest)).toBe(0)
    })

    it.each(authScreens)('sends them from the %s address to the invite they came from when the router state has a join token', async (path) => {
      stubFetchByRequest({
        [`POST ${endpoints.invitePreview}`]: jsonAnswer({ ...openInvitePreview, status: 'AlreadyMember', groupId: testGroupId }),
        [`GET /api/groups/${testGroupId}`]: jsonAnswer(testGroup),
      })

      const { router } = await renderRoutesWithProviders(routeObjects, path, {
        seedCache: seedMe(testMe),
        routerState: { joinToken: testInviteToken },
      })

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(joinPath(testInviteToken))
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it.each(authScreens)('shows nothing of the %s screen while sending them away', async (path) => {
      stubFetch(Response.json(testMe))

      await renderRoutesWithProviders(routeObjects, path, { seedCache: seedMe(testMe) })

      expect(screen.queryByRole('heading', { name: translated('en', 'auth.signUp.title') })).toBeNull()
      expect(screen.queryByRole('heading', { name: translated('en', 'auth.logIn.title') })).toBeNull()
      expect(screen.queryByRole('link', { name: translated('en', 'welcome.haveAccount') })).toBeNull()
    })
  })

  describe('for a visitor who is signed out or not known yet', () => {
    it.each([
      [routes.welcome, 'link', 'welcome.haveAccount'],
      [routes.signUp, 'heading', 'auth.signUp.title'],
      [routes.logIn, 'heading', 'auth.logIn.title'],
    ])('shows the %s screen to a visitor known to be signed out, without any request', async (path, role, key) => {
      const fetchMock = stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))

      const { router } = await renderRoutesWithProviders(routeObjects, path, { seedCache: seedMe(null) })

      expect(await screen.findByRole(role, { name: translated('en', key) })).toBeTruthy()
      expect(router.state.location.pathname).toBe(path)
      expect(requestCount(fetchMock, 'GET', meRequest)).toBe(0)
      expect(fetchMock).not.toHaveBeenCalled()
    })

    it.each([
      [routes.welcome, 'link', 'welcome.haveAccount'],
      [routes.signUp, 'heading', 'auth.signUp.title'],
      [routes.logIn, 'heading', 'auth.logIn.title'],
    ])('shows the %s screen to a visitor nothing is known about, without any request', async (path, role, key) => {
      const fetchMock = stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))

      const { router } = await renderRoutesWithProviders(routeObjects, path)

      expect(await screen.findByRole(role, { name: translated('en', key) })).toBeTruthy()
      expect(router.state.location.pathname).toBe(path)
      expect(requestCount(fetchMock, 'GET', meRequest)).toBe(0)
      expect(fetchMock).not.toHaveBeenCalled()
    })
  })

  describe('when the person signs in on one of the screens', () => {
    it('keeps the log-in screen in place while the answer arrives and then shows the home screen', async () => {
      stubFetch(Response.json(testMe))
      const { router, queryClient } = await renderRoutesWithProviders(routeObjects, routes.logIn)

      await fillAndSubmitLogIn()

      expect(await screen.findByText(homeGreeting())).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.dashboard)
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testMe)
    })

    it('shows the Deletion cancelled toast once and lands on the home screen after a log-in whose answer says the deletion was cancelled', async () => {
      stubFetch(Response.json({ ...testMe, deletionCancelled: true }))
      const { router } = await renderRoutesWithProviders(routeObjects, routes.logIn)

      await fillAndSubmitLogIn()

      expect(await screen.findByText(homeGreeting())).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.dashboard)
      const cancelledText = translated('en', 'auth.deletionCancelled')
      expect(shownToastTexts().filter((shown) => shown === cancelledText)).toHaveLength(1)
    })

    it('lands on the home screen after signing up and sends the browser Back to the welcome address on to the home screen', async () => {
      stubFetch(Response.json(testMe))
      const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
      pressLink('welcome.signUpWithEmail')
      await fillAndSubmitSignUp()
      expect(await screen.findByText(homeGreeting())).toBeTruthy()

      await browserBack(router)

      expect(await screen.findByText(homeGreeting())).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.dashboard)
      expect(screen.queryByRole('heading', { name: translated('en', 'auth.signUp.title') })).toBeNull()
    })

    it('does not show the sign-up screen to the signed-in person when the browser Back is pressed twice after signing up', async () => {
      stubFetch(Response.json(testMe))
      const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
      pressLink('welcome.signUpWithEmail')
      await fillAndSubmitSignUp()
      await screen.findByText(homeGreeting())

      await browserBack(router)
      await browserBack(router)

      expect(screen.queryByRole('heading', { name: translated('en', 'auth.signUp.title') })).toBeNull()
      expect(router.state.location.pathname).not.toBe(routes.signUp)
    })
  })
})

describe('the history of the sign-up and log-in screens', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
    stubFetch(problemResponse(401, 'AUTH_NOT_SIGNED_IN'))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('replaces the sign-up entry when the in-app Back button is pressed, so the browser Back does not return to sign-up', async () => {
    const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
    pressLink('welcome.signUpWithEmail')
    await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })

    pressLink('common.back')

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(router.state.historyAction).toBe('REPLACE')
    await browserBack(router)
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it('replaces the log-in entry when the in-app Back button is pressed, so the browser Back does not return to log-in', async () => {
    const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
    pressLink('welcome.haveAccount')
    await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })

    pressLink('common.back')

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.welcome)
    })
    expect(router.state.historyAction).toBe('REPLACE')
    await browserBack(router)
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it('replaces the sign-up entry with the log-in screen when the Log in link is pressed, so the browser Back goes to the welcome screen', async () => {
    const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
    pressLink('welcome.signUpWithEmail')
    await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })

    pressLink('welcome.haveAccount')

    expect(await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })).toBeTruthy()
    expect(router.state.historyAction).toBe('REPLACE')
    await browserBack(router)
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it('replaces the log-in entry with the sign-up screen when the Create account link is pressed, so the browser Back goes to the welcome screen', async () => {
    const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)
    pressLink('welcome.haveAccount')
    await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })

    pressLink('auth.logIn.noAccount')

    expect(await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })).toBeTruthy()
    expect(router.state.historyAction).toBe('REPLACE')
    await browserBack(router)
    expect(router.state.location.pathname).toBe(routes.welcome)
  })

  it('goes from the welcome screen to the sign-up screen by adding an entry, not by replacing one', async () => {
    const { router } = await renderRoutesWithProviders(routeObjects, routes.welcome)

    pressLink('welcome.signUpWithEmail')

    await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })
    expect(router.state.historyAction).toBe('PUSH')
  })

  it('goes back to the invite card with the in-app Back button from the sign-up screen reached through a hop from log-in, replacing the entry', async () => {
    stubFetchByRequest({
      [`GET ${meRequest}`]: problemAnswer(401, 'AUTH_NOT_SIGNED_IN'),
      [`POST ${endpoints.invitePreview}`]: jsonAnswer(openInvitePreview),
    })
    const { router } = await renderRoutesWithProviders(routeObjects, routes.logIn, {
      routerState: { joinToken: testInviteToken },
    })
    await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })
    pressLink('auth.logIn.noAccount')
    await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })

    pressLink('common.back')

    expect(await screen.findByText(translated('en', 'join.invitedTo', { name: testInviteGroupName }))).toBeTruthy()
    expect(router.state.location.pathname).toBe(joinPath(testInviteToken))
    expect(router.state.historyAction).toBe('REPLACE')
  })
})
