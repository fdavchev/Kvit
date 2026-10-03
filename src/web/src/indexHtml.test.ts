import { describe, expect, it } from 'vitest'
import indexHtml from '../index.html?raw'

const page = new DOMParser().parseFromString(indexHtml, 'text/html')
const viewportContent = page.querySelector('meta[name="viewport"]')?.getAttribute('content') ?? ''

function requiredElement(selector: string): Element {
  const element = page.querySelector(selector)
  if (element === null) {
    throw new Error(`index.html has no element matching "${selector}"`)
  }
  return element
}

function themeColorFor(scheme: 'light' | 'dark'): string | undefined {
  return (
    page
      .querySelector(`meta[name="theme-color"][media="(prefers-color-scheme: ${scheme})"]`)
      ?.getAttribute('content')
      ?.toLowerCase()
  )
}

describe('index.html', () => {
  it('lets the page extend under the phone notch with viewport-fit=cover', () => {
    expect(viewportContent).toContain('viewport-fit=cover')
  })

  it.each(['user-scalable', 'maximum-scale'])('does not block pinch zoom with %s in the viewport', (setting) => {
    expect(viewportContent).not.toContain(setting)
  })

  it('has no inline script, because the Content-Security-Policy forbids them', () => {
    const inlineScripts = Array.from(page.querySelectorAll('script:not([src])'))

    expect(inlineScripts).toHaveLength(0)
  })

  it('loads /theme-boot.js exactly once', () => {
    expect(page.querySelectorAll('script[src="/theme-boot.js"]')).toHaveLength(1)
  })

  it.each(['type', 'async', 'defer'])('loads /theme-boot.js as a plain blocking script without the %s attribute', (attribute) => {
    expect(requiredElement('script[src="/theme-boot.js"]').hasAttribute(attribute)).toBe(false)
  })

  it('loads /theme-boot.js before the module script', () => {
    const themeBoot = requiredElement('script[src="/theme-boot.js"]')
    const moduleScript = requiredElement('script[type="module"]')

    expect(
      themeBoot.compareDocumentPosition(moduleScript) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy()
  })

  it('loads /theme-boot.js after both theme-color tags, because it changes them', () => {
    const themeBoot = requiredElement('script[src="/theme-boot.js"]')
    const tags = Array.from(page.querySelectorAll('meta[name="theme-color"]'))

    const tagsAfterTheScript = tags.filter(
      (tag) => !(tag.compareDocumentPosition(themeBoot) & Node.DOCUMENT_POSITION_FOLLOWING),
    )

    expect(tagsAfterTheScript).toEqual([])
  })

  it('has exactly two theme-color tags, one per colour scheme', () => {
    expect(page.querySelectorAll('meta[name="theme-color"]')).toHaveLength(2)
  })

  it('colours the phone status bar peach in the light scheme', () => {
    expect(themeColorFor('light')).toBe('#ffd9a8')
  })

  it('colours the phone status bar dark brown in the dark scheme', () => {
    expect(themeColorFor('dark')).toBe('#351f1b')
  })
})
