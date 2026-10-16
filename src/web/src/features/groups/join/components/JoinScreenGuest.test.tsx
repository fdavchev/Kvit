import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { meQueryKey } from '@/core/auth/useMe'
import { languages, type Language } from '@/core/i18n/language'
import { invitePreviewQueryKey } from '@/core/invites/joinRoundTrip'
import { routes } from '@/core/router/routes'
import type { InvitePreview } from '@/core/services/invites/invitesService'
import type { Me } from '@/core/services/me/meService'
import { problemResponse, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testIdToken,
} from '@/test/googleTestHelpers'
import { testGroupId, testInviteToken } from '@/test/groupTestData'
import {
  openInvitePreview,
  openInvitePreviewWithNames,
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
import { expectTapTarget } from '@/test/tapTargetTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { JoinScreen } from './JoinScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const previewRequest = `POST ${endpoints.invitePreview}`
const guestJoinRequest = `POST ${endpoints.inviteJoinAsGuest}`
const meRequest = 'GET /api/me'
const freeNames = openInvitePreviewWithNames.unclaimedNames
const deviceTimeZone = 'Europe/Skopje'

interface RenderOptions {
  language?: Language
  preview?: AnswerFactory
  answers?: Record<string, AnswerFactory>
}

async function renderGuestJoin(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const identity = stubGoogleSignIn()
  const fetchMock = stubFetchByRequest({
    [previewRequest]: options.preview ?? jsonAnswer(openInvitePreviewWithNames),
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
    routes.join(testInviteToken),
    { language, seedCache: seedMe(null) },
  )
  await screen.findByText(translated(language, 'join.invitedTo', { name: testInviteGroupName }))
  return { fetchMock, identity, ...rendered }
}

function joinedAs(me: Me = testGuestMe): Record<string, AnswerFactory> {
  return {
    [guestJoinRequest]: jsonAnswer({ groupId: testGroupId }),
    [meRequest]: jsonAnswer(me),
  }
}

function refusedWith(status: number, code: string): Record<string, AnswerFactory> {
  return { [guestJoinRequest]: problemAnswer(status, code) }
}

function stubDeviceTimeZone(timeZone: string): void {
  const options = new Intl.DateTimeFormat().resolvedOptions()
  vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue(
    Object.assign({}, options, { timeZone }),
  )
}

function shownTextOf(element: HTMLElement): string {
  const copy = element.cloneNode(true) as HTMLElement
  copy.querySelectorAll('[aria-hidden="true"]').forEach((hidden) => hidden.remove())
  return copy.textContent ?? ''
}

function listedButtonTexts(): string[] {
  return within(screen.getByRole('list')).getAllByRole('button').map(shownTextOf)
}

function nameButton(name: string): HTMLElement {
  return screen.getByRole('button', { name })
}

function imNewButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'join.guest.imNew') })
}

function joinButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'join.join') })
}

function logInButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'auth.logIn.title') })
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return screen.getByLabelText<HTMLInputElement>(translated(language, 'auth.signUp.name'))
}

function openNewName(language: Language = 'en'): void {
  fireEvent.click(imNewButton(language))
}

function joinWithNewName(name: string, language: Language = 'en'): void {
  openNewName(language)
  typeInto(translated(language, 'auth.signUp.name'), name)
  fireEvent.click(joinButton(language))
}

function guestJoinBodies(fetchMock: Parameters<typeof requestsOf>[0]): unknown[] {
  return requestsOf(fetchMock, 'POST', endpoints.inviteJoinAsGuest).map((request) => request.body)
}

function openLogInSheet(language: Language = 'en'): HTMLElement {
  fireEvent.click(logInButton(language))
  return screen.getByRole('dialog', { name: translated(language, 'auth.logIn.title') })
}

function consoleErrorSilenced(): void {
  vi.spyOn(console, 'error').mockImplementation(() => {})
}

