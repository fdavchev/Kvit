import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { colorUtilityOfClasses, contrastBetween, type InteractionState, type ThemeName } from '@/test/indexCssColors'
import { KvitButton } from './KvitButton'
import { kvitButtonLooks, type KvitButtonVariant } from './kvitButtonLooks'

const themes: readonly ThemeName[] = ['light', 'dark']
const minimumTextContrast = 4.5
const variantsWithHoverFill = (Object.keys(kvitButtonLooks) as KvitButtonVariant[]).filter((variant) =>
  kvitButtonLooks[variant].className.includes('hover:bg-'),
)
const textShownOnHoveredRows = [
  'foreground',
  'card-foreground',
  'muted-foreground',
  'link',
  'positive',
  'negative',
  'danger',
] as const

function renderedButtonClasses(variant: KvitButtonVariant): string[] {
  render(<KvitButton variant={variant}>Reset link</KvitButton>)
  return Array.from(screen.getByRole('button', { name: 'Reset link' }).classList)
}

function colorUtilityOf(classNames: readonly string[], utility: 'text' | 'bg', theme: ThemeName, state: InteractionState): string {
  const value = colorUtilityOfClasses(classNames, utility, theme, state)
  if (value === null) {
    throw new Error(`The button has no ${utility} colour for ${theme} mode when ${state}: "${classNames.join(' ')}"`)
  }
  return value
}

describe('the KvitButton looks on mouse hover', () => {
  it('has the danger and secondary buttons among the looks with a hover fill', () => {
    expect(variantsWithHoverFill).toEqual(expect.arrayContaining(['danger', 'secondary']))
  })

  describe.each(themes)('in %s mode', (theme) => {
    it.each(variantsWithHoverFill)('keeps the text of the %s button at 4.5:1 or better on its hover colour', (variant) => {
      const classes = renderedButtonClasses(variant)
      const text = colorUtilityOf(classes, 'text', theme, 'hover')
      const fill = colorUtilityOf(classes, 'bg', theme, 'hover')
      const contrast = contrastBetween(text, fill, theme)

      expect(contrast, `${variant}: text-${text} on bg-${fill}, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
    })

    it.each(textShownOnHoveredRows)('keeps %s text at 4.5:1 or better on the shared hover colour of rows and cards', (textToken) => {
      const contrast = contrastBetween(textToken, 'secondary-hover', theme)

      expect(contrast, `${textToken} on secondary-hover, ${contrast.toFixed(2)}`).toBeGreaterThanOrEqual(minimumTextContrast)
    })
  })
})
