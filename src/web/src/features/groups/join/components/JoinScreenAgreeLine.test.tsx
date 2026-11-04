import { fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { InvitePreview } from '@/core/services/invites/invitesService'
import type { Me } from '@/core/services/me/meService'
import { agreeLine, agreeLineLabels, agreeLines, isBefore } from '@/test/agreeLineTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testInviteToken } from '@/test/groupTestData'
import { openInvitePreview, openInvitePreviewWithNames, testInviteGroupName } from '@/test/inviteTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { JoinScreen } from './JoinScreen'

async function renderJoin(preview: InvitePreview, visitor: Me | null, language: Language = 'en') {
  stubGoogleSignIn()
  stubFetchByRequest({ [`POST ${endpoints.invitePreview}`]: jsonAnswer(preview) })
  const rendered = await renderRoutesWithProviders(
    [{ path: routes.join(':token'), element: <JoinScreen /> }],
    routes.join(testInviteToken),
    { language, seedCache: seedMe(visitor === null ? null : { ...visitor, language }) },
  )
  await screen.findByText(translated(language, 'join.invitedTo', { name: testInviteGroupName }))
  return rendered
}

function haveAccountText(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'join.guest.haveAccount'))
}

describe('JoinScreen, the agree line', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('for a visitor without an account, with free names in the group', () => {
    it.each(languages)('shows the line once, under the list with the I\'m new row and above Have an account (%s)', async (language) => {
      await renderJoin(openInvitePreviewWithNames, null, language)

      const list = screen.getByRole('list')

      expect(agreeLines(language)).toHaveLength(1)
      expect(isBefore(list, agreeLine(language))).toBe(true)
      expect(list.contains(agreeLine(language))).toBe(false)
      expect(isBefore(agreeLine(language), haveAccountText(language))).toBe(true)
    })

    it('puts a line under the Join button of the name field once I\'m new is tapped, above Have an account', async () => {
      await renderJoin(openInvitePreviewWithNames, null)
      fireEvent.click(screen.getByRole('button', { name: translated('en', 'join.guest.imNew') }))

      const joinButton = screen.getByRole('button', { name: translated('en', 'join.join') })
      const linesBelowJoin = agreeLines().filter((line) => isBefore(joinButton, line))

      expect(linesBelowJoin).toHaveLength(1)
      expect(isBefore(linesBelowJoin[0], haveAccountText())).toBe(true)
    })

    it('opens the Terms from the line and remembers the invite page for Back', async () => {
      const { router } = await renderJoin(openInvitePreviewWithNames, null)

      fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().terms }))

      expect(router.state.location.pathname).toBe(routes.terms)
      expect(router.state.location.state).toEqual({ from: routes.join(testInviteToken) })
    })

    it('opens the Privacy Policy from the line and remembers the invite page for Back', async () => {
      const { router } = await renderJoin(openInvitePreviewWithNames, null)

      fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().privacy }))

      expect(router.state.location.pathname).toBe(routes.privacy)
      expect(router.state.location.state).toEqual({ from: routes.join(testInviteToken) })
    })
  })

  describe('for a visitor without an account, with no free names in the group', () => {
    it.each(languages)('shows the line once, under the Join button of the name field and above Have an account (%s)', async (language) => {
      await renderJoin(openInvitePreview, null, language)

      const joinButton = screen.getByRole('button', { name: translated(language, 'join.join') })

      expect(agreeLines(language)).toHaveLength(1)
      expect(isBefore(joinButton, agreeLine(language))).toBe(true)
      expect(isBefore(agreeLine(language), haveAccountText(language))).toBe(true)
    })
  })

  describe('for a person who is already signed in', () => {
    it.each([
      ['with free names', openInvitePreviewWithNames],
      ['with no free names', openInvitePreview],
    ])('shows no line when the group has %s', async (_name, preview) => {
      await renderJoin(preview, testMe)

      expect(agreeLines()).toHaveLength(0)
    })
  })
})
