import { fireEvent, screen, waitFor } from '@testing-library/react'
import { useRouteError } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  renderElementWithProviders,
  renderRoutesWithProviders,
} from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { HomeScreen } from './HomeScreen'

function ShownRouteError() {
  const error: unknown = useRouteError()
  return <p>{error instanceof Error ? error.message : 'not an Error'}</p>
}

async function renderHome(displayName = testMe.displayName, language = testMe.language) {
  return renderElementWithProviders(<HomeScreen />, routes.dashboard, {
    language,
    seedCache: seedMe({ ...testMe, displayName, language }),
  })
}

describe('HomeScreen', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it.each(languages)('greets the signed-in person by name in %s', async (language) => {
    await renderHome('Filip', language)

    expect(
      screen.getByText(translated(language, 'home.greeting', { name: 'Filip' })),
    ).toBeTruthy()
  })

  it.each(languages)('shows the placeholder line about groups and balances in %s', async (language) => {
    await renderHome('Filip', language)

    expect(screen.getByText(translated(language, 'home.placeholder'))).toBeTruthy()
  })

  it.each([
    ['Filip', 'F'],
    ['simona', 'S'],
    ['ана', 'А'],
  ])('shows the upper-case first letter of the name %s as the round button: %s', async (displayName, initial) => {
    await renderHome(displayName)

    const settingsButton = screen.getByLabelText(translated('en', 'settings.title'))

    expect(settingsButton.textContent).toBe(initial)
  })

  it('opens the settings screen when the round button is pressed', async () => {
    const { router } = await renderHome()

    fireEvent.click(screen.getByLabelText(translated('en', 'settings.title')))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.settings)
    })
  })

  it('crashes with a clear error naming the signed-in person when nobody is in the me cache', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    await renderRoutesWithProviders(
      [{ path: routes.dashboard, element: <HomeScreen />, errorElement: <ShownRouteError /> }],
      routes.dashboard,
      { seedCache: seedMe(null) },
    )

    expect(await screen.findByText(/signed-in/)).toBeTruthy()
  })
})
