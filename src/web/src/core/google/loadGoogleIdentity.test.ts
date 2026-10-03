import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { stubGoogleSignIn, testGoogleScriptUrl } from '@/test/googleTestHelpers'

async function freshLoadGoogleIdentity() {
  vi.resetModules()
  const module = await import('./loadGoogleIdentity')
  return module.loadGoogleIdentity
}

function googleScripts(): HTMLScriptElement[] {
  return Array.from(
    document.head.querySelectorAll<HTMLScriptElement>(`script[src="${testGoogleScriptUrl}"]`),
  )
}

describe('loadGoogleIdentity', () => {
  beforeEach(() => {
    googleScripts().forEach((script) => script.remove())
  })

  afterEach(() => {
    googleScripts().forEach((script) => script.remove())
    vi.unstubAllGlobals()
  })

  it('resolves with the Google identity object that is already on the page', async () => {
    const identity = stubGoogleSignIn()
    const loadGoogleIdentity = await freshLoadGoogleIdentity()

    await expect(loadGoogleIdentity()).resolves.toBe(identity)
  })

  it('adds no script tag when the Google identity object is already on the page', async () => {
    stubGoogleSignIn()
    const loadGoogleIdentity = await freshLoadGoogleIdentity()

    await loadGoogleIdentity()

    expect(googleScripts()).toHaveLength(0)
  })

  it('adds one async script tag for Google to the head when the page has no Google identity object yet', async () => {
    const loadGoogleIdentity = await freshLoadGoogleIdentity()

    void loadGoogleIdentity()

    const scripts = googleScripts()
    expect(scripts).toHaveLength(1)
    expect(scripts[0].parentElement).toBe(document.head)
    expect(scripts[0].async).toBe(true)
  })

  it('resolves with the Google identity object once the script has loaded', async () => {
    const loadGoogleIdentity = await freshLoadGoogleIdentity()
    const loading = loadGoogleIdentity()

    const identity = stubGoogleSignIn()
    googleScripts()[0].dispatchEvent(new Event('load'))

    await expect(loading).resolves.toBe(identity)
  })

  it('rejects with an Error when the script fails to load', async () => {
    const loadGoogleIdentity = await freshLoadGoogleIdentity()
    const loading = loadGoogleIdentity()

    googleScripts()[0].dispatchEvent(new Event('error'))

    await expect(loading).rejects.toBeInstanceOf(Error)
  })

  it('shares one script tag and one result between calls made while the script is loading', async () => {
    const loadGoogleIdentity = await freshLoadGoogleIdentity()
    const first = loadGoogleIdentity()
    const second = loadGoogleIdentity()

    const identity = stubGoogleSignIn()
    googleScripts()[0].dispatchEvent(new Event('load'))

    expect(googleScripts()).toHaveLength(1)
    await expect(first).resolves.toBe(identity)
    await expect(second).resolves.toBe(identity)
  })
})
