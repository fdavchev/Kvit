import { describe, expect, it } from 'vitest'
import indexCss from '@/index.css?raw'
import {
  colorOfToken,
  colorUtilityOfClasses,
  compositeOver,
  contrastOfColors,
  glassTintOfToken,
  tintOfColorUtility,
  type InteractionState,
  type Rgb,
  type ThemeName,
  type Tint,
} from '@/test/indexCssColors'
import { kvitGlassButtonLook, kvitGlassDangerButtonLook, kvitGlassPanelBlur, kvitGlassPanelLook } from './kvitGlassLooks'

const themes: readonly ThemeName[] = ['light', 'dark']
const minimumTextContrast = 4.5
const minimumEdgeContrast = 3
const anyBackdrop: readonly Rgb[] = Array.from({ length: 256 }, (_unused, grey): Rgb => [grey, grey, grey])
const popupTextTokens = [
  'card-foreground',
  'popover-foreground',
  'foreground',
  'muted-foreground',
  'link',
  'destructive',
  'danger',
  'positive',
] as const
const surfacesUnderPopups = ['background', 'card'] as const

function classesOf(look: string): string[] {
  return look.split(' ')
}

function colorUtilityOf(look: string, utility: 'text' | 'border' | 'bg', theme: ThemeName, state: InteractionState): string {
  const value = colorUtilityOfClasses(classesOf(look), utility, theme, state)
  if (value === null) {
    throw new Error(`The look has no ${utility} colour for ${theme} mode when ${state}: "${look}"`)
  }
  return value
}

function lowestContrastOverAnyBackdrop(text: Rgb, surface: Tint): number {
  return Math.min(...anyBackdrop.map((backdrop) => contrastOfColors(text, compositeOver(surface, backdrop))))
}

function panelTint(theme: ThemeName): Tint {
  return tintOfColorUtility(colorUtilityOf(kvitGlassPanelLook, 'bg', theme, 'rest'), theme)
}

function panelOver(surface: string, theme: ThemeName): Rgb {
  return compositeOver(panelTint(theme), colorOfToken(surface, theme))
}

