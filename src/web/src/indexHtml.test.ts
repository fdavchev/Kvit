import { describe, expect, it } from 'vitest'
import indexHtml from '../index.html?raw'

const page = new DOMParser().parseFromString(indexHtml, 'text/html')
const viewportContent = page.querySelector('meta[name="viewport"]')?.getAttribute('content') ?? ''

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

  it('applies the saved theme before the first paint with one blocking inline script in the head', () => {
    const themeScripts = Array.from(page.head.querySelectorAll('script:not([src])')).filter(
      (script) =>
        script.getAttribute('type') !== 'module' && (script.textContent ?? '').includes('kvit.theme'),
    )

    expect(themeScripts).toHaveLength(1)
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
