import { act, cleanup, fireEvent, screen, waitFor } from '@testing-library/react'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import type { ThemeChoice } from '@/core/theme/theme'
import { KvitThemeToggle } from '@/shared/components/KvitThemeToggle'
import {
  sendGoogleCredential,
  stubGoogleSignIn,
  testGoogleClientId,
  testGoogleScriptUrl,
  type GoogleButtonOptions,
  type GoogleIdentityStub,
} from '@/test/googleTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { installFakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import {
  resetThemeDocument,
  stubDeviceColorScheme,
  type ColorScheme,
} from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { GoogleSignInButton } from './GoogleSignInButton'

async function renderButton(
  onCredential: (idToken: string) => void = () => {},
  options: { language?: Language; saved?: ThemeChoice; device?: ColorScheme } = {},
) {
  if (options.saved !== undefined) {
    window.localStorage.setItem('kvit.theme', options.saved)
  }
  stubDeviceColorScheme(options.device ?? 'light')
  return renderElementWithProviders(<GoogleSignInButton onCredential={onCredential} />, '/', {
    language: options.language ?? 'en',
  })
}

function slot(): HTMLElement {
  const slots = document.querySelectorAll<HTMLElement>('[data-language]')
  if (slots.length !== 1) {
    throw new Error(`Expected exactly one Google button slot, found ${slots.length}`)
  }
  return slots[0]
}

function rootOfSlot(): HTMLElement {
  const root = slot().parentElement
  if (root === null) {
    throw new Error('The Google button slot has no parent element')
  }
  return root
}

async function waitForDraws(identity: GoogleIdentityStub, count: number): Promise<void> {
  await waitFor(() => {
    expect(identity.renderButton).toHaveBeenCalledTimes(count)
  })
}

function drawnLocales(identity: GoogleIdentityStub): unknown[] {
  return identity.renderButton.mock.calls.map(([, options]) => options.locale)
}

function lastDraw(identity: GoogleIdentityStub): GoogleButtonOptions {
  const draw = identity.renderButton.mock.calls.at(-1)
  if (draw === undefined) {
    throw new Error('Google has not drawn a button yet')
  }
  return draw[1]
}

function stubElementWidth(width: number): void {
  vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockReturnValue(width)
  vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(width)
  vi.spyOn(Element.prototype, 'getBoundingClientRect').mockReturnValue(
    DOMRect.fromRect({ x: 0, y: 0, width, height: 44 }),
  )
}

function failEveryGoogleScript(): void {
  document.head
    .querySelectorAll(`script[src="${testGoogleScriptUrl}"]`)
    .forEach((script) => script.dispatchEvent(new Event('error')))
}

function SwitchableHandler({
  first,
  second,
}: {
  first: (idToken: string) => void
  second: (idToken: string) => void
}) {
  const [usesSecond, setUsesSecond] = useState<boolean>(false)
  return (
    <>
      <button type="button" onClick={() => setUsesSecond(true)}>
        use the second handler
      </button>
      <GoogleSignInButton onCredential={(idToken) => (usesSecond ? second : first)(idToken)} />
    </>
  )
}

