import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { agreeLine, agreeLineLabels, agreeLines, agreeLineText, isBefore } from '@/test/agreeLineTestHelpers'
import { testIdToken } from '@/test/googleTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { GoogleSignUpScreen } from './GoogleSignUpScreen'

async function renderGoogleSignUp(language: Language = 'en') {
  return renderElementWithProviders(<GoogleSignUpScreen />, routes.googleSignUp, {
    language,
    routerState: { idToken: testIdToken },
  })
}

describe('GoogleSignUpScreen, the agree line', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the agree line once (%s)', async (language) => {
    await renderGoogleSignUp(language)

    expect(agreeLines(language)).toHaveLength(1)
    expect(agreeLine(language).textContent).toBe(agreeLineText(language))
  })

  it.each(languages)('puts the line under the Continue button (%s)', async (language) => {
    await renderGoogleSignUp(language)

    const submit = screen.getByRole('button', { name: translated(language, 'auth.googleSignUp.submit') })

    expect(isBefore(submit, agreeLine(language))).toBe(true)
  })

  it('opens the Terms from the line and remembers this screen for Back', async () => {
    const { router } = await renderGoogleSignUp()

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().terms }))

    expect(router.state.location.pathname).toBe(routes.terms)
    expect(router.state.location.state).toEqual({ from: routes.googleSignUp })
  })

  it('opens the Privacy Policy from the line and remembers this screen for Back', async () => {
    const { router } = await renderGoogleSignUp()

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().privacy }))

    expect(router.state.location.pathname).toBe(routes.privacy)
    expect(router.state.location.state).toEqual({ from: routes.googleSignUp })
  })

  it('shows no agree line to a visitor who arrives without a Google token and is sent to Welcome', async () => {
    await renderElementWithProviders(<GoogleSignUpScreen />, routes.googleSignUp)

    expect(agreeLines()).toHaveLength(0)
  })
})
