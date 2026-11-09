import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { agreeLine, agreeLineLabels, agreeLines, agreeLineText, isBefore } from '@/test/agreeLineTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { SignUpScreen } from './SignUpScreen'

async function renderSignUp(language: Language = 'en') {
  return renderElementWithProviders(<SignUpScreen />, routes.signUp, { language })
}

describe('SignUpScreen, the agree line', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the agree line once (%s)', async (language) => {
    await renderSignUp(language)

    expect(agreeLines(language)).toHaveLength(1)
    expect(agreeLine(language).textContent).toBe(agreeLineText(language))
  })

  it.each(languages)('puts the line under the Create account button and above the Have an account link (%s)', async (language) => {
    await renderSignUp(language)

    const submit = screen.getByRole('button', { name: translated(language, 'auth.signUp.title') })
    const logInLink = screen.getByRole('link', { name: translated(language, 'welcome.haveAccount') })

    expect(isBefore(submit, agreeLine(language))).toBe(true)
    expect(isBefore(agreeLine(language), logInLink)).toBe(true)
  })

  it('opens the Terms from the line and remembers the sign-up screen for Back', async () => {
    const { router } = await renderSignUp()

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().terms }))

    expect(router.state.location.pathname).toBe(routes.terms)
    expect(router.state.location.state).toEqual({ from: routes.signUp, fromState: null })
  })

  it('opens the Privacy Policy from the line and remembers the sign-up screen for Back', async () => {
    const { router } = await renderSignUp()

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().privacy }))

    expect(router.state.location.pathname).toBe(routes.privacy)
    expect(router.state.location.state).toEqual({ from: routes.signUp, fromState: null })
  })
})