describe('the glass panel look of the popups', () => {
  it.each(themes)('paints the see-through glass-panel colour with a bright edge in %s mode', (theme) => {
    expect(colorUtilityOf(kvitGlassPanelLook, 'bg', theme, 'rest')).toBe('glass-panel')
    expect(colorUtilityOf(kvitGlassPanelLook, 'border', theme, 'rest')).toBe('glass-edge')
  })

  it.each(themes)('lets the backdrop show through the panel where blur works in %s mode', (theme) => {
    expect(panelTint(theme).alpha).toBeLessThan(1)
  })

  it.each(themes)('tints the panel with the card colour at 90 percent in %s mode', (theme) => {
    expect(panelTint(theme)).toEqual({ rgb: colorOfToken('card', theme), alpha: 0.9 })
  })

  it.each(themes)('falls back to the solid card colour where the browser cannot blur in %s mode', (theme) => {
    expect(colorOfToken('glass-panel', theme)).toEqual(colorOfToken('card', theme))
  })

  it('turns the see-through tint on only where backdrop-filter works, with or without the -webkit- prefix', () => {
    expect(indexCss).toContain('@supports (backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))')
  })

  it('blurs with one 14px layer and nothing else', () => {
    const blurs = classesOf(kvitGlassPanelLook).filter((className) => className.startsWith('backdrop-blur'))

    expect(blurs).toEqual(['backdrop-blur-[14px]'])
    expect(kvitGlassPanelLook.startsWith(kvitGlassPanelBlur)).toBe(true)
  })

  it('gives every field inside the panel the raised edge colour instead of the page one', () => {
    expect(classesOf(kvitGlassPanelLook)).toContain('[--field-border:var(--field-border-raised)]')
  })

  describe.each(themes)('in %s mode', (theme) => {
    it.each(['card', 'field'] as const)('keeps the raised field edge at 3:1 or better against the %s colour', (surface) => {
      const contrast = contrastOfColors(colorOfToken('field-border-raised', theme), colorOfToken(surface, theme))

      expect(contrast, `raised field edge against ${surface}, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumEdgeContrast)
    })

    it.each(popupTextTokens)('keeps %s text at 4.5:1 or better over any backdrop', (textToken) => {
      const contrast = lowestContrastOverAnyBackdrop(colorOfToken(textToken, theme), panelTint(theme))

      expect(contrast, `${textToken} on the glass panel, worst ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    it('keeps the panel text colour itself at 4.5:1 or better over any backdrop', () => {
      const textToken = colorUtilityOf(kvitGlassPanelLook, 'text', theme, 'rest')

      expect(lowestContrastOverAnyBackdrop(colorOfToken(textToken, theme), panelTint(theme))).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    it('keeps the white text of a danger toast at 4.5:1 or better over any backdrop', () => {
      const contrast = lowestContrastOverAnyBackdrop(colorOfToken('danger-fill-foreground', theme), glassTintOfToken('glass-danger', theme))

      expect(contrast, `danger toast text, worst ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    it('tints a danger toast with the same opacity as the panel', () => {
      expect(glassTintOfToken('glass-danger', theme).alpha).toBe(panelTint(theme).alpha)
    })

    it.each(surfacesUnderPopups)('gives the edge 3:1 or better against the %s around the popup', (surface) => {
      const edge = compositeOver(tintOfColorUtility('glass-edge', theme), panelOver(surface, theme))
      const contrast = contrastOfColors(edge, colorOfToken(surface, theme))

      expect(contrast, `edge against ${surface}, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumEdgeContrast)
    })
  })
})

describe('the glass buttons', () => {
  describe.each(themes)('in %s mode', (theme) => {
    it('keeps the text of the main glass button at 4.5:1 or better over any backdrop', () => {
      const text = colorOfToken(colorUtilityOf(kvitGlassButtonLook, 'text', theme, 'rest'), theme)
      const fill = tintOfColorUtility(colorUtilityOf(kvitGlassButtonLook, 'bg', theme, 'rest'), theme)
      const contrast = lowestContrastOverAnyBackdrop(text, fill)

      expect(contrast, `main glass button text, worst ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    it('keeps the hovered text of the main glass button at 4.5:1 or better', () => {
      const text = colorOfToken(colorUtilityOf(kvitGlassButtonLook, 'text', theme, 'hover'), theme)
      const fill = tintOfColorUtility(colorUtilityOf(kvitGlassButtonLook, 'bg', theme, 'hover'), theme)

      expect(lowestContrastOverAnyBackdrop(text, fill)).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    describe.each<InteractionState>(['rest', 'hover'])('when %s', (state) => {
      const surfaces: readonly [string, () => Rgb][] = [
        ['a card', () => colorOfToken('card', theme)],
        ['the glass panel over the page', () => panelOver('background', theme)],
        ['the glass panel over a card', () => panelOver('card', theme)],
      ]

      it.each(surfaces)('keeps the text of the danger glass button at 4.5:1 or better on %s', (_name, surface) => {
        const text = colorOfToken(colorUtilityOf(kvitGlassDangerButtonLook, 'text', theme, state), theme)
        const fill = tintOfColorUtility(colorUtilityOf(kvitGlassDangerButtonLook, 'bg', theme, state), theme)
        const contrast = contrastOfColors(text, compositeOver(fill, surface()))

        expect(contrast, `danger glass button text, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
      })
    })
  })

  it('keeps the cream text of the danger glass button in dark mode when hovered', () => {
    expect(colorUtilityOf(kvitGlassDangerButtonLook, 'text', 'dark', 'hover')).toBe('foreground')
  })

  it('gives the danger glass button in light mode the darker red text at rest, the same as when hovered', () => {
    expect(colorUtilityOf(kvitGlassDangerButtonLook, 'text', 'light', 'rest')).toBe('destructive')
    expect(colorUtilityOf(kvitGlassDangerButtonLook, 'text', 'light', 'hover')).toBe('destructive')
  })

  it('keeps the text of the danger glass button in light mode at rest at 4.5:1 or better on the glass panel over any backdrop', () => {
    const text = colorOfToken(colorUtilityOf(kvitGlassDangerButtonLook, 'text', 'light', 'rest'), 'light')
    const fill = tintOfColorUtility(colorUtilityOf(kvitGlassDangerButtonLook, 'bg', 'light', 'rest'), 'light')
    const contrast = Math.min(
      ...anyBackdrop.map((backdrop) => contrastOfColors(text, compositeOver(fill, compositeOver(panelTint('light'), backdrop)))),
    )

    expect(contrast, `danger glass button text at rest, worst ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
  })
})
