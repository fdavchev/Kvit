import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testIdToken,
} from '@/test/googleTestHelpers'
import { defaultGroupEmoji, testGroupId, testInviteToken } from '@/test/groupTestData'
import {
  alreadyMemberInvitePreview,
  openInvitePreview,
  openInvitePreviewWithNames,
  removedInvitePreview,
  testInviteGroupName,
} from '@/test/inviteTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { JoinScreen } from './JoinScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const previewRequest = `POST ${endpoints.invitePreview}`
const joinRequest = `POST ${endpoints.inviteJoin}`
const joinedAnswer = jsonAnswer({ groupId: testGroupId })
const unclaimedNames = openInvitePreviewWithNames.unclaimedNames

type VisitorState = Me | null | 'unknown'

interface RenderOptions {
  language?: Language
  visitor?: VisitorState
  token?: string
  preview?: AnswerFactory
  answers?: Record<string, AnswerFactory>
}

async function renderJoin(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const token = options.token ?? testInviteToken
  const visitor = options.visitor === undefined ? testMe : options.visitor
  const identity = stubGoogleSignIn()
  const fetchMock = stubFetchByRequest({
    [previewRequest]: options.preview ?? jsonAnswer(openInvitePreview),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.join(':token'), element: <JoinScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.dashboard, element: <p>home page</p> },
      { path: routes.signUp, element: <p>sign up page</p> },
      { path: routes.logIn, element: <p>log in page</p> },
      { path: routes.googleSignUp, element: <p>google sign up page</p> },
    ],
    routes.join(token),
    {
      language,
      seedCache:
        visitor === 'unknown'
          ? undefined
          : seedMe(visitor === null ? null : { ...visitor, language }),
    },
  )
  return { fetchMock, identity, ...rendered }
}

function joinButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'join.join') })
}

function imNewButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'join.imNew') })
}

function nameButton(name: string): HTMLElement {
  return screen.getByRole('button', { name })
}

async function showsInviteCard(language: Language = 'en'): Promise<void> {
  await screen.findByText(
    translated(language, 'join.invitedTo', { name: testInviteGroupName }),
  )
}

function mainText(): string {
  return screen.getByRole('main').textContent ?? ''
}

function expectTokenNeverInAnApiAddress(fetchMock: { mock: { calls: unknown[][] } }, token: string): void {
  const urls = fetchMock.mock.calls.map(([url]) => String(url))
  expect(urls.length).toBeGreaterThan(0)
  for (const url of urls) {
    expect(url).not.toContain(token)
  }
}

function consoleErrorSilenced(): void {
  vi.spyOn(console, 'error').mockImplementation(() => {})
}

