import { render, screen } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import type { Language } from '@/core/i18n/language'
import { routeObjects } from './router'

const crash = new Error('formatMoney got a bad amount from the API')

vi.mock('@/features/auth/welcome/hooks/useApiHealth', () => ({
  useApiHealth: (): void => {
    throw crash
  },
}))

async function renderWelcomeRoute(language: Language): Promise<void> {
  const i18n = await createI18n(language)
  const router = createMemoryRouter(routeObjects, {
    initialEntries: ['/welcome'],
  })
  render(
    <I18nextProvider i18n={i18n}>
      <RouterProvider router={router} />
    </I18nextProvider>,
  )
}

describe('router error element', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it.each([
    ['en', en.errors.generic, en.common.retry],
    ['mk', mk.errors.generic, mk.common.retry],
  ] as const)(
    'shows a translated message with a retry button when a screen crashes while rendering (%s)',
    async (language, message, retry) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})

      await renderWelcomeRoute(language)

      expect((await screen.findByRole('alert')).textContent).toContain(message)
      expect(screen.getByRole('button', { name: retry })).toBeTruthy()
      expect(screen.queryByText(/Unexpected Application Error/)).toBeNull()
    },
  )

  it('logs the error that crashed the screen', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    await renderWelcomeRoute('en')
    await screen.findByRole('alert')

    expect(consoleError).toHaveBeenCalledWith(
      'A screen crashed while rendering',
      crash,
    )
  })
})