describe('JoinScreen for a visitor without an account', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceTimeZone(deviceTimeZone)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('while the invite is being asked', () => {
    it('shows the loading spinner', async () => {
      stubGoogleSignIn()
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: routes.join(':token'), element: <JoinScreen /> }],
        routes.join(testInviteToken),
        { seedCache: seedMe(null) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
      expect(screen.queryByRole('button')).toBeNull()
    })
  })

  describe('with free names in the group', () => {
    it.each(languages)('asks "Who are you? Tap your name:" as a heading (%s)', async (language) => {
      await renderGuestJoin({ language })

      expect(
        screen.getByRole('heading', { name: translated(language, 'join.guest.whoAreYou') }),
      ).toBeTruthy()
    })

    it.each(languages)("shows one button per free name and the I'm new button last (%s)", async (language) => {
      await renderGuestJoin({ language })

      expect(listedButtonTexts()).toEqual([
        ...freeNames.map(({ name }) => name),
        translated(language, 'join.guest.imNew'),
      ])
    })

    it('shows no sign-up link, no old sign-in hint, no Google button and no Join button before anything is tapped', async () => {
      const { identity } = await renderGuestJoin()

      expect(
        screen.queryByRole('link', { name: translated('en', 'welcome.signUpWithEmail') }),
      ).toBeNull()
      expect(screen.queryByText('Create an account or log in to join.')).toBeNull()
      expect(identity.renderButton).not.toHaveBeenCalled()
      expect(screen.queryByRole('button', { name: translated('en', 'join.join') })).toBeNull()
      expect(screen.queryByLabelText(translated('en', 'auth.signUp.name'))).toBeNull()
    })

    it('sends no join request before a name is tapped', async () => {
      const { fetchMock } = await renderGuestJoin()

      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoinAsGuest)).toBe(0)
      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })

    it('joins as a guest with one tap on a free name, sending the token, the id of the name, the language and the time zone', async () => {
      const { fetchMock } = await renderGuestJoin({ answers: joinedAs() })

      fireEvent.click(nameButton('Grandma'))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', endpoints.inviteJoinAsGuest)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', endpoints.inviteJoinAsGuest)[0]
      expect(request.contentType).toBe('application/json')
      expect(request.body).toStrictEqual({
        token: testInviteToken,
        claimMemberId: freeNames[1].id,
        language: 'en',
        timeZone: deviceTimeZone,
      })
      expect(request.url).not.toContain(testInviteToken)
    })

    it('sends the language of the screen, Macedonian when the screen is in Macedonian', async () => {
      const { fetchMock } = await renderGuestJoin({ language: 'mk', answers: joinedAs({ ...testGuestMe, language: 'mk' }) })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(guestJoinBodies(fetchMock)).toHaveLength(1)
      })
      expect(guestJoinBodies(fetchMock)[0]).toMatchObject({ language: 'mk' })
    })

    it('never uses the normal join, which needs an account', async () => {
      const { fetchMock, router } = await renderGuestJoin({ answers: joinedAs() })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    })

    it.each(languages)('goes to the group, replacing the join entry in the history, and shows the joined toast (%s)', async (language) => {
      const { router } = await renderGuestJoin({ language, answers: joinedAs({ ...testGuestMe, language }) })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(router.state.historyAction).toBe('REPLACE')
      expect(shownToastTexts()).toContain(translated(language, 'join.joined', { name: testInviteGroupName }))
    })

    it('asks who is signed in once, after the join, and keeps the guest as the signed-in person', async () => {
      const { fetchMock, queryClient, router } = await renderGuestJoin({ answers: joinedAs() })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      const sentRequests = fetchMock.mock.calls.map(([url, init]) => `${init?.method ?? 'GET'} ${String(url)}`)
      expect(sentRequests.filter((request) => request === meRequest)).toHaveLength(1)
      expect(sentRequests.indexOf(meRequest)).toBeGreaterThan(sentRequests.indexOf(guestJoinRequest))
      expect(queryClient.getQueryData(meQueryKey)).toEqual(testGuestMe)
    })

    it('switches the cached person to the guest, with no email and the guest mark', async () => {
      const { queryClient, router } = await renderGuestJoin({ answers: joinedAs() })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(queryClient.getQueryData<Me | null>(meQueryKey)).toMatchObject({
        id: testGuestMe.id,
        email: null,
        isGuest: true,
      })
    })

    it('marks the invite preview as old after joining, so it is asked again next time', async () => {
      const { queryClient, router } = await renderGuestJoin({ answers: joinedAs() })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(queryClient.getQueryState(invitePreviewQueryKey(testInviteToken))?.isInvalidated).toBe(true)
    })

    it("disables every name, the I'm new button and the Log in button while the join waits for an answer", async () => {
      await renderGuestJoin()
      stubFetchThatNeverAnswers()

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        for (const { name } of freeNames) {
          expect(nameButton(name).matches(':disabled')).toBe(true)
        }
        expect(imNewButton().matches(':disabled')).toBe(true)
        expect(logInButton().matches(':disabled')).toBe(true)
      })
    })

    it('shows the spinner only on the tapped name while the join waits, so a slow server is visible', async () => {
      await renderGuestJoin()
      stubFetchThatNeverAnswers()

      fireEvent.click(nameButton('Grandma'))

      await waitFor(() => {
        expect(nameButton('Grandma').querySelector('.animate-spin')).not.toBeNull()
      })
      expect(nameButton('Marko').querySelector('.animate-spin')).toBeNull()
    })

    it('sends only one join request when the name is tapped again while the first tap waits', async () => {
      await renderGuestJoin()
      const fetchThatNeverAnswers = vi.fn<typeof fetch>(() => new Promise<Response>(() => {}))
      vi.stubGlobal('fetch', fetchThatNeverAnswers)
      fireEvent.click(nameButton('Marko'))
      await waitFor(() => {
        expect(nameButton('Marko').matches(':disabled')).toBe(true)
      })

      fireEvent.click(nameButton('Marko'))
      fireEvent.click(nameButton('Grandma'))

      expect(fetchThatNeverAnswers).toHaveBeenCalledTimes(1)
    })

    it('gives every name row a tap area of at least 56 px', async () => {
      await renderGuestJoin()

      for (const button of within(screen.getByRole('list')).getAllByRole('button')) {
        expect(button.className).toMatch(/(^|\s)min-h-14(\s|$)/)
      }
    })

    it('lets a long name wrap anywhere inside its row instead of pushing the screen wider', async () => {
      const longName = 'Aleksandra-Konstantinovska-Petrovska-Dimitrievska'
      await renderGuestJoin({
        preview: jsonAnswer({ ...openInvitePreviewWithNames, unclaimedNames: [{ id: freeNames[0].id, name: longName }] }),
      })

      const nameText = screen.getByText(longName)
      expect(nameText.className).toMatch(/(^|\s)wrap-anywhere(\s|$)/)
      expect(nameText.className).toMatch(/(^|\s)min-w-0(\s|$)/)
    })
  })

  describe("the I'm new row", () => {
    it("opens one name field and a Join button, and marks the row as opened", async () => {
      await renderGuestJoin()

      openNewName()

      expect(nameField()).toBeTruthy()
      expect(joinButton()).toBeTruthy()
      expect(imNewButton().getAttribute('aria-expanded')).toBe('true')
    })

    it('puts the focus in the name field so the person can type at once', async () => {
      await renderGuestJoin()

      openNewName()

      await waitFor(() => {
        expect(document.activeElement).toBe(nameField())
      })
    })

    it('is closed at first', async () => {
      await renderGuestJoin()

      expect(imNewButton().getAttribute('aria-expanded')).toBe('false')
    })

    it('joins with the typed name and no free name after one field and two taps', async () => {
      const { fetchMock } = await renderGuestJoin({ answers: joinedAs({ ...testGuestMe, displayName: 'Ана Петровска' }) })

      joinWithNewName('Ана Петровска')

      await waitFor(() => {
        expect(guestJoinBodies(fetchMock)).toHaveLength(1)
      })
      expect(guestJoinBodies(fetchMock)[0]).toStrictEqual({
        token: testInviteToken,
        name: 'Ана Петровска',
        language: 'en',
        timeZone: deviceTimeZone,
      })
    })

    it('goes to the group and shows the joined toast after joining with a new name', async () => {
      const { router } = await renderGuestJoin({ answers: joinedAs() })

      joinWithNewName('Ana')

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
      expect(shownToastTexts()).toContain(translated('en', 'join.joined', { name: testInviteGroupName }))
    })

    it('joins when the name is sent with the keyboard instead of the Join button', async () => {
      const { fetchMock } = await renderGuestJoin({ answers: joinedAs() })
      openNewName()
      typeInto(translated('en', 'auth.signUp.name'), 'Ana')

      fireEvent.submit(nameField())

      await waitFor(() => {
        expect(guestJoinBodies(fetchMock)).toHaveLength(1)
      })
    })

    it('disables the Join button and every name while the new name waits for an answer', async () => {
      await renderGuestJoin()
      openNewName()
      typeInto(translated('en', 'auth.signUp.name'), 'Ana')
      stubFetchThatNeverAnswers()

      fireEvent.click(joinButton())

      await waitFor(() => {
        expect(joinButton().matches(':disabled')).toBe(true)
        expect(nameButton('Marko').matches(':disabled')).toBe(true)
      })
    })

    it.each([
      ['an empty name', ''],
      ['a name longer than 60 characters', 'A'.repeat(61)],
    ])('sends %s as typed and shows the server answer AUTH_DISPLAY_NAME_INVALID next to the field, keeping what was typed', async (_label, typedName) => {
      const { fetchMock, router } = await renderGuestJoin({ answers: refusedWith(400, 'AUTH_DISPLAY_NAME_INVALID') })

      joinWithNewName(typedName)

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.AUTH_DISPLAY_NAME_INVALID'),
      )
      expect(guestJoinBodies(fetchMock)[0]).toMatchObject({ name: typedName })
      expect(nameField().value).toBe(typedName)
      expect(nameField().getAttribute('aria-invalid')).toBe('true')
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    })

    it('clears the red edge of the field once the person types again', async () => {
      await renderGuestJoin({ answers: refusedWith(400, 'AUTH_DISPLAY_NAME_INVALID') })
      joinWithNewName('')
      await screen.findByRole('alert')

      typeInto(translated('en', 'auth.signUp.name'), 'Ana')

      expect(nameField().getAttribute('aria-invalid')).not.toBe('true')
    })

    it.each(languages)('shows MEMBER_NAME_TAKEN with the typed name when someone in the group already has it (%s)', async (language) => {
      await renderGuestJoin({ language, answers: refusedWith(400, 'MEMBER_NAME_TAKEN') })

      joinWithNewName('Filip', language)

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.MEMBER_NAME_TAKEN', { name: 'Filip' }),
      )
      expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
    })

    it('lets the person press Join again after a refused name', async () => {
      let tries = 0
      const { router } = await renderGuestJoin({
        answers: {
          [guestJoinRequest]: () => {
            tries += 1
            return tries === 1 ? problemResponse(400, 'MEMBER_NAME_TAKEN') : Response.json({ groupId: testGroupId })
          },
          [meRequest]: jsonAnswer(testGuestMe),
        },
      })
      joinWithNewName('Filip')
      await screen.findByRole('alert')

      typeInto(translated('en', 'auth.signUp.name'), 'Filip D.')
      fireEvent.click(joinButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.group(testGroupId))
      })
    })
  })

  describe('with no free names in the group', () => {
    async function renderWithoutFreeNames(options: RenderOptions = {}) {
      return renderGuestJoin({ ...options, preview: jsonAnswer(openInvitePreview) })
    }

    it.each(languages)('asks "What\'s your name?" instead of "Who are you?" (%s)', async (language) => {
      await renderWithoutFreeNames({ language })

      expect(
        screen.getByRole('heading', { name: translated(language, 'join.guest.whatsYourName') }),
      ).toBeTruthy()
      expect(screen.queryByText(translated(language, 'join.guest.whoAreYou'))).toBeNull()
    })

    it.each(languages)("shows only the I'm new row in the list (%s)", async (language) => {
      await renderWithoutFreeNames({ language })

      expect(listedButtonTexts()).toEqual([translated(language, 'join.guest.imNew')])
    })

    it('joins with a typed name', async () => {
      const { fetchMock } = await renderWithoutFreeNames({ answers: joinedAs() })

      joinWithNewName('Ana')

      await waitFor(() => {
        expect(guestJoinBodies(fetchMock)).toHaveLength(1)
      })
      expect(guestJoinBodies(fetchMock)[0]).toMatchObject({ name: 'Ana' })
    })
  })

  describe('when the join is refused', () => {
    it.each(languages)('shows MEMBER_NOT_FOUND and asks the invite again so the name someone just took disappears (%s)', async (language) => {
      let previews = 0
      const previewWithoutMarko: InvitePreview = { ...openInvitePreviewWithNames, unclaimedNames: [freeNames[1]] }
      const { fetchMock } = await renderGuestJoin({
        language,
        preview: () => {
          previews += 1
          return Response.json(previews === 1 ? openInvitePreviewWithNames : previewWithoutMarko)
        },
        answers: refusedWith(404, 'MEMBER_NOT_FOUND'),
      })

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.MEMBER_NOT_FOUND'),
      )
      await waitFor(() => {
        expect(screen.queryByRole('button', { name: 'Marko' })).toBeNull()
      })
      expect(nameButton('Grandma')).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', endpoints.invitePreview)).toBe(2)
    })

    it.each(
      languages.flatMap((language) =>
        (
          [
            ['INVITE_NOT_FOUND', 404],
            ['RATE_LIMITED', 429],
            ['INVITE_GUEST_CHOICE_INVALID', 400],
            ['AUTH_DISPLAY_NAME_INVALID', 400],
          ] as const
        ).map(([code, status]) => [language, code, status] as const),
      ),
    )('in %s shows the %s text for the answer %s in an alert after a tap on a name, stays and shows no joined toast', async (language, code, status) => {
      const { router } = await renderGuestJoin({ language, answers: refusedWith(status, code) })

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, `errors.${code}`))
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      expect(shownToastTexts()).toEqual([])
      expect(nameButton('Marko').matches(':disabled')).toBe(false)
    })

    it('shows the network message when the join cannot reach the server', async () => {
      await renderGuestJoin({ answers: { [guestJoinRequest]: networkFailureAnswer() } })

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('does not ask the invite again after too many tries, which would be refused too', async () => {
      const { fetchMock } = await renderGuestJoin({ answers: refusedWith(429, 'RATE_LIMITED') })

      fireEvent.click(nameButton('Marko'))
      await screen.findByRole('alert')

      expect(requestCount(fetchMock, 'POST', endpoints.invitePreview)).toBe(1)
    })

    it.each(
      languages.flatMap((language) =>
        (['GROUP_CLOSING', 'GROUP_FINISHED'] as const).map((code) => [language, code] as const),
      ),
    )('in %s shows the %s message with a Home link and no names any more', async (language, code) => {
      await renderGuestJoin({ language, answers: refusedWith(400, code) })

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, `errors.${code}`))
      expect(screen.getByRole('link', { name: translated(language, 'nav.home') }).getAttribute('href')).toBe(routes.dashboard)
      expect(screen.queryByRole('button', { name: 'Marko' })).toBeNull()
      expect(screen.queryByRole('button', { name: translated(language, 'join.guest.imNew') })).toBeNull()
    })

    it('shows GUEST_JOIN_SIGNED_IN as an error toast and asks who is signed in again, then shows the join for that account', async () => {
      consoleErrorSilenced()
      const { fetchMock } = await renderGuestJoin({
        answers: { ...refusedWith(409, 'GUEST_JOIN_SIGNED_IN'), [meRequest]: jsonAnswer(testMe) },
      })

      fireEvent.click(nameButton('Marko'))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.GUEST_JOIN_SIGNED_IN'))
      })
      expect(await screen.findByRole('button', { name: translated('en', 'join.join') })).toBeTruthy()
      expect(requestCount(fetchMock, 'GET', endpoints.me)).toBe(1)
      expect(screen.queryByRole('button', { name: translated('en', 'join.guest.imNew') })).toBeNull()
    })

    it('shows the generic message when the server says nobody is signed in right after the join', async () => {
      await renderGuestJoin({
        answers: {
          [guestJoinRequest]: jsonAnswer({ groupId: testGroupId }),
          [meRequest]: problemAnswer(401, 'AUTH_NOT_SIGNED_IN'),
        },
      })

      fireEvent.click(nameButton('Marko'))

      expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
    })
  })

  describe('the Log in link', () => {
    it.each(languages)('shows "Have an account?" with a Log in button that has a 44 px tap area (%s)', async (language) => {
      await renderGuestJoin({ language })

      expect(screen.getByText(translated(language, 'join.guest.haveAccount'))).toBeTruthy()
      expectTapTarget(logInButton(language), 'The Log in button')
    })

    it.each(languages)('opens a sheet with Google, Log in with email, Cancel and the note about coming back (%s)', async (language) => {
      const { identity } = await renderGuestJoin({ language })

      const sheet = openLogInSheet(language)

      expect(await findDrawnGoogleButton(identity, sheet)).toBeTruthy()
      expect(within(sheet).getByRole('button', { name: translated(language, 'join.guest.logInWithEmail') })).toBeTruthy()
      expect(within(sheet).getByRole('button', { name: translated(language, 'settlements.cancel') })).toBeTruthy()
      expect(within(sheet).getByText(translated(language, 'join.guest.logInNote'))).toBeTruthy()
    })

    it('closes the sheet with Cancel and stays on the join screen', async () => {
      const { router } = await renderGuestJoin()
      const sheet = openLogInSheet()

      fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'settlements.cancel') }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    })

    it('opens the log-in screen carrying the invite token when Log in with email is pressed', async () => {
      const { router } = await renderGuestJoin()
      const sheet = openLogInSheet()

      fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'join.guest.logInWithEmail') }))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.logIn)
      })
      expect(router.state.location.state).toEqual({ joinToken: testInviteToken })
    })

    it('comes back to the join screen as a signed-in person with the normal Join button after Google signs in', async () => {
      const { identity, router, fetchMock } = await renderGuestJoin({
        answers: { [`POST ${endpoints.googleLogIn}`]: jsonAnswer(testMe) },
      })
      const sheet = openLogInSheet()
      const button = await findDrawnGoogleButton(identity, sheet)

      sendGoogleCredential(identity, button, testIdToken)

      expect(await screen.findByRole('button', { name: translated('en', 'join.join') })).toBeTruthy()
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
      expect(router.state.historyAction).toBe('REPLACE')
      expect(screen.queryByRole('button', { name: translated('en', 'join.guest.imNew') })).toBeNull()
      expect(requestCount(fetchMock, 'POST', endpoints.inviteJoinAsGuest)).toBe(0)
    })

    it('goes to the Google name screen carrying the ID token and the invite token when the Google account has no Kvit account yet', async () => {
      const { identity, router } = await renderGuestJoin({
        answers: { [`POST ${endpoints.googleLogIn}`]: problemAnswer(404, 'AUTH_GOOGLE_NO_ACCOUNT') },
      })
      const sheet = openLogInSheet()
      const button = await findDrawnGoogleButton(identity, sheet)

      sendGoogleCredential(identity, button, testIdToken)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.googleSignUp)
      })
      expect(router.state.location.state).toEqual({ idToken: testIdToken, joinToken: testInviteToken })
    })

    it('shows the error toast and stays on the join screen when Google sign-in fails', async () => {
      consoleErrorSilenced()
      const { identity, router } = await renderGuestJoin({
        answers: { [`POST ${endpoints.googleLogIn}`]: problemAnswer(401, 'AUTH_GOOGLE_TOKEN_INVALID') },
      })
      const sheet = openLogInSheet()
      const button = await findDrawnGoogleButton(identity, sheet)

      sendGoogleCredential(identity, button, testIdToken)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.AUTH_GOOGLE_TOKEN_INVALID'))
      })
      expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    })
  })

  describe('a signed-in person', () => {
    it('still sees the normal Join button and none of the guest parts', async () => {
      stubGoogleSignIn()
      stubFetchByRequest({ [previewRequest]: jsonAnswer(openInvitePreviewWithNames) })
      await renderRoutesWithProviders(
        [{ path: routes.join(':token'), element: <JoinScreen /> }],
        routes.join(testInviteToken),
        { seedCache: seedMe(testMe) },
      )

      expect(await screen.findByRole('button', { name: translated('en', 'join.join') })).toBeTruthy()
      expect(screen.queryByText(translated('en', 'join.guest.whoAreYou'))).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'join.guest.imNew') })).toBeNull()
      expect(screen.queryByText(translated('en', 'join.guest.haveAccount'))).toBeNull()
      expect(screen.queryByRole('button', { name: 'Marko' })).toBeNull()
    })
  })
})
