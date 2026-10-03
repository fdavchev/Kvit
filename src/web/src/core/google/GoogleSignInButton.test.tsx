import { act, fireEvent, screen, waitFor } from '@testing-library/react'
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
import {
  resetThemeDocument,
  stubDeviceColorScheme,
  type ColorScheme,
} from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { GoogleSignInButton } from './GoogleSignInButton'

const otherLanguage: Record<Language, Language> = { en: 'mk', mk: 'en' }

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

function slotFor(language: Language): HTMLElement {
  const slot = document.querySelector<HTMLElement>(`[data-language="${language}"]`)
  if (slot === null) {
    throw new Error(`The Google button has no slot with data-language="${language}"`)
  }
  return slot
}

function rootOfSlots(): HTMLElement {
  const root = slotFor('en').parentElement
  if (root === null) {
    throw new Error('The "en" slot of the Google button has no parent element')
  }
  return root
}

async function waitForDraws(identity: GoogleIdentityStub, count: number): Promise<void> {
  await waitFor(() => {
    expect(identity.renderButton).toHaveBeenCalledTimes(count)
  })
}

function drawsInto(identity: GoogleIdentityStub, slot: HTMLElement): GoogleButtonOptions[] {
  return identity.renderButton.mock.calls
    .filter(([element]) => element === slot)
    .map(([, options]) => options)
}

