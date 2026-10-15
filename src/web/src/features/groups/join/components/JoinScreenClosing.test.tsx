import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testInviteToken } from '@/test/groupTestData'
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
  stubFetchByRequest,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import type { InvitePreview } from '@/core/services/invites/invitesService'
import { JoinScreen } from './JoinScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const previewRequest = `POST ${endpoints.invitePreview}`
const joinRequest = `POST ${endpoints.inviteJoin}`

const refusalCodes = ['GROUP_CLOSING', 'GROUP_FINISHED'] as const

const refusalCases = languages.flatMap((language) =>
  refusalCodes.map((code) => [language, code] as const),
)

async function renderJoin(language: Language, code: string, preview: InvitePreview = openInvitePreview) {
  stubGoogleSignIn()
  const fetchMock = stubFetchByRequest({
    [previewRequest]: jsonAnswer(preview),
    [joinRequest]: problemAnswer(400, code),
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.join(':token'), element: <JoinScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.dashboard, element: <p>home page</p> },
    ],
    routes.join(testInviteToken),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  await screen.findByText(translated(language, 'join.invitedTo', { name: testInviteGroupName }))
  return { fetchMock, ...rendered }
}

async function pressJoin(language: Language): Promise<void> {
  fireEvent.click(screen.getByRole('button', { name: translated(language, 'join.join') }))
}

describe('JoinScreen when the group is being closed or is finished', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(refusalCases)('shows the translated text of the answer in an alert (%s, %s)', async (language, code) => {
    await renderJoin(language, code)

    await pressJoin(language)

    expect((await screen.findByRole('alert')).textContent).toContain(translated(language, `errors.${code}`))
  })

  it.each(refusalCases)('offers a way back to Home and no way to try again (%s, %s)', async (language, code) => {
    await renderJoin(language, code)

    await pressJoin(language)
    await screen.findByRole('alert')

    const home = screen.getByRole('link', { name: translated(language, 'nav.home') })
    expect(home.getAttribute('href')).toBe(routes.dashboard)
    expect(screen.queryByRole('button', { name: translated(language, 'join.join') })).toBeNull()
    expect(screen.queryByRole('button', { name: translated(language, 'common.retry') })).toBeNull()
  })

  it.each(refusalCodes)('sends the join request only once for %s, so there is no retry loop', async (code) => {
    const { fetchMock } = await renderJoin('en', code)

    await pressJoin('en')
    await screen.findByRole('alert')

    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
  })

  it.each(refusalCodes)('goes Home when the person presses the Home link after %s', async (code) => {
    const { router } = await renderJoin('en', code)
    await pressJoin('en')
    await screen.findByRole('alert')

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'nav.home') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.dashboard)
    })
  })

  it.each(refusalCodes)('shows no joined toast and stays on the join address for %s', async (code) => {
    const { router } = await renderJoin('en', code)

    await pressJoin('en')
    await screen.findByRole('alert')

    expect(router.state.location.pathname).toBe(routes.join(testInviteToken))
    expect(shownToastTexts()).toEqual([])
  })

  it.each(refusalCodes)('shows the same screen for %s when the person answers the question about the names with No, I am new', async (code) => {
    const { fetchMock } = await renderJoin('en', code, openInvitePreviewWithNames)
    await pressJoin('en')
    fireEvent.click(await screen.findByRole('button', { name: translated('en', 'join.imNew') }))

    expect((await screen.findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))

    expect(screen.getByRole('link', { name: translated('en', 'nav.home') })).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
  })

  it.each(refusalCodes)('shows the same screen for %s when the person takes one of the names', async (code) => {
    const { fetchMock } = await renderJoin('en', code, openInvitePreviewWithNames)
    await pressJoin('en')
    fireEvent.click(await screen.findByRole('button', { name: openInvitePreviewWithNames.unclaimedNames[0].name }))

    expect((await screen.findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))

    expect(screen.getByRole('link', { name: translated('en', 'nav.home') })).toBeTruthy()
    expect(requestCount(fetchMock, 'POST', endpoints.inviteJoin)).toBe(1)
  })
})
