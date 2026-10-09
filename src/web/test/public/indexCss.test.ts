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

describe('index.css avatar colour tokens', () => {
  const avatarTokenNames = Array.from({ length: 10 }, (_, index) => `--avatar-${index}`)

  function effectiveValue(name: string, mode: 'light' | 'dark'): string | undefined {
    return mode === 'light'
      ? tokenValue(light, name)
      : (tokenValue(dark, name) ?? tokenValue(light, name))
  }

  it.each(avatarTokenNames)('defines %s with a colour in light mode', (name) => {
    expect(effectiveValue(name, 'light')).toBeTruthy()
  })

  it.each(avatarTokenNames)('defines %s with a colour in dark mode', (name) => {
    expect(effectiveValue(name, 'dark')).toBeTruthy()
  })

  it.each(['light', 'dark'] as const)('gives the ten tokens ten different colours in %s mode', (mode) => {
    const colors = avatarTokenNames.map((name) => effectiveValue(name, mode))

    expect(new Set(colors).size).toBe(10)
  })

  it('has no eleventh token, because the colour index wraps around after ten', () => {
    expect(tokenValue(indexCss, '--avatar-10')).toBeUndefined()
  })
})

const categoryColourNames = [
  'orange',
  'green',
  'yellow',
  'blue',
  'pink',
  'purple',
  'slate',
  'red',
  'teal',
  'gray',
]

const minimumTextContrast = 4.5
const hexColour = /^#[0-9a-f]{6}$/

function effectiveTokenValue(name: string, mode: 'light' | 'dark'): string {
  const value = mode === 'light' ? tokenValue(light, name) : (tokenValue(dark, name) ?? tokenValue(light, name))
  if (value === undefined) {
    throw new Error(`index.css defines no ${name} in ${mode} mode`)
  }
  return value
}

function channelLuminance(channel: number): number {
  const fraction = channel / 255
  return fraction <= 0.04045 ? fraction / 12.92 : ((fraction + 0.055) / 1.055) ** 2.4
}

function relativeLuminance(hex: string): number {
  if (!hexColour.test(hex)) {
    throw new Error(`Expected a colour written as #rrggbb, got "${hex}"`)
  }
  const red = Number.parseInt(hex.slice(1, 3), 16)
  const green = Number.parseInt(hex.slice(3, 5), 16)
  const blue = Number.parseInt(hex.slice(5, 7), 16)
  return 0.2126 * channelLuminance(red) + 0.7152 * channelLuminance(green) + 0.0722 * channelLuminance(blue)
}

function contrastWithWhite(hex: string): number {
  return 1.05 / (relativeLuminance(hex) + 0.05)
}

function hueInDegrees(hex: string): number {
  const red = Number.parseInt(hex.slice(1, 3), 16) / 255
  const green = Number.parseInt(hex.slice(3, 5), 16) / 255
  const blue = Number.parseInt(hex.slice(5, 7), 16) / 255
  const highest = Math.max(red, green, blue)
  const spread = highest - Math.min(red, green, blue)
  if (highest === red) {
    return (60 * (((green - blue) / spread) % 6) + 360) % 360
  }
  return highest === green ? 60 * ((blue - red) / spread + 2) : 60 * ((red - green) / spread + 4)
}

describe('the contrast helpers of this test', () => {
  it('gives white on white a ratio of 1', () => {
    expect(contrastWithWhite('#ffffff')).toBeCloseTo(1, 5)
  })

  it('gives white on black a ratio of 21', () => {
    expect(contrastWithWhite('#000000')).toBeCloseTo(21, 5)
  })

  it('gives white on #767676 a ratio of 4.54, the known limit of grey text', () => {
    expect(contrastWithWhite('#767676')).toBeCloseTo(4.54, 2)
  })

  it('gives white on #777777 a ratio of 4.48, a little under the limit', () => {
    expect(contrastWithWhite('#777777')).toBeCloseTo(4.48, 2)
  })

  it('stops with an error naming a colour that is not written as #rrggbb', () => {
    expect(() => contrastWithWhite('var(--category-orange)')).toThrow('var(--category-orange)')
  })
})

describe('index.css category colour tokens', () => {
  it('defines exactly the ten category colours, in this order', () => {
    const definedNames = [...indexCss.matchAll(/(?:^|[\s;{])--category-([a-z]+):/g)].map(([, name]) => name)

    expect(definedNames).toEqual(categoryColourNames)
  })

  it.each(categoryColourNames)('writes --category-%s as a plain #rrggbb colour in light and in dark mode', (name) => {
    expect(effectiveTokenValue(`--category-${name}`, 'light')).toMatch(hexColour)
    expect(effectiveTokenValue(`--category-${name}`, 'dark')).toMatch(hexColour)
  })

  it.each(categoryColourNames)('gives white text on --category-%s a contrast of at least 4.5 to 1 in light mode', (name) => {
    const colour = effectiveTokenValue(`--category-${name}`, 'light')

    expect(
      contrastWithWhite(colour),
      `white on --category-${name} (${colour}) has the contrast ${contrastWithWhite(colour).toFixed(3)}`,
    ).toBeGreaterThanOrEqual(minimumTextContrast)
  })

  it.each(categoryColourNames)('gives white text on --category-%s a contrast of at least 4.5 to 1 in dark mode', (name) => {
    const colour = effectiveTokenValue(`--category-${name}`, 'dark')

    expect(
      contrastWithWhite(colour),
      `white on --category-${name} (${colour}) has the contrast ${contrastWithWhite(colour).toFixed(3)}`,
    ).toBeGreaterThanOrEqual(minimumTextContrast)
  })

  it('keeps the orange an orange: a hue between 22 and 35 degrees, close to the old #c25a00 at 28.5, not a brown-red or a yellow', () => {
    const hue = hueInDegrees(effectiveTokenValue('--category-orange', 'light'))

    expect(hue).toBeGreaterThanOrEqual(22)
    expect(hue).toBeLessThanOrEqual(35)
  })

  it('gives the ten colours ten different values in each mode', () => {
    for (const mode of ['light', 'dark'] as const) {
      const colours = categoryColourNames.map((name) => effectiveTokenValue(`--category-${name}`, mode))

      expect(new Set(colours).size).toBe(10)
    }
  })

  it.each(
    categoryColourNames.map((name, index) => [index, name] as const),
  )('maps --avatar-%i to --category-%s, as before', (index, name) => {
    expect(tokenValue(light, `--avatar-${index}`)).toBe(`var(--category-${name})`)
  })
})