function lastDrawInto(identity: GoogleIdentityStub, slot: HTMLElement): GoogleButtonOptions {
  const draws = drawsInto(identity, slot)
  const lastDraw = draws[draws.length - 1]
  if (lastDraw === undefined) {
    throw new Error('Google has not drawn a button into the given slot')
  }
  return lastDraw
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

function expectActive(slot: HTMLElement): void {
  expect(slot.hasAttribute('aria-hidden')).toBe(false)
  expect(slot.hasAttribute('inert')).toBe(false)
  expect(slot.classList.contains('opacity-0')).toBe(false)
  expect(slot.classList.contains('pointer-events-none')).toBe(false)
  expect(slot.classList.contains('invisible')).toBe(false)
}

function expectInactive(slot: HTMLElement): void {
  expect(slot.getAttribute('aria-hidden')).toBe('true')
  expect(slot.hasAttribute('inert')).toBe(true)
  expect(slot.classList.contains('opacity-0')).toBe(true)
  expect(slot.classList.contains('pointer-events-none')).toBe(true)
  expect(slot.classList.contains('invisible')).toBe(false)
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
    await waitForDraws(identity, 2)

    expect(identity.initialize).toHaveBeenCalledOnce()
    expect(identity.initialize).toHaveBeenCalledWith(
      expect.objectContaining({ client_id: testGoogleClientId, callback: expect.any(Function) }),
    )
  })

  it('has one root element with a slot per app language, in the order en, mk', async () => {
    stubGoogleSignIn()

    await renderButton()

    const slots = document.querySelectorAll('[data-language]')
    expect(Array.from(slots, (slot) => slot.getAttribute('data-language'))).toEqual(['en', 'mk'])
    expect(slots[0].parentElement).toBe(slots[1].parentElement)
    expect(Array.from(rootOfSlots().children)).toEqual(Array.from(slots))
  })

  it('keeps the scheme-light class on the root element', async () => {
    stubGoogleSignIn()

    await renderButton()

    expect(rootOfSlots().classList.contains('scheme-light')).toBe(true)
  })

  it('draws exactly two Google buttons on the first draw, one into each slot of the page', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 2)

    const drawnElements = identity.renderButton.mock.calls.map(([element]) => element)
    expect(drawnElements).toEqual([slotFor('en'), slotFor('mk')])
    drawnElements.forEach((element) => {
      expect(document.body.contains(element)).toBe(true)
    })
  })

  it.each(languages)('draws the %s slot with the locale of the same name', async (language) => {
    const identity = stubGoogleSignIn()

    await renderButton(() => {}, { language: otherLanguage[language] })
    await waitForDraws(identity, 2)

    expect(drawsInto(identity, slotFor(language))).toHaveLength(1)
    expect(lastDrawInto(identity, slotFor(language))).toMatchObject({ locale: language })
  })

  it.each(languages)('asks for the standard large pill button that says "Continue with" with the logo on the left in the %s slot', async (language) => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 2)

    expect(lastDrawInto(identity, slotFor(language))).toMatchObject({
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
  ] as const)('draws both slots with the outline Google theme for the %s saved choice on a %s device', async (saved, device) => {
    const identity = stubGoogleSignIn()

    await renderButton(() => {}, { saved, device })
    await waitForDraws(identity, 2)

    languages.forEach((language) => {
      expect(lastDrawInto(identity, slotFor(language))).toMatchObject({ theme: 'outline' })
    })
  })

  it.each(languages.flatMap((language) => [360, 1000].map((width) => [language, width] as const)))('draws the %s slot with a width in pixels of at most 400 inside a %i pixel wide container', async (language, containerWidth) => {
    const identity = stubGoogleSignIn()
    stubElementWidth(containerWidth)

    await renderButton()
    await waitForDraws(identity, 2)

    const { width } = lastDrawInto(identity, slotFor(language))
    expect(width).toMatch(/^\d+$/)
    expect(Number(width)).toBeGreaterThan(0)
    expect(Number(width)).toBeLessThanOrEqual(400)
  })

  it.each(languages)('shows only the slot of the app language when the app starts in %s', async (language) => {
    stubGoogleSignIn()

    await renderButton(() => {}, { language })

    expectActive(slotFor(language))
    expectInactive(slotFor(otherLanguage[language]))
  })

  it.each([
    ['en', 'mk'],
    ['mk', 'en'],
  ] as const)('switches the active slot from %s to %s when the app language changes', async (from, to) => {
    const identity = stubGoogleSignIn()
    const { i18n } = await renderButton(() => {}, { language: from })
    await waitForDraws(identity, 2)

    await act(() => i18n.changeLanguage(to))

    await waitFor(() => {
      expectActive(slotFor(to))
    })
    expectInactive(slotFor(from))
  })

  it.each([
    ['en', 'mk'],
    ['mk', 'en'],
  ] as const)('draws no new Google button when the app language changes from %s to %s', async (from, to) => {
    const identity = stubGoogleSignIn()
    const { i18n } = await renderButton(() => {}, { language: from })
    await waitForDraws(identity, 2)

    await act(() => i18n.changeLanguage(to))
    await waitFor(() => {
      expectActive(slotFor(to))
    })

    expect(identity.renderButton).toHaveBeenCalledTimes(2)
  })

  it('does not initialize Google again when the app language changes', async () => {
    const identity = stubGoogleSignIn()
    const { i18n } = await renderButton(() => {}, { language: 'en' })
    await waitForDraws(identity, 2)

    await act(() => i18n.changeLanguage('mk'))
    await waitFor(() => {
      expectActive(slotFor('mk'))
    })

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
      await waitForDraws(identity, 2)

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

      expect(identity.renderButton).toHaveBeenCalledTimes(2)
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
      await waitForDraws(identity, 2)

      stubDeviceColorScheme('dark')
      await act(() => i18n.changeLanguage('mk'))
      await waitFor(() => {
        expectActive(slotFor('mk'))
      })
      return identity
    }

    it('draws no new Google button', async () => {
      const identity = await renderOnLightDeviceThenSwitchToDarkAndChangeLanguage()

      expect(identity.renderButton).toHaveBeenCalledTimes(2)
    })

    it('does not initialize Google again', async () => {
      const identity = await renderOnLightDeviceThenSwitchToDarkAndChangeLanguage()

      expect(identity.initialize).toHaveBeenCalledOnce()
    })
  })

  it.each(languages)('passes the credential Google returns to onCredential (%s slot)', async (language) => {
    const identity = stubGoogleSignIn()
    const onCredential = vi.fn<(idToken: string) => void>()
    await renderButton(onCredential)
    await waitForDraws(identity, 2)

    sendGoogleCredential(identity, slotFor(language), 'header.payload.signature')

    expect(onCredential).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
  })

  it('passes the credential to the latest onCredential after the parent renders again', async () => {
    const identity = stubGoogleSignIn()
    const first = vi.fn<(idToken: string) => void>()
    const second = vi.fn<(idToken: string) => void>()
    stubDeviceColorScheme('light')
    await renderElementWithProviders(<SwitchableHandler first={first} second={second} />, '/')
    await waitForDraws(identity, 2)
    fireEvent.click(screen.getByRole('button', { name: 'use the second handler' }))

    sendGoogleCredential(identity, slotFor('en'), 'header.payload.signature')

    expect(second).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
    expect(first).not.toHaveBeenCalled()
  })

  it('passes the credential to onCredential after the app language changed', async () => {
    const identity = stubGoogleSignIn()
    const onCredential = vi.fn<(idToken: string) => void>()
    const { i18n } = await renderButton(onCredential, { language: 'en' })
    await waitForDraws(identity, 2)
    await act(() => i18n.changeLanguage('mk'))
    await waitFor(() => {
      expectActive(slotFor('mk'))
    })

    sendGoogleCredential(identity, slotFor('mk'), 'header.payload.signature')

    expect(onCredential).toHaveBeenCalledExactlyOnceWith('header.payload.signature')
  })

  it('shows no alert while Google loads fine', async () => {
    const identity = stubGoogleSignIn()

    await renderButton()
    await waitForDraws(identity, 2)

    expect(screen.queryByRole('alert')).toBeNull()
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
})
