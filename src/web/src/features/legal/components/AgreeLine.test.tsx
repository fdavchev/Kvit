import { fireEvent, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { agreeLine, agreeLineLabels, agreeLineText, agreeLines } from '@/test/agreeLineTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { AgreeLine } from './AgreeLine'

const pathsItCanSitOn = [routes.signUp, routes.googleSignUp, routes.makeAccount]

describe('AgreeLine', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows one line whose text is the translation without its tags (%s)', async (language) => {
    await renderElementWithProviders(<AgreeLine />, routes.signUp, { language })

    expect(agreeLines(language)).toHaveLength(1)
    expect(agreeLine(language).textContent).toBe(agreeLineText(language))
  })

  it.each(languages)('has exactly two links in the line, the Terms first and the Privacy Policy second (%s)', async (language) => {
    await renderElementWithProviders(<AgreeLine />, routes.signUp, { language })

    const links = within(agreeLine(language)).getAllByRole('link')

    expect(links.map((link) => link.textContent)).toEqual([
      agreeLineLabels(language).terms,
      agreeLineLabels(language).privacy,
    ])
  })

  it.each(languages)('points the first link to the Terms and the second to the Privacy Policy (%s)', async (language) => {
    await renderElementWithProviders(<AgreeLine />, routes.signUp, { language })

    const labels = agreeLineLabels(language)

    expect(screen.getByRole('link', { name: labels.terms }).getAttribute('href')).toBe(routes.terms)
    expect(screen.getByRole('link', { name: labels.privacy }).getAttribute('href')).toBe(routes.privacy)
  })

  it('is a small grey centered line', async () => {
    await renderElementWithProviders(<AgreeLine />, routes.signUp)

    const line = agreeLine()

    expect(line.closest('.text-center')).not.toBeNull()
    expect(line.closest('.text-muted-foreground')).not.toBeNull()
  })

  it.each(pathsItCanSitOn)('opens the Terms with the page it sits on as the place Back returns to (%s)', async (path) => {
    const { router } = await renderElementWithProviders(<AgreeLine />, path)

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().terms }))

    expect(router.state.location.pathname).toBe(routes.terms)
    expect(router.state.location.state).toEqual({ from: path })
  })

  it.each(pathsItCanSitOn)('opens the Privacy Policy with the page it sits on as the place Back returns to (%s)', async (path) => {
    const { router } = await renderElementWithProviders(<AgreeLine />, path)

    fireEvent.click(screen.getByRole('link', { name: agreeLineLabels().privacy }))

    expect(router.state.location.pathname).toBe(routes.privacy)
    expect(router.state.location.state).toEqual({ from: path })
  })
})