describe('JoinScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('asking about the invite', () => {
    it('sends the token in a JSON body to the preview endpoint, once, and never in an address', async () => {
      const { fetchMock } = await renderJoin()
      await showsInviteCard()

      expect(requestCount(fetchMock, 'POST', endpoints.invitePreview)).toBe(1)
      expect(requestsOf(fetchMock, 'POST', endpoints.invitePreview)[0]).toMatchObject({
        contentType: 'application/json',
        body: { token: testInviteToken },
      })
      expectTokenNeverInAnApiAddress(fetchMock, testInviteToken)
    })

    it('sends the token of the address that is open', async () => {
      const { fetchMock } = await renderJoin({ token: 'another-token_42' })
      await screen.findByText(translated('en', 'join.invitedTo', { name: testInviteGroupName }))

      expect(requestsOf(fetchMock, 'POST', endpoints.invitePreview)[0].body).toEqual({
        token: 'another-token_42',
      })
    })

    it('shows the loading spinner while the invite is being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: routes.join(':token'), element: <JoinScreen /> }],
        routes.join(testInviteToken),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })
  })

  describe('an old or reset link', () => {
    it.each(languages)('shows the INVITE_NOT_FOUND message and no way to join (%s)', async (language) => {
      await renderJoin({ language, preview: problemAnswer(404, 'INVITE_NOT_FOUND') })

      expect(
        await screen.findByText(translated(language, 'errors.INVITE_NOT_FOUND')),
      ).toBeTruthy()
      expect(
        screen.queryByRole('button', { name: translated(language, 'join.join') }),
      ).toBeNull()
    })

    it.each(languages)('has a Home button that goes to the home address (%s)', async (language) => {
      const { router } = await renderJoin({ language, preview: problemAnswer(404, 'INVITE_NOT_FOUND') })
      await screen.findByText(translated(language, 'errors.INVITE_NOT_FOUND'))

      fireEvent.click(screen.getByText(translated(language, 'nav.home')))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
    })

    it('shows the old-link message to a signed-out visitor too, with no sign-up links', async () => {
      await renderJoin({ visitor: null, preview: problemAnswer(404, 'INVITE_NOT_FOUND') })

      expect(
        await screen.findByText(translated('en', 'errors.INVITE_NOT_FOUND')),
      ).toBeTruthy()
      expect(
        screen.queryByRole('link', { name: translated('en', 'welcome.signUpWithEmail') }),
      ).toBeNull()
    })
  })

  describe('a person who was removed from the group', () => {
    it.each(languages)('shows the removed message with the group name and no way to join (%s)', async (language) => {
      await renderJoin({ language, preview: jsonAnswer(removedInvitePreview) })

      expect(
        await screen.findByText(
          translated(language, 'join.removed', { name: testInviteGroupName }),
        ),
      ).toBeTruthy()
      expect(
        screen.queryByRole('button', { name: translated(language, 'join.join') }),
      ).toBeNull()
    })

    it('has a Home button that goes to the home address', async () => {
      const { router } = await renderJoin({ preview: jsonAnswer(removedInvitePreview) })
      await screen.findByText(translated('en', 'join.removed', { name: testInviteGroupName }))

      fireEvent.click(screen.getByText(translated('en', 'nav.home')))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.dashboard)
      })
    })

    it('sends no join request', async () => {
      const { fetchMock } = await renderJoin({ preview: jsonAnswer(removedInvitePreview) })
      await screen.findByText(translated('en', 'join.removed', { name: testInviteGroupName }))

      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })
  })

  describe('when the invite cannot be asked', () => {
    it.each(languages)('shows the RATE_LIMITED message after too many tries (%s)', async (language) => {
      await renderJoin({ language, preview: problemAnswer(429, 'RATE_LIMITED') })

      expect(await screen.findByText(translated(language, 'errors.RATE_LIMITED'))).toBeTruthy()
    })

    it('shows the generic message after a server error', async () => {
      await renderJoin({ preview: () => new Response(null, { status: 500 }) })

      expect(await screen.findByText(translated('en', 'errors.generic'))).toBeTruthy()
    })

    it('shows the network message when the server cannot be reached', async () => {
      await renderJoin({ preview: networkFailureAnswer() })

      expect(await screen.findByText(translated('en', 'errors.network'))).toBeTruthy()
    })

    it('shows no invite card and no Join button after too many tries', async () => {
      await renderJoin({ preview: problemAnswer(429, 'RATE_LIMITED') })
      await screen.findByText(translated('en', 'errors.RATE_LIMITED'))

      expect(screen.queryByRole('button', { name: translated('en', 'join.join') })).toBeNull()
      expect(screen.queryByText(/invited/)).toBeNull()
    })
  })

  describe('a person who is already in the group', () => {
    it('goes straight to the group, replacing the join entry in the history', async () => {
      const { router } = await renderJoin({ preview: jsonAnswer(alreadyMemberInvitePreview) })

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(screen.getByText('group page')).toBeTruthy()
    })

    it('shows no invite card and sends no join request on the way', async () => {
      const { fetchMock, router } = await renderJoin({
        preview: jsonAnswer(alreadyMemberInvitePreview),
      })

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })

      expect(screen.queryByText(/invited/)).toBeNull()
      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })
  })

  describe('a signed-in person with an open invite and no names to take', () => {
    it.each(languages)('shows the invite card with the emoji, the group name, the people in the group and a Join button (%s)', async (language) => {
      await renderJoin({ language })

      await showsInviteCard(language)

      expect(screen.getByText(defaultGroupEmoji)).toBeTruthy()
      expect(joinButton(language)).toBeTruthy()
      expect(mainText()).toContain(translated(language, 'join.inGroup'))
      for (const name of openInvitePreview.memberNames) {
        expect(mainText()).toContain(name)
      }
    })

    it('shows no name question, no sign-up links and no Google button', async () => {
      const { identity } = await renderJoin()
      await showsInviteCard()

      expect(screen.queryByText(translated('en', 'join.areYou'))).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'join.imNew') })).toBeNull()
      expect(screen.queryByText(translated('en', 'join.signInHint'))).toBeNull()
      expect(
        screen.queryByRole('link', { name: translated('en', 'welcome.signUpWithEmail') }),
      ).toBeNull()
      expect(identity.renderButton).not.toHaveBeenCalled()
    })

    it('sends no join request before Join is pressed', async () => {
      const { fetchMock } = await renderJoin()
      await showsInviteCard()

      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })

    it('joins at once with only the token when Join is pressed', async () => {
      const { fetchMock } = await renderJoin({ answers: { [joinRequest]: joinedAnswer } })
      await showsInviteCard()

      fireEvent.click(joinButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', endpoints.inviteJoin)[0]
      expect(request.contentType).toBe('application/json')
      expect(request.body).toStrictEqual({ token: testInviteToken })
      expectTokenNeverInAnApiAddress(fetchMock, testInviteToken)
    })

    it('goes to the group, replacing the join entry in the history, after joining', async () => {
      const { router } = await renderJoin({ answers: { [joinRequest]: joinedAnswer } })
      await showsInviteCard()

      fireEvent.click(joinButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
    })

    it.each(languages)('shows the You joined toast with the group name after joining (%s)', async (language) => {
      await renderJoin({ language, answers: { [joinRequest]: joinedAnswer } })
      await showsInviteCard(language)

      fireEvent.click(joinButton(language))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(
          translated(language, 'join.joined', { name: testInviteGroupName }),
        )
      })
    })

    it('disables the Join button while the request waits for an answer', async () => {
      await renderJoin()
      await showsInviteCard()
      stubFetchThatNeverAnswers()

      fireEvent.click(joinButton())

      await expectDisabledWhilePending(joinButton())
    })

    it.each(
      languages.flatMap((language) =>
        (
          [
            ['INVITE_REMOVED', 403],
            ['RATE_LIMITED', 429],
            ['INVITE_NOT_FOUND', 404],
          ] as const
        ).map(([code, status]) => [language, code, status] as const),
      ),
    )('shows the %s text for the answer %s inline in an alert, stays on the join screen and shows no joined toast', async (language, code, status) => {
      const { router } = await renderJoin({
        language,
        answers: { [joinRequest]: problemAnswer(status, code) },
      })
      await showsInviteCard(language)

      fireEvent.click(joinButton(language))

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, `errors.${code}`),
      )
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      expect(shownToastTexts()).toEqual([])
    })

    it('shows the network message inline when the join cannot reach the server', async () => {
      await renderJoin({ answers: { [joinRequest]: networkFailureAnswer() } })
      await showsInviteCard()

      fireEvent.click(joinButton())

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.network'),
      )
    })

    it('still shows the invite card after the join was refused', async () => {
      await renderJoin({ answers: { [joinRequest]: problemAnswer(403, 'INVITE_REMOVED') } })
      await showsInviteCard()

      fireEvent.click(joinButton())
      await screen.findByRole('alert')

      expect(screen.getByText(translated('en', 'join.invitedTo', { name: testInviteGroupName }))).toBeTruthy()
    })
  })

  describe('a signed-in person with an open invite and names to take', () => {
    async function renderJoinWithNames(options: RenderOptions = {}) {
      return renderJoin({ ...options, preview: jsonAnswer(openInvitePreviewWithNames) })
    }

    async function pressJoin(language: Language = 'en'): Promise<void> {
      await showsInviteCard(language)
      fireEvent.click(joinButton(language))
      await screen.findByText(translated(language, 'join.areYou'))
    }

    it('shows the invite card with a Join button first and no name question yet', async () => {
      await renderJoinWithNames()

      await showsInviteCard()

      expect(joinButton()).toBeTruthy()
      expect(screen.queryByText(translated('en', 'join.areYou'))).toBeNull()
    })

    it('sends no join request when Join is pressed, because it asks the question first', async () => {
      const { fetchMock } = await renderJoinWithNames()

      await pressJoin()

      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })

    it.each(languages)("shows Are you one of these? with a button for each name and No, I'm new (%s)", async (language) => {
      await renderJoinWithNames({ language })

      await pressJoin(language)

      for (const { name } of unclaimedNames) {
        expect(nameButton(name)).toBeTruthy()
      }
      expect(imNewButton(language)).toBeTruthy()
    })

    it('sends the id of the name together with the token when a name is pressed', async () => {
      const { fetchMock } = await renderJoinWithNames({ answers: { [joinRequest]: joinedAnswer } })
      await pressJoin()

      fireEvent.click(nameButton('Grandma'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.inviteJoin)[0].body).toStrictEqual({
        token: testInviteToken,
        claimMemberId: unclaimedNames[1].id,
      })
      expectTokenNeverInAnApiAddress(fetchMock, testInviteToken)
    })

    it("sends only the token when No, I'm new is pressed", async () => {
      const { fetchMock } = await renderJoinWithNames({ answers: { [joinRequest]: joinedAnswer } })
      await pressJoin()

      fireEvent.click(imNewButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', endpoints.inviteJoin)[0].body).toStrictEqual({
        token: testInviteToken,
      })
    })

    it.each([
      ['a name', () => nameButton('Marko')],
      ["No, I'm new", () => imNewButton()],
    ])('goes to the group and shows the You joined toast after %s is pressed', async (_label, button) => {
      const { router } = await renderJoinWithNames({ answers: { [joinRequest]: joinedAnswer } })
      await pressJoin()

      fireEvent.click(button())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(shownToastTexts()).toContain(
        translated('en', 'join.joined', { name: testInviteGroupName }),
      )
    })

    it.each(languages)('shows the MEMBER_CANNOT_CLAIM text inline in an alert when the name cannot be taken (%s)', async (language) => {
      const { router } = await renderJoinWithNames({
        language,
        answers: { [joinRequest]: problemAnswer(400, 'MEMBER_CANNOT_CLAIM') },
      })
      await pressJoin(language)

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.MEMBER_CANNOT_CLAIM'),
      )
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      expect(shownToastTexts()).toEqual([])
    })

    it("shows the RATE_LIMITED text inline in an alert when No, I'm new is refused", async () => {
      await renderJoinWithNames({ answers: { [joinRequest]: problemAnswer(429, 'RATE_LIMITED') } })
      await pressJoin()

      fireEvent.click(imNewButton())

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.RATE_LIMITED'),
      )
    })
  })

  describe('a signed-out visitor with an open invite', () => {
    it.each(languages)('shows the invite card, the sign-in hint, the Google button and the two account links, and no Join button (%s)', async (language) => {
      const { identity } = await renderJoin({ language, visitor: null })

      await showsInviteCard(language)

      expect(screen.getByText(defaultGroupEmoji)).toBeTruthy()
      expect(mainText()).toContain(translated(language, 'join.inGroup'))
      expect(screen.getByText(translated(language, 'join.signInHint'))).toBeTruthy()
      expect(await findDrawnGoogleButton(identity, screen.getByRole('main'))).toBeTruthy()
      expect(
        screen.getByRole('link', { name: translated(language, 'welcome.signUpWithEmail') }),
      ).toBeTruthy()
      expect(
        screen.getByRole('link', { name: translated(language, 'welcome.haveAccount') }),
      ).toBeTruthy()
      expect(
        screen.queryByRole('button', { name: translated(language, 'join.join') }),
      ).toBeNull()
    })

    it('knows the visitor is signed out from the 401 of asking who is signed in', async () => {
      await renderJoin({
        visitor: 'unknown',
        answers: { 'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN') },
      })

      await showsInviteCard()

      expect(screen.getByText(translated('en', 'join.signInHint'))).toBeTruthy()
      expect(screen.queryByRole('button', { name: translated('en', 'join.join') })).toBeNull()
    })

    it('knows the visitor is signed in from asking who is signed in', async () => {
      await renderJoin({
        visitor: 'unknown',
        answers: { 'GET /api/me': jsonAnswer(testMe) },
      })

      await showsInviteCard()

      expect(joinButton()).toBeTruthy()
      expect(screen.queryByText(translated('en', 'join.signInHint'))).toBeNull()
    })

    it('shows an error instead of the sign-up links when asking who is signed in fails with a server error', async () => {
      await renderJoin({
        visitor: 'unknown',
        answers: { 'GET /api/me': () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.generic'),
      )
      expect(
        screen.queryByRole('link', { name: translated('en', 'welcome.signUpWithEmail') }),
      ).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'join.join') })).toBeNull()
    })

    it.each([
      ['welcome.signUpWithEmail', routes.signUp],
      ['welcome.haveAccount', routes.logIn],
    ])('opens the screen for "%s" and carries the invite token in the router state', async (key, path) => {
      const { router } = await renderJoin({ visitor: null })
      await showsInviteCard()

      fireEvent.click(screen.getByRole('link', { name: translated('en', key) }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(path)
      })
      expect(router.state.location.state).toEqual({ joinToken: testInviteToken })
    })

    it.each([
      ['welcome.signUpWithEmail', routes.signUp],
      ['welcome.haveAccount', routes.logIn],
    ])('points the link "%s" at %s', async (key, path) => {
      await renderJoin({ visitor: null })
      await showsInviteCard()

      const link = screen.getByRole('link', { name: translated('en', key) })

      expect(link.getAttribute('href')).toBe(path)
    })

    it('sends no join request', async () => {
      const { fetchMock } = await renderJoin({ visitor: null })
      await showsInviteCard()

      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
      expectTokenNeverInAnApiAddress(fetchMock, testInviteToken)
    })

    it('shows the names to take only after the visitor has an account, not before', async () => {
      await renderJoin({ visitor: null, preview: jsonAnswer(openInvitePreviewWithNames) })
      await showsInviteCard()

      expect(screen.queryByText(translated('en', 'join.areYou'))).toBeNull()
    })

    describe('and the Google button', () => {
      it('goes back to the join screen, replacing the entry in the history, and shows Join after Google signs the visitor in', async () => {
        const { identity, router } = await renderJoin({
          visitor: null,
          answers: { [`POST ${endpoints.googleLogIn}`]: jsonAnswer(testMe) },
        })
        await showsInviteCard()
        const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

        sendGoogleCredential(identity, button, testIdToken)

        expect(await screen.findByRole('button', { name: translated('en', 'join.join') })).toBeTruthy()
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
        expect(router.state.historyAction).toBe('REPLACE')
      })

      it('goes to the Google name screen carrying the ID token and the invite token when the person has no account yet', async () => {
        const { identity, router } = await renderJoin({
          visitor: null,
          answers: {
            [`POST ${endpoints.googleLogIn}`]: problemAnswer(404, 'AUTH_GOOGLE_NO_ACCOUNT'),
          },
        })
        await showsInviteCard()
        const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

        sendGoogleCredential(identity, button, testIdToken)

        await waitFor(() => {
          expect(router.state.location.pathname).toBe(routes.googleSignUp)
        })
        expect(router.state.location.state).toEqual({
          idToken: testIdToken,
          joinToken: testInviteToken,
        })
      })

      it('shows the error toast and stays on the join screen when Google sign-in fails', async () => {
        consoleErrorSilenced()
        const { identity, router } = await renderJoin({
          visitor: null,
          answers: {
            [`POST ${endpoints.googleLogIn}`]: problemAnswer(401, 'AUTH_GOOGLE_TOKEN_INVALID'),
          },
        })
        await showsInviteCard()
        const button = await findDrawnGoogleButton(identity, screen.getByRole('main'))

        sendGoogleCredential(identity, button, testIdToken)

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(
            translated('en', 'errors.AUTH_GOOGLE_TOKEN_INVALID'),
          )
        })
        expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      })
    })
  })
})
