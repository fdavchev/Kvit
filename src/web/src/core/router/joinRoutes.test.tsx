import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { problemResponse } from '@/test/apiTestHelpers'
import { typeInto } from '@/test/formTestHelpers'
import {
  findDrawnGoogleButton,
  sendGoogleCredential,
  stubGoogleSignIn,
  testIdToken,
  type GoogleIdentityStub,
} from '@/test/googleTestHelpers'
import { testGroup, testGroupId, testInviteToken } from '@/test/groupTestData'
import {
  openInvitePreview,
  openInvitePreviewWithNames,
  testInviteGroupName,
} from '@/test/inviteTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { testGuestMe, testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { routeObjects } from './router'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const previewRequest = `POST ${endpoints.invitePreview}`
const joinRequest = `POST ${endpoints.inviteJoin}`

interface JoinFlowOptions {
  preview?: AnswerFactory
  googleLogIn?: AnswerFactory
}

async function openInviteAsSignedOutVisitor(options: JoinFlowOptions = {}) {
  const identity = stubGoogleSignIn()
  const fetchMock = stubFetchByRequest({
    'GET /api/me': problemAnswer(401, 'AUTH_NOT_SIGNED_IN'),
    [previewRequest]: options.preview ?? jsonAnswer(openInvitePreview),
    [`POST ${endpoints.register}`]: jsonAnswer(testMe),
    [`POST ${endpoints.logIn}`]: jsonAnswer(testMe),
    [`POST ${endpoints.googleLogIn}`]: options.googleLogIn ?? jsonAnswer(testMe),
    [`POST ${endpoints.googleSignUp}`]: jsonAnswer(testMe),
    [joinRequest]: jsonAnswer({ groupId: testGroupId }),
    [`GET /api/groups/${testGroupId}`]: jsonAnswer(testGroup),
  })
  const rendered = await renderRoutesWithProviders(routeObjects, routes.join(testInviteToken))
  await showsInviteCard()
  return { fetchMock, identity, ...rendered }
}

async function showsInviteCard(): Promise<void> {
  await screen.findByText(translated('en', 'join.invitedTo', { name: testInviteGroupName }))
}

function pressLink(key: string): void {
  fireEvent.click(screen.getByRole('link', { name: translated('en', key) }))
}

function pressButton(key: string): void {
  fireEvent.click(screen.getByRole('button', { name: translated('en', key) }))
}

async function fillAndSubmitSignUp(): Promise<void> {
  await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })
  typeInto(translated('en', 'auth.signUp.name'), testMe.displayName)
  typeInto(translated('en', 'auth.email'), testMe.email)
  typeInto(translated('en', 'auth.password'), 'Passw0rdOk')
  pressButton('auth.signUp.title')
}

async function fillAndSubmitLogIn(): Promise<void> {
  await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })
  typeInto(translated('en', 'auth.email'), testMe.email)
  typeInto(translated('en', 'auth.password'), 'Passw0rdOk')
  pressButton('auth.logIn.title')
}

function openLogInSheet(): HTMLElement {
  pressButton('auth.logIn.title')
  return screen.getByRole('dialog', { name: translated('en', 'auth.logIn.title') })
}

async function openEmailLogIn(): Promise<void> {
  const sheet = openLogInSheet()
  fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'join.guest.logInWithEmail') }))
  await screen.findByRole('heading', { name: translated('en', 'auth.logIn.title') })
}

async function openSignUp(): Promise<void> {
  await openEmailLogIn()
  pressLink('auth.logIn.noAccount')
  await screen.findByRole('heading', { name: translated('en', 'auth.signUp.title') })
}

async function sendGoogleCredentialFromJoinCard(identity: GoogleIdentityStub): Promise<void> {
  const button = await findDrawnGoogleButton(identity, openLogInSheet())
  sendGoogleCredential(identity, button, testIdToken)
}

async function showsJoinButtonAgain(): Promise<void> {
  await showsInviteCard()
  await screen.findByRole('button', { name: translated('en', 'join.join') })
}