describe('GoogleSignInButton', () => {
  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('initializes Google once with the client id and a callback', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 1)

    expect(identity.initialize).toHaveBeenCalledOnce()
    expect(identity.initialize).toHaveBeenCalledWith(
      expect.objectContaining({ client_id: testGoogleClientId, callback: expect.any(Function) }),
    )
  })

  it.each(languages)('has one root element with one slot, marked with the app language %s, and no hidden slot', async (language) => {
    stubGoogleSignIn()

    await renderButton(() => {}, { language })

    expect(slot().getAttribute('data-language')).toBe(language)
    expect(Array.from(rootOfSlot().children)).toEqual([slot()])
    expect(slot().hasAttribute('aria-hidden')).toBe(false)
    expect(slot().hasAttribute('inert')).toBe(false)
    expect(slot().classList.contains('opacity-0')).toBe(false)
  })

  it('keeps the scheme-light class on the root element', async () => {
    stubGoogleSignIn()

    await renderButton()

    expect(rootOfSlot().classList.contains('scheme-light')).toBe(true)
  })

  it('draws exactly one Google button on the first draw, into the slot on the page', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 1)

    expect(identity.renderButton.mock.calls.map(([element]) => element)).toEqual([slot()])
    expect(document.body.contains(slot())).toBe(true)
  })

  it.each(languages)('draws the button only with the locale of the app language %s, never in the other language', async (language) => {
    const identity = stubGoogleSignIn()

    await renderButton(() => {}, { language })
    await waitForDraws(identity, 1)

    expect(drawnLocales(identity)).toEqual([language])
  })

  it('asks for the standard large pill button that says "Continue with" with the logo on the left', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 1)

    expect(lastDraw(identity)).toMatchObject({
      type: 'standard',
      size: 'large',
      text: 'continue_with',
      shape: 'pill',
      logo_alignment: 'left',
    })
  })

  it.each([
    ['light', 'light'],
    ['light', 'dark'],
    ['dark', 'light'],
    ['dark', 'dark'],
    ['system', 'light'],
    ['system', 'dark'],
  ] as const)('draws the button with the outline Google theme for the %s saved choice on a %s device', async (saved, device) => {
    const identity = stubGoogleSignIn()

    await renderButton(() => {}, { saved, device })
    await waitForDraws(identity, 1)

    expect(lastDraw(identity)).toMatchObject({ theme: 'outline' })
  })

  it.each([360, 1000])('draws the button with a width in pixels of at most 400 inside a %i pixel wide container', async (containerWidth) => {
    const identity = stubGoogleSignIn()
    stubElementWidth(containerWidth)

    await renderButton()
    await waitForDraws(identity, 1)

    const { width } = lastDraw(identity)
    expect(width).toMatch(/^\d+$/)
    expect(Number(width)).toBeGreaterThan(0)
    expect(Number(width)).toBeLessThanOrEqual(400)
  })

  it.each([
    ['en', 'mk'],
    ['mk', 'en'],
  ] as const)('draws the button once more when the app language changes from %s to %s, in the new language', async (from, to) => {
    const identity = stubGoogleSignIn()
    const { i18n } = await renderButton(() => {}, { language: from })
    await waitForDraws(identity, 1)

    await act(() => i18n.changeLanguage(to))

    await waitForDraws(identity, 2)
    expect(drawnLocales(identity)).toEqual([from, to])
    expect(identity.renderButton.mock.calls[1][0]).toBe(slot())
    expect(slot().getAttribute('data-language')).toBe(to)
  })

  it('keeps the width when it draws the button again for a new language', async () => {
    const identity = stubGoogleSignIn()
    stubElementWidth(320)
    const { i18n } = await renderButton(() => {}, { language: 'en' })
    await waitForDraws(identity, 1)

    await act(() => i18n.changeLanguage('mk'))

    await waitForDraws(identity, 2)
    expect(lastDraw(identity)).toMatchObject({ width: '320', locale: 'mk' })
  })

  it('does not initialize Google again when the app language changes', async () => {
    const identity = stubGoogleSignIn()
    const { i18n } = await renderButton(() => {}, { language: 'en' })
    await waitForDraws(identity, 1)

    await act(() => i18n.changeLanguage('mk'))
    await waitForDraws(identity, 2)

    expect(identity.initialize).toHaveBeenCalledOnce()
  })

  describe('when the saved theme choice changes after the first draw', () => {
    async function renderWithThemeToggleAndTapIt(
      saved: ThemeChoice,
      device: ColorScheme,
      toggleText: 'common.switchToDarkMode' | 'common.switchToLightMode',
    ): Promise<GoogleIdentityStub> {
      const identity = stubGoogleSignIn()
      window.localStorage.setItem('kvit.theme', saved)
      stubDeviceColorScheme(device)
      await renderElementWithProviders(
        <>
          <KvitThemeToggle />
          <GoogleSignInButton onCredential={() => {}} />
        </>,
        '/',
      )
      await waitForDraws(identity, 1)

      fireEvent.click(screen.getByRole('button', { name: translated('en', toggleText) }))
      await waitFor(() => {
        expect(screen.queryByRole('button', { name: translated('en', toggleText) })).toBeNull()
      })
      return identity
    }

    const changes = [
      ['light', 'light', 'common.switchToDarkMode'],
      ['dark', 'light', 'common.switchToLightMode'],
      ['system', 'dark', 'common.switchToLightMode'],
    ] as const

    it.each(changes)('draws no new Google button when the %s saved choice on a %s device is changed', async (saved, device, toggleText) => {
      const identity = await renderWithThemeToggleAndTapIt(saved, device, toggleText)

      expect(identity.renderButton).toHaveBeenCalledTimes(1)
    })

    it.each(changes)('does not initialize Google again when the %s saved choice on a %s device is changed', async (saved, device, toggleText) => {
      const identity = await renderWithThemeToggleAndTapIt(saved, device, toggleText)

      expect(identity.initialize).toHaveBeenCalledOnce()
    })
  })

  describe('when the device colour scheme changes after the first draw', () => {
    async function renderOnLightDeviceThenSwitchToDarkAndChangeLanguage(): Promise<GoogleIdentityStub> {
      const identity = stubGoogleSignIn()
      const { i18n } = await renderButton(() => {}, { saved: 'system', device: 'light' })
      await waitForDraws(identity, 1)

      stubDeviceColorScheme('dark')
      await act(() => i18n.changeLanguage('mk'))
      await waitForDraws(identity, 2)
      return identity
    }

    it('draws only the one new button the language change asks for, still in the outline theme', async () => {
      const identity = await renderOnLightDeviceThenSwitchToDarkAndChangeLanguage()

      expect(identity.renderButton).toHaveBeenCalledTimes(2)
      expect(lastDraw(identity)).toMatchObject({ theme: 'outline' })
    })

    it('does not initialize Google again', async () => {
      const identity = await renderOnLightDeviceThenSwitchToDarkAndChangeLanguage()

      expect(identity.initialize).toHaveBeenCalledOnce()
    })
  })

  it.each(languages)('passes the credential Google returns to onCredential (%s)', async (language) => {
    const identity = stubGoogleSignIn()
    const onCredential = vi.fn<(idToken: string) => void>()
    await renderButton(onCredential, { language })
    await waitForDraws(identity, 1)

    sendGoogleCredential(identity, slot(), 'header.payload.signature')

    expect(onCredential).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
  })

  it('passes the credential to the latest onCredential after the parent renders again', async () => {
    const identity = stubGoogleSignIn()
    const first = vi.fn<(idToken: string) => void>()
    const second = vi.fn<(idToken: string) => void>()
    stubDeviceColorScheme('light')
    await renderElementWithProviders(<SwitchableHandler first={first} second={second} />, '/')
    await waitForDraws(identity, 1)
    fireEvent.click(screen.getByRole('button', { name: 'use the second handler' }))

    sendGoogleCredential(identity, slot(), 'header.payload.signature')

    expect(second).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
    expect(first).not.toHaveBeenCalled()
  })

  it('passes the credential to onCredential after the app language changed', async () => {
    const identity = stubGoogleSignIn()
    const onCredential = vi.fn<(idToken: string) => void>()
    const { i18n } = await renderButton(onCredential, { language: 'en' })
    await waitForDraws(identity, 1)
    await act(() => i18n.changeLanguage('mk'))
    await waitForDraws(identity, 2)

    sendGoogleCredential(identity, slot(), 'header.payload.signature')

    expect(onCredential).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
  })

  it('initializes Google only once in the page when a Google button is shown again later', async () => {
    const identity = stubGoogleSignIn()
    await renderButton()
    await waitForDraws(identity, 1)
    cleanup()

    await renderButton()
    await waitForDraws(identity, 2)

    expect(identity.initialize).toHaveBeenCalledOnce()
  })

  it('passes the credential to the onCredential of the Google button shown again later', async () => {
    const identity = stubGoogleSignIn()
    const first = vi.fn<(idToken: string) => void>()
    const second = vi.fn<(idToken: string) => void>()
    await renderButton(first)
    await waitForDraws(identity, 1)
    cleanup()
    await renderButton(second)
    await waitForDraws(identity, 2)

    sendGoogleCredential(identity, slot(), 'header.payload.signature')

    expect(second).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
    expect(first).not.toHaveBeenCalled()
  })

  describe('when the window is resized after the first draw', () => {
    it('draws the button again with the new width once the resizing stops', async () => {
      const resizeObservers = installFakeResizeObservers()
      const identity = stubGoogleSignIn()
      stubElementWidth(360)
      await renderButton()
      await waitForDraws(identity, 1)

      stubElementWidth(300)
      resizeObservers.fireFor(rootOfSlot())
      resizeObservers.fireFor(rootOfSlot())

      await waitForDraws(identity, 2)
      expect(lastDraw(identity)).toMatchObject({ width: '300', locale: 'en' })
    })

    it('draws nothing again when the width stayed the same', async () => {
      const resizeObservers = installFakeResizeObservers()
      const identity = stubGoogleSignIn()
      stubElementWidth(360)
      await renderButton()
      await waitForDraws(identity, 1)

      resizeObservers.fireFor(rootOfSlot())
      await act(() => new Promise<void>((resolve) => setTimeout(resolve, 400)))

      expect(identity.renderButton).toHaveBeenCalledTimes(1)
    })

    it('keeps the widest button at 400 pixels on a window that gets wider', async () => {
      const resizeObservers = installFakeResizeObservers()
      const identity = stubGoogleSignIn()
      stubElementWidth(320)
      await renderButton()
      await waitForDraws(identity, 1)

      stubElementWidth(1000)
      resizeObservers.fireFor(rootOfSlot())

      await waitForDraws(identity, 2)
      expect(lastDraw(identity)).toMatchObject({ width: '400' })
    })
  })

  it('shows no alert while Google loads fine', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 1)

    expect(screen.queryByRole('alert')).toBeNull()
  })
})

