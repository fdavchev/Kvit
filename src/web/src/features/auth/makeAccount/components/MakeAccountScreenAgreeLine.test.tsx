import { fireEvent, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { agreeLineLabels, agreeLines, isBefore } from '@/test/agreeLineTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { seedMe, testGuestMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { MakeAccountScreen } from './MakeAccountScreen'

async function renderMakeAccount(language: Language = 'en') {
  stubGoogleSignIn()
  return renderRoutesWithProviders(
    [
      { path: routes.makeAccount, element: <MakeAccountScreen /> },
      { path: '*', element: <p>another page</p> },
    ],
    routes.makeAccount,
    { language, seedCache: seedMe({ ...testGuestMe, language }) },
  )
}

describe('MakeAccountScreen, the agree line', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('puts the line under the Save button, where the account is made (%s)', async (language) => {
    await renderMakeAccount(language)

    const save = screen.getByRole('button', { name: translated(language, 'groupSettings.save') })
    const linesBelowSave = agreeLines(language).filter((line) => isBefore(save, line))

    expect(linesBelowSave).toHaveLength(1)
  })

  it('opens the Terms from the line and remembers the make-account screen for Back', async () => {
    const { router } = await renderMakeAccount()

    fireEvent.click(within(agreeLineBelowSave()).getByRole('link', { name: agreeLineLabels().terms }))

    expect(router.state.location.pathname).toBe(routes.terms)
    expect(router.state.location.state).toEqual({ from: routes.makeAccount })
  })

  it('opens the Privacy Policy from the line and remembers the make-account screen for Back', async () => {
    const { router } = await renderMakeAccount()

    fireEvent.click(within(agreeLineBelowSave()).getByRole('link', { name: agreeLineLabels().privacy }))

    expect(router.state.location.pathname).toBe(routes.privacy)
    expect(router.state.location.state).toEqual({ from: routes.makeAccount })
  })
})

function agreeLineBelowSave(language: Language = 'en'): HTMLElement {
  const save = screen.getByRole('button', { name: translated(language, 'groupSettings.save') })
  const line = agreeLines(language).find((candidate) => isBefore(save, candidate))
  if (line === undefined) {
    throw new Error('Found no agree line below the Save button')
  }
  return line
}
