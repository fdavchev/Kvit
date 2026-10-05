import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

const darkVariantStart = '@variant dark {'

const indexCss = readFileSync(path.resolve(import.meta.dirname, '../../src/index.css'), 'utf8')

function balancedBlockAfter(text: string, start: number): { inner: string; end: number } {
  let depth = 0
  for (let position = start; position < text.length; position += 1) {
    if (text[position] === '{') {
      depth += 1
    } else if (text[position] === '}') {
      depth -= 1
      if (depth === 0) {
        return { inner: text.slice(text.indexOf('{', start) + 1, position), end: position + 1 }
      }
    }
  }
  throw new Error(`index.css has an unbalanced block starting at character ${start}`)
}

function splitLightAndDark(css: string): { light: string; dark: string } {
  let light = ''
  let dark = ''
  let cursor = 0
  for (;;) {
    const start = css.indexOf(darkVariantStart, cursor)
    if (start === -1) {
      light += css.slice(cursor)
      break
    }
    light += css.slice(cursor, start)
    const block = balancedBlockAfter(css, start)
    dark += `${block.inner}\n`
    cursor = block.end
  }
  if (dark === '') {
    throw new Error('index.css has no "@variant dark" block')
  }
  return { light, dark }
}

function tokenValue(styles: string, name: string): string | undefined {
  const declaration = new RegExp(`(?:^|[\\s;{])${name}:\\s*([^;]+);`).exec(styles)
  return declaration?.[1].trim().toLowerCase()
}

const { light, dark } = splitLightAndDark(indexCss)

describe('index.css design tokens', () => {
  it.each([
    ['--danger', '#b71f1f', '#ff6b64'],
    ['--danger-fill', '#b71f1f', '#d12828'],
  ])('sets %s to %s in light mode and %s in dark mode', (name, lightValue, darkValue) => {
    expect(tokenValue(light, name)).toBe(lightValue)
    expect(tokenValue(dark, name)).toBe(darkValue)
  })

  it.each([
    ['light', light, '#a51d1d'],
    ['dark', dark, '#ff8a80'],
  ])('keeps --destructive unchanged in %s mode', (_mode, styles, value) => {
    expect(tokenValue(styles, '--destructive')).toBe(value)
  })

  it('gives the selected tab the deep orange #b54a00 in dark mode', () => {
    const tabDeclarations = [...dark.matchAll(/(--[a-z-]*tab[a-z-]*):\s*([^;]+);/g)]

    const orangeTabTokens = tabDeclarations.filter(([, , value]) =>
      ['#b54a00', 'var(--color-brand-700)'].includes(value.trim().toLowerCase()),
    )

    expect(orangeTabTokens.length).toBeGreaterThan(0)
  })

  it('defines --color-brand-700, the orange behind var(--color-brand-700), as #b54a00', () => {
    expect(tokenValue(indexCss, '--color-brand-700')).toBe('#b54a00')
  })
})
