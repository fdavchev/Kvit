import indexCss from '@/index.css?raw'

export type ThemeName = 'light' | 'dark'

export type Rgb = readonly [number, number, number]

export interface Tint {
  rgb: Rgb
  alpha: number
}

export type InteractionState = 'rest' | 'hover'

const glassSupportsHeader = '@supports (backdrop-filter'

export function blockAfter(css: string, header: string): string {
  const headerStart = css.indexOf(header)
  if (headerStart === -1) {
    throw new Error(`index.css has no block that starts with "${header}"`)
  }
  const bodyStart = css.indexOf('{', headerStart) + 1
  let depth = 1
  let position = bodyStart
  while (depth > 0 && position < css.length) {
    if (css[position] === '{') {
      depth += 1
    } else if (css[position] === '}') {
      depth -= 1
    }
    position += 1
  }
  if (depth > 0) {
    throw new Error(`The block "${header}" in index.css is never closed`)
  }
  return css.slice(bodyStart, position - 1)
}

function declarationsOf(block: string): Map<string, string> {
  const declarations = new Map<string, string>()
  for (const match of block.matchAll(/(--[\w-]+)\s*:\s*([^;]+);/g)) {
    declarations.set(match[1], match[2].trim().replace(/\s+/g, ' '))
  }
  return declarations
}

function declarationsOfTheme(theme: ThemeName): Map<string, string> {
  const rootBlock = blockAfter(indexCss, ':root {')
  const darkBlock = blockAfter(rootBlock, '@variant dark')
  const light = new Map<string, string>([
    ...declarationsOf(blockAfter(indexCss, '@theme {')),
    ...declarationsOf(blockAfter(indexCss, '@theme inline {')),
    ...declarationsOf(rootBlock.replace(darkBlock, '')),
  ])
  return theme === 'light' ? light : new Map([...light, ...declarationsOf(darkBlock)])
}

function withoutVariables(text: string, declarations: Map<string, string>): string {
  let resolved = text
  while (resolved.includes('var(')) {
    resolved = resolved.replace(/var\((--[\w-]+)\)/g, (_whole, name: string) => {
      const value = declarations.get(name)
      if (value === undefined) {
        throw new Error(`index.css has no value for ${name}`)
      }
      return value
    })
  }
  return resolved
}

function parseColor(text: string): Rgb {
  const color = text.trim()
  const hex = /^#([0-9a-f]{6})$/i.exec(color)
  if (hex === null) {
    throw new Error(`Only #rrggbb colours are understood, found: ${color}`)
  }
  const value = Number.parseInt(hex[1], 16)
  return [(value >> 16) & 255, (value >> 8) & 255, value & 255]
}

function parseTint(text: string): Tint {
  const color = text.trim()
  const seeThrough = /^color-mix\(in srgb, (#[0-9a-f]{6}) (\d+(?:\.\d+)?)%, transparent\)$/i.exec(color)
  if (seeThrough === null) {
    return { rgb: parseColor(color), alpha: 1 }
  }
  return { rgb: parseColor(seeThrough[1]), alpha: Number(seeThrough[2]) / 100 }
}

function declarationsWithGlass(theme: ThemeName): Map<string, string> {
  const glassRoot = blockAfter(blockAfter(indexCss, glassSupportsHeader), ':root {')
  return new Map([...declarationsOfTheme(theme), ...declarationsOf(glassRoot)])
}

function relativeLuminance(rgb: Rgb): number {
  const [red, green, blue] = rgb.map((channel) => {
    const share = channel / 255
    return share <= 0.03928 ? share / 12.92 : ((share + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue
}

export function hasColorToken(tokenName: string): boolean {
  return declarationsOfTheme('light').has(`--color-${tokenName}`)
}

export function colorOfToken(tokenName: string, theme: ThemeName): Rgb {
  const declarations = declarationsOfTheme(theme)
  const value = declarations.get(`--color-${tokenName}`)
  if (value === undefined) {
    throw new Error(`index.css has no colour token "--color-${tokenName}"`)
  }
  return parseColor(withoutVariables(value, declarations))
}

export function contrastOfColors(first: Rgb, second: Rgb): number {
  const firstLuminance = relativeLuminance(first)
  const secondLuminance = relativeLuminance(second)
  const lighter = Math.max(firstLuminance, secondLuminance)
  const darker = Math.min(firstLuminance, secondLuminance)
  return (lighter + 0.05) / (darker + 0.05)
}

export function contrastBetween(foregroundToken: string, backgroundToken: string, theme: ThemeName): number {
  return contrastOfColors(colorOfToken(foregroundToken, theme), colorOfToken(backgroundToken, theme))
}

export function glassTintOfToken(tokenName: string, theme: ThemeName): Tint {
  const declarations = declarationsWithGlass(theme)
  const value = declarations.get(`--color-${tokenName}`)
  if (value === undefined) {
    throw new Error(`index.css has no colour token "--color-${tokenName}"`)
  }
  return parseTint(withoutVariables(value, declarations))
}

export function tintOfColorUtility(utilityValue: string, theme: ThemeName): Tint {
  const [tokenName, opacityPercent] = utilityValue.split('/')
  const tint = glassTintOfToken(tokenName, theme)
  if (opacityPercent === undefined) {
    return tint
  }
  return { rgb: tint.rgb, alpha: tint.alpha * (Number(opacityPercent) / 100) }
}

export function compositeOver(tint: Tint, backdrop: Rgb): Rgb {
  const [red, green, blue] = tint.rgb.map((channel, index) => tint.alpha * channel + (1 - tint.alpha) * backdrop[index])
  return [red, green, blue]
}

export function colorUtilityOfClasses(
  classNames: readonly string[],
  utility: 'text' | 'border' | 'bg',
  theme: ThemeName,
  state: InteractionState,
): string | null {
  const variants = ['', 'dark:', 'hover:', 'dark:hover:'].filter(
    (variant) => (theme === 'dark' || !variant.includes('dark:')) && (state === 'hover' || !variant.includes('hover:')),
  )
  let found: string | null = null
  for (const variant of variants) {
    for (const className of classNames) {
      if (!className.startsWith(`${variant}${utility}-`)) {
        continue
      }
      const value = className.slice(variant.length + utility.length + 1)
      if (hasColorToken(value.split('/')[0])) {
        found = value
      }
    }
  }
  return found
}

export function colorTokenOfClasses(
  classNames: readonly string[],
  utility: 'text' | 'border' | 'bg',
  theme: ThemeName,
): string | null {
  const variants = theme === 'dark' ? ['', 'dark:'] : ['']
  let found: string | null = null
  for (const variant of variants) {
    for (const className of classNames) {
      if (!className.startsWith(`${variant}${utility}-`)) {
        continue
      }
      const tokenName = className.slice(variant.length + utility.length + 1)
      if (hasColorToken(tokenName)) {
        found = tokenName
      }
    }
  }
  return found
}
