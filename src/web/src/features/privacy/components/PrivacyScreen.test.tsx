import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetch } from '@/test/apiTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { PrivacyScreen } from './PrivacyScreen'

const sectionNumbers = [1, 2, 3, 4, 5, 6, 7]

const sectionsInEachLanguage = languages.flatMap((language) =>
  sectionNumbers.map((number) => [language, number] as const),
)

async function renderPrivacy(options: { language?: Language; routerState?: unknown } = {}) {
  return renderElementWithProviders(<PrivacyScreen />, routes.privacy, {
    language: options.language ?? 'en',
    routerState: options.routerState,
  })
}

describe('PrivacyScreen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the privacy heading (%s)', async (language) => {
    await renderPrivacy({ language })

    expect(
      screen.getByRole('heading', { level: 1, name: translated(language, 'privacy.title') }),
    ).toBeTruthy()
  })

  it.each(languages)('shows the last-updated line (%s)', async (language) => {
    await renderPrivacy({ language })

    expect(screen.getByText(translated(language, 'privacy.updated'))).toBeTruthy()
  })

  it.each(languages)('shows seven section headings in order (%s)', async (language) => {
    await renderPrivacy({ language })

    const headings = screen
      .getAllByRole('heading', { level: 2 })
      .map((heading) => heading.textContent)

    expect(headings).toEqual(
      sectionNumbers.map((number) => translated(language, `privacy.sections.${number}.heading`)),
    )
  })

  it.each(sectionsInEachLanguage)('shows the paragraph of section %s-%i right after its heading', async (language, number) => {
    await renderPrivacy({ language })

    const heading = screen.getByRole('heading', {
      level: 2,
      name: translated(language, `privacy.sections.${number}.heading`),
    })

    expect(heading.nextElementSibling?.textContent).toBe(
      translated(language, `privacy.sections.${number}.body`),
    )
  })

  it('shows the screen without asking the server anything, so it works for a signed-out visitor', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 401 }))

    await renderPrivacy()

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each([
    ['no screen asked to come back', undefined, routes.welcome],
    ['the welcome screen', { from: routes.welcome }, routes.welcome],
    ['the settings screen', { from: routes.settings }, routes.settings],
  ])('has a back button that leads to the right screen when %s', async (_name, routerState, path) => {
    await renderPrivacy({ routerState })

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(path)
  })
})
