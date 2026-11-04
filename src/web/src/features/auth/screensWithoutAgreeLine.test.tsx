import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { agreeLines } from '@/test/agreeLineTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { stubFetchByRequest, jsonAnswer } from '@/test/requestTestHelpers'
import { testMe } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { LogInScreen } from './logIn/components/LogInScreen'
import { WelcomeScreen } from './welcome/components/WelcomeScreen'

describe('the screens that make no account', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows no agree line on Welcome (%s)', async (language) => {
    stubGoogleSignIn()
    stubDeviceColorScheme('light')
    stubFetchByRequest({ 'GET /api/health': () => new Response('Healthy'), 'POST /api/auth/google': jsonAnswer(testMe) })

    await renderElementWithProviders(<WelcomeScreen />, routes.welcome, { language })

    expect(agreeLines(language)).toHaveLength(0)
  })

  it.each(languages)('shows no agree line on Log in (%s)', async (language) => {
    stubGoogleSignIn()
    stubDeviceColorScheme('light')

    await renderElementWithProviders(<LogInScreen />, routes.logIn, { language })

    expect(agreeLines(language)).toHaveLength(0)
  })
})
