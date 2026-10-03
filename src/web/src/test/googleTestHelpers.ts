import { act, waitFor } from '@testing-library/react'
import { vi, type Mock } from 'vitest'

export interface GoogleCredentialResponse {
  credential: string
}

export interface GoogleInitializeConfig {
  client_id: string
  callback: (response: GoogleCredentialResponse) => void
}

export type GoogleButtonOptions = Record<string, unknown>

export interface GoogleIdentityStub {
  initialize: Mock<(config: GoogleInitializeConfig) => void>
  renderButton: Mock<(element: HTMLElement, options: GoogleButtonOptions) => void>
}

export interface GoogleProfile {
  name: string
  email: string
}

export const testGoogleClientId = 'test-client-id.apps.googleusercontent.com'

export const testGoogleScriptUrl = 'https://accounts.google.com/gsi/client'

export const testGoogleProfile: GoogleProfile = {
  name: 'Марко Петровски',
  email: 'marko.petrovski@gmail.com',
}

function toBase64Url(text: string): string {
  const bytes = new TextEncoder().encode(text)
  const binary = Array.from(bytes, (byte) => String.fromCharCode(byte)).join('')
  return btoa(binary).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '')
}

export function fakeIdToken(claims: object): string {
  const header = toBase64Url(JSON.stringify({ alg: 'RS256', typ: 'JWT' }))
  const payload = toBase64Url(JSON.stringify(claims))
  return `${header}.${payload}.signature`
}

export const testIdToken: string = fakeIdToken(testGoogleProfile)

export function stubGoogleSignIn(): GoogleIdentityStub {
  const identity: GoogleIdentityStub = {
    initialize: vi.fn<(config: GoogleInitializeConfig) => void>(),
    renderButton: vi.fn<(element: HTMLElement, options: GoogleButtonOptions) => void>(),
  }
  vi.stubGlobal('google', { accounts: { id: identity } })
  vi.stubEnv('VITE_GOOGLE_CLIENT_ID', testGoogleClientId)
  return identity
}

export async function findDrawnGoogleButton(
  identity: GoogleIdentityStub,
  region: HTMLElement = document.body,
): Promise<HTMLElement> {
  return waitFor(() => {
    const drawing = identity.renderButton.mock.calls.findLast(
      ([element]) => region.contains(element) && element.getAttribute('aria-hidden') !== 'true',
    )
    if (drawing === undefined) {
      throw new Error('Google has not drawn a visible button inside the region yet')
    }
    return drawing[0]
  })
}

export function sendGoogleCredential(
  identity: GoogleIdentityStub,
  button: HTMLElement,
  idToken: string,
): void {
  const drawIndex = identity.renderButton.mock.calls.findLastIndex(
    ([element]) => element === button,
  )
  if (drawIndex === -1) {
    throw new Error('Google never drew a button into the given element')
  }
  const drawOrder = identity.renderButton.mock.invocationCallOrder[drawIndex]
  const initializeIndex = identity.initialize.mock.invocationCallOrder.findLastIndex(
    (order) => order < drawOrder,
  )
  if (initializeIndex === -1) {
    throw new Error('Google was never initialized before the button was drawn')
  }
  const { callback } = identity.initialize.mock.calls[initializeIndex][0]
  act(() => {
    callback({ credential: idToken })
  })
}
