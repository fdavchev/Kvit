import indexCss from '@/index.css?raw'

export type ThemeName = 'light' | 'dark'

type Rgb = readonly [number, number, number]

const colorMixPrefix = 'color-mix('

function blockAfter(css: string, header: string): string {
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

function splitAtTopLevelCommas(text: string): string[] {
  const parts: string[] = []
  let depth = 0
  let partStart = 0
  for (let position = 0; position < text.length; position += 1) {
    if (text[position] === '(') {
      depth += 1
    } else if (text[position] === ')') {
      depth -= 1
    } else if (text[position] === ',' && depth === 0) {
      parts.push(text.slice(partStart, position).trim())
      partStart = position + 1
    }
  }
  parts.push(text.slice(partStart).trim())
  return parts
}

function parseMixPart(part: string): { rgb: Rgb; percent: number | null } {
  const match = /^(.*?)(?:\s+(\d+(?:\.\d+)?)%)?$/s.exec(part)
  if (match === null) {
    throw new Error(`Cannot read the colour-mix part "${part}"`)
  }
  return { rgb: parseColor(match[1]), percent: match[2] === undefined ? null : Number(match[2]) }
}

function parseColor(text: string): Rgb {
  const color = text.trim()
  const hex = /^#([0-9a-f]{6})$/i.exec(color)
  if (hex !== null) {
    const value = Number.parseInt(hex[1], 16)
    return [(value >> 16) & 255, (value >> 8) & 255, value & 255]
  }
  if (color.startsWith(colorMixPrefix) && color.endsWith(')')) {
    const parts = splitAtTopLevelCommas(color.slice(colorMixPrefix.length, -1))
    if (parts.length !== 3 || parts[0] !== 'in srgb') {
      throw new Error(`Only "color-mix(in srgb, A, B)" is understood, found: ${color}`)
    }
    const first = parseMixPart(parts[1])
    const second = parseMixPart(parts[2])
    const firstShare = (first.percent ?? 100 - (second.percent ?? 50)) / 100
    const mixed = (channel: 0 | 1 | 2): number =>
      Math.round(first.rgb[channel] * firstShare + second.rgb[channel] * (1 - firstShare))
    return [mixed(0), mixed(1), mixed(2)]
  }
  throw new Error(`Only #rrggbb and color-mix(in srgb, ...) colours are understood, found: ${color}`)
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

export function contrastBetween(foregroundToken: string, backgroundToken: string, theme: ThemeName): number {
  const foreground = relativeLuminance(colorOfToken(foregroundToken, theme))
  const background = relativeLuminance(colorOfToken(backgroundToken, theme))
  const lighter = Math.max(foreground, background)
  const darker = Math.min(foreground, background)
  return (lighter + 0.05) / (darker + 0.05)
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