describe('the invite round trip followed through the real route table', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('comes back to the invite card after signing up with email, and Join then opens the group with the toast', async () => {
    const { router, fetchMock } = await openInviteAsSignedOutVisitor()
    await openSignUp()

    await fillAndSubmitSignUp()

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    pressButton('join.join')
    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    })
    expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    expect(shownToastTexts()).toContain(
      translated('en', 'join.joined', { name: testInviteGroupName }),
    )
    expect(requestsOf(fetchMock, 'POST', endpoints.inviteJoin)[0].body).toStrictEqual({
      token: testInviteToken,
    })
  })

  it('shows no bottom bar anywhere on the way from the invite card through sign-up and back', async () => {
    await openInviteAsSignedOutVisitor()
    expect(screen.queryByRole('navigation')).toBeNull()
    await openSignUp()

    await fillAndSubmitSignUp()

    await showsJoinButtonAgain()
    expect(screen.queryByRole('navigation')).toBeNull()
  })

  it('comes back to the invite card after logging in with email from the Log in sheet', async () => {
    const { router } = await openInviteAsSignedOutVisitor()
    await openEmailLogIn()

    await fillAndSubmitLogIn()

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
  })

  it('keeps the invite through a hop from sign-up to log-in and comes back to the invite card', async () => {
    const { router } = await openInviteAsSignedOutVisitor()
    await openSignUp()
    pressLink('welcome.haveAccount')

    await fillAndSubmitLogIn()

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
  })

  it('keeps the invite through a hop from log-in to sign-up and comes back to the invite card', async () => {
    const { router } = await openInviteAsSignedOutVisitor()
    await openEmailLogIn()
    pressLink('auth.logIn.noAccount')

    await fillAndSubmitSignUp()

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
  })

  it('goes back from sign-up to the invite card with the Back button, still signed out', async () => {
    const { router } = await openInviteAsSignedOutVisitor()
    await openSignUp()

    pressLink('common.back')

    await showsInviteCard()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    expect(screen.getByText(translated('en', 'join.guest.whatsYourName'))).toBeTruthy()
  })

  it('shows Join after Google signs in a person who has an account, without leaving the invite', async () => {
    const { identity, router } = await openInviteAsSignedOutVisitor()

    await sendGoogleCredentialFromJoinCard(identity)

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
  })

  it('goes through the Google name screen for a person with no account and comes back to the invite card', async () => {
    const { identity, router } = await openInviteAsSignedOutVisitor({
      googleLogIn: problemAnswer(404, 'AUTH_GOOGLE_NO_ACCOUNT'),
    })
    await sendGoogleCredentialFromJoinCard(identity)
    await screen.findByRole('heading', { name: translated('en', 'auth.googleSignUp.title') })
    expect(router.state.location.pathname).toBe(routes.googleSignUp)

    pressButton('auth.googleSignUp.submit')

    await showsJoinButtonAgain()
    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
  })

  it('asks the new person which name is theirs after signing up, and claims the name that is pressed', async () => {
    const { router, fetchMock } = await openInviteAsSignedOutVisitor({
      preview: jsonAnswer(openInvitePreviewWithNames),
    })
    await openSignUp()
    await fillAndSubmitSignUp()
    await showsJoinButtonAgain()

    pressButton('join.join')
    await screen.findByText(translated('en', 'join.areYou'))
    fireEvent.click(screen.getByRole('button', { name: 'Marko' }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    })
    expect(requestsOf(fetchMock, 'POST', endpoints.inviteJoin)[0].body).toStrictEqual({
      token: testInviteToken,
      claimMemberId: openInvitePreviewWithNames.unclaimedNames[0].id,
    })
  })

  it('never puts the invite token in the address of any request on the whole way', async () => {
    const { fetchMock } = await openInviteAsSignedOutVisitor()
    await openSignUp()
    await fillAndSubmitSignUp()
    await showsJoinButtonAgain()
    pressButton('join.join')
    await screen.findByRole('heading', { level: 1, name: testGroup.name })

    for (const [url] of fetchMock.mock.calls) {
      expect(String(url)).not.toContain(testInviteToken)
    }
  })

  it('asks the server about the invite again after the round trip, because the answer depends on who is asking', async () => {
    const { fetchMock } = await openInviteAsSignedOutVisitor()
    await openSignUp()

    await fillAndSubmitSignUp()

    await showsJoinButtonAgain()
    expect(requestCount(fetchMock, 'POST', endpoints.invitePreview)).toBeGreaterThanOrEqual(2)
  })

  it('joins as a guest with one tap on a free name and opens the group page behind the sign-in guard', async () => {
    let meAnswers = 0
    const identity = stubGoogleSignIn()
    const fetchMock = stubFetchByRequest({
      'GET /api/me': () => {
        meAnswers += 1
        return meAnswers === 1 ? problemResponse(401, 'AUTH_NOT_SIGNED_IN') : Response.json(testGuestMe)
      },
      [previewRequest]: jsonAnswer(openInvitePreviewWithNames),
      [`POST ${endpoints.inviteJoinAsGuest}`]: jsonAnswer({ groupId: testGroupId }),
      [`GET /api/groups/${testGroupId}`]: jsonAnswer(testGroup),
    })
    const { router } = await renderRoutesWithProviders(routeObjects, routes.join(testInviteToken))
    await showsInviteCard()

    fireEvent.click(screen.getByRole('button', { name: 'Marko' }))

    expect(await screen.findByRole('heading', { level: 1, name: testGroup.name })).toBeTruthy()
    expect(router.state.location.pathname).toBe(routes.group(testGroupId))
    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoinAsGuest)).toBe(1)
    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(0)
    expect(identity.renderButton).not.toHaveBeenCalled()
    expect(shownToastTexts()).toContain(translated('en', 'join.joined', { name: testInviteGroupName }))
  })
})
