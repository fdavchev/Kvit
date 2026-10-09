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

const pictureUrl = 'https://lh3.googleusercontent.com/a/filip-picture=s96-c'

async function renderHome(
  displayName = testMe.displayName,
  language = testMe.language,
  mePictureUrl: string | null = testMe.pictureUrl,
) {
  return renderElementWithProviders(<HomeScreen />, routes.dashboard, {
    language,
    seedCache: seedMe({ ...testMe, displayName, language, pictureUrl: mePictureUrl }),
  })
}

function settingsLink(): HTMLElement {
  return screen.getByRole('link', { name: translated('en', 'settings.title') })
}

function pictureIn(link: HTMLElement): HTMLImageElement {
  const picture = link.querySelector('img')
  if (picture === null) {
    throw new Error('Expected the Settings link to show a picture, found no img element')
  }
  return picture
}

function classNamesIn(link: HTMLElement): string[] {
  return [link, ...link.querySelectorAll('*')].flatMap((element) => [...element.classList])
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

  describe('with a profile picture', () => {
    it('shows the picture from the address of the signed-in person in the round button', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      expect(pictureIn(settingsLink()).getAttribute('src')).toBe(pictureUrl)
    })

    it('asks the browser to send no referrer with the picture request', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      expect(pictureIn(settingsLink()).getAttribute('referrerpolicy')).toBe('no-referrer')
    })

    it('gives the picture an empty alt text, because the button is named by its aria-label', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      expect(pictureIn(settingsLink()).getAttribute('alt')).toBe('')
      expect(screen.queryByRole('img')).toBeNull()
    })

    it('does not write the initial next to the picture', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      expect(settingsLink().textContent).toBe('')
    })

    it.each(languages)('keeps the Settings name on the round button in %s', async (language) => {
      await renderHome('Filip', language, pictureUrl)

      expect(
        screen.getByRole('link', { name: translated(language, 'settings.title') }),
      ).toBeTruthy()
    })

    it('keeps the round button a 48 px round tap target', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      const link = settingsLink()

      expect(link.classList.contains('size-12')).toBe(true)
      expect(link.classList.contains('rounded-full')).toBe(true)
    })

    it('opens the settings screen when the picture is pressed', async () => {
      const { router } = await renderHome('Filip', 'en', pictureUrl)

      fireEvent.click(pictureIn(settingsLink()))

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
    })

    it('keeps the greeting and the placeholder line unchanged', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
      expect(screen.getByText(translated('en', 'home.placeholder'))).toBeTruthy()
    })

    it('shows the initial inside the same link when the picture fails to load', async () => {
      await renderHome('Filip', 'en', pictureUrl)
      const link = settingsLink()

      fireEvent.error(pictureIn(link))

      expect(link.querySelector('img')).toBeNull()
      expect(link.textContent).toBe('F')
      expect(settingsLink()).toBe(link)
    })

    it('shows the upper-case initial of a lower-case non-Latin name when the picture fails to load', async () => {
      await renderHome('ана', 'mk', pictureUrl)
      const link = screen.getByRole('link', { name: translated('mk', 'settings.title') })

      fireEvent.error(pictureIn(link))

      expect(link.textContent).toBe('А')
    })

    it('uses the existing brand colours for the initial circle when the picture fails to load', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      fireEvent.error(pictureIn(settingsLink()))
      const classNames = classNamesIn(settingsLink())

      expect(classNames).toContain('bg-avatar')
      expect(classNames).toContain('text-avatar-foreground')
      expect(settingsLink().outerHTML).not.toMatch(/--avatar-\d/)
    })

    it('still opens the settings screen after the picture failed to load', async () => {
      const { router } = await renderHome('Filip', 'en', pictureUrl)

      fireEvent.error(pictureIn(settingsLink()))
      fireEvent.click(settingsLink())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.settings)
      })
    })

    it('keeps showing the picture when it loads fine', async () => {
      await renderHome('Filip', 'en', pictureUrl)

      fireEvent.load(pictureIn(settingsLink()))

      expect(pictureIn(settingsLink()).getAttribute('src')).toBe(pictureUrl)
      expect(settingsLink().textContent).toBe('')
    })
  })

  describe('without a profile picture', () => {
    it('shows no picture element', async () => {
      await renderHome('Filip', 'en', null)

      expect(settingsLink().querySelector('img')).toBeNull()
    })

    it('keeps the round button a 48 px round tap target', async () => {
      await renderHome('Filip', 'en', null)

      const link = settingsLink()

      expect(link.classList.contains('size-12')).toBe(true)
      expect(link.classList.contains('rounded-full')).toBe(true)
    })

    it('keeps the existing brand colours of the initial circle', async () => {
      await renderHome('Filip', 'en', null)

      const classNames = classNamesIn(settingsLink())

      expect(classNames).toContain('bg-avatar')
      expect(classNames).toContain('text-avatar-foreground')
    })

    it('does not use the colour of a person in a group for the initial circle', async () => {
      await renderHome('Filip', 'en', null)

      expect(settingsLink().outerHTML).not.toMatch(/--avatar-\d/)
    })

    it('keeps the greeting and the placeholder line unchanged', async () => {
      await renderHome('Filip', 'en', null)

      expect(screen.getByText(translated('en', 'home.greeting', { name: 'Filip' }))).toBeTruthy()
      expect(screen.getByText(translated('en', 'home.placeholder'))).toBeTruthy()
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
