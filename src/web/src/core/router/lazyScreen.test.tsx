import { act, render, screen } from '@testing-library/react'
import type { ComponentType } from 'react'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import type { Language } from '@/core/i18n/language'
import { translated } from '@/test/translated'
import { lazyScreen, ScreenLoadError, type LazyScreen } from './lazyScreen'
import { RouteError } from './RouteError'

const screenPath = '/screen'
const loadFailure = new TypeError('Failed to fetch dynamically imported module')

function PlannerScreen() {
  return <p>planner screen</p>
}

async function renderLazyScreen(Screen: LazyScreen, language: Language = 'en'): Promise<void> {
  const i18n = await createI18n(language)
  const router = createMemoryRouter(
    [{ errorElement: <RouteError />, children: [{ path: screenPath, element: <Screen /> }] }],
    { initialEntries: [screenPath] },
  )
  await act(async () => {
    render(
      <I18nextProvider i18n={i18n}>
        <RouterProvider router={router} />
      </I18nextProvider>,
    )
  })
}

describe('lazyScreen', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('shows the loading spinner until the code of the screen arrives, then the screen', async () => {
    let finishLoading: (screen: ComponentType) => void = () => {}
    const Screen = lazyScreen(
      () =>
        new Promise<ComponentType>((resolve) => {
          finishLoading = resolve
        }),
    )
    await renderLazyScreen(Screen)
    expect(screen.getByRole('status').textContent).toBe(translated('en', 'common.loading'))

    await act(async () => finishLoading(PlannerScreen))

    expect(screen.getByText('planner screen')).toBeTruthy()
    expect(screen.queryByRole('status')).toBeNull()
  })

  it('shows the screen right away when its code was loaded in advance', async () => {
    const Screen = lazyScreen(() => Promise.resolve(PlannerScreen))
    await Screen.preload()

    await renderLazyScreen(Screen)

    expect(screen.getByText('planner screen')).toBeTruthy()
  })

  it('loads the code of the screen once, however often it is asked for', async () => {
    const importScreen = vi.fn(() => Promise.resolve(PlannerScreen))
    const Screen = lazyScreen(importScreen)

    await Screen.preload()
    await Screen.preload()
    await renderLazyScreen(Screen)

    expect(importScreen).toHaveBeenCalledTimes(1)
  })

  it.each(['en', 'mk'] as const)(
    'shows the page-load message with Try again when the code of the screen cannot be loaded (%s)',
    async (language) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const Screen = lazyScreen(() => Promise.reject(loadFailure))

      await renderLazyScreen(Screen, language)

      expect((await screen.findByRole('alert')).textContent).toContain(translated(language, 'errors.pageLoadFailed'))
      expect(screen.getByRole('button', { name: translated(language, 'common.retry') })).toBeTruthy()
    },
  )

  it('logs the load failure with its cause', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const Screen = lazyScreen(() => Promise.reject(loadFailure))

    await renderLazyScreen(Screen)
    await screen.findByRole('alert')

    const [message, error] = consoleError.mock.calls.find(([logged]) => logged === 'A screen crashed while rendering') ?? []
    expect(message).toBe('A screen crashed while rendering')
    expect(error).toBeInstanceOf(ScreenLoadError)
    expect((error as ScreenLoadError).cause).toBe(loadFailure)
  })

  it('rejects the advance load with the load failure', async () => {
    const Screen = lazyScreen(() => Promise.reject(loadFailure))

    await expect(Screen.preload()).rejects.toBeInstanceOf(ScreenLoadError)
  })

  it('tries the code again when the screen opens after a failed advance load', async () => {
    const importScreen = vi
      .fn<() => Promise<ComponentType>>()
      .mockRejectedValueOnce(loadFailure)
      .mockResolvedValue(PlannerScreen)
    const Screen = lazyScreen(importScreen)
    await expect(Screen.preload()).rejects.toBeInstanceOf(ScreenLoadError)

    await renderLazyScreen(Screen)

    expect(screen.getByText('planner screen')).toBeTruthy()
    expect(importScreen).toHaveBeenCalledTimes(2)
  })
})