describe('the title of the frame Google draws', () => {
  const titleFromGoogleScript = 'Копче за „Најавување со Google“'

  function drawFramesLikeGoogle(identity: GoogleIdentityStub): void {
    identity.renderButton.mockImplementation((element) => {
      const frame = document.createElement('iframe')
      frame.title = titleFromGoogleScript
      element.replaceChildren(frame)
    })
  }

  function frameInSlot(): HTMLIFrameElement {
    const frame = slot().querySelector('iframe')
    if (frame === null) {
      throw new Error('The Google button slot has no Google frame')
    }
    return frame
  }

  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(languages)('titles the frame in the app language %s, not in the language of the Google script', async (language) => {
    const identity = stubGoogleSignIn()
    drawFramesLikeGoogle(identity)

    await renderButton(() => {}, { language })
    await waitForDraws(identity, 1)

    await waitFor(() => {
      expect(frameInSlot().title).toBe(translated(language, 'welcome.googleButtonTitle'))
    })
    expect(document.querySelectorAll('iframe')).toHaveLength(1)
  })

  it.each([
    ['en', 'mk'],
    ['mk', 'en'],
  ] as const)('gives the frame the title in the page language, %s first and %s after a switch', async (from, to) => {
    const identity = stubGoogleSignIn()
    drawFramesLikeGoogle(identity)
    const { i18n } = await renderButton(() => {}, { language: from })
    await waitForDraws(identity, 1)
    await waitFor(() => {
      expect(frameInSlot().title).toBe(translated(from, 'welcome.googleButtonTitle'))
    })

    await act(() => i18n.changeLanguage(to))

    await waitForDraws(identity, 2)
    await waitFor(() => {
      expect(frameInSlot().title).toBe(translated(to, 'welcome.googleButtonTitle'))
    })
    expect(document.querySelectorAll('iframe')).toHaveLength(1)
  })

  it('titles a frame that Google puts into the slot again later', async () => {
    const identity = stubGoogleSignIn()
    drawFramesLikeGoogle(identity)
    await renderButton(() => {}, { language: 'en' })
    await waitForDraws(identity, 1)
    const newFrame = document.createElement('iframe')
    newFrame.title = titleFromGoogleScript

    act(() => {
      slot().replaceChildren(newFrame)
    })

    await waitFor(() => {
      expect(newFrame.title).toBe(translated('en', 'welcome.googleButtonTitle'))
    })
  })
})

describe('GoogleSignInButton when the Google script cannot be loaded', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it.each(languages)('shows the unavailable text as an alert (%s)', async (language) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', testGoogleClientId)
    await renderButton(() => {}, { language })

    failEveryGoogleScript()

    expect((await screen.findByRole('alert')).textContent).toBe(
      translated(language, 'welcome.googleUnavailable'),
    )
  })

  it('logs the cause with console.error', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', testGoogleClientId)
    await renderButton()

    failEveryGoogleScript()
    await screen.findByRole('alert')

    expect(consoleError).toHaveBeenCalledWith(expect.any(String), expect.any(Error))
  })

  it('keeps showing the unavailable text when the app language changes afterwards', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', testGoogleClientId)
    const { i18n } = await renderButton(() => {}, { language: 'en' })
    failEveryGoogleScript()
    await screen.findByRole('alert')

    await act(() => i18n.changeLanguage('mk'))

    expect((await screen.findByRole('alert')).textContent).toBe(translated('mk', 'welcome.googleUnavailable'))
  })
})
