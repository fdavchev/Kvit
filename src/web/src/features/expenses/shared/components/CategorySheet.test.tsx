import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { categoryName } from '@/test/expenseTestHelpers'
import { testCategories } from '@/test/expenseTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { CategorySheet } from './CategorySheet'

const minimumLabelSizeInRem = 0.75
const namedTextSizesInRem: Record<string, number> = {
  'text-xs': 0.75,
  'text-sm': 0.875,
  'text-base': 1,
  'text-lg': 1.125,
}

async function renderedSheet(language: Language = 'en'): Promise<HTMLElement> {
  await renderElementWithProviders(
    <CategorySheet categories={testCategories} categoryId={null} onPick={() => {}} onClose={() => {}} />,
    '/category',
    { language },
  )
  return screen.findByRole('dialog', { name: translated(language, 'expense.pickCategory') })
}

function categoryButtons(sheet: HTMLElement): HTMLElement[] {
  return within(sheet).getAllByRole('button')
}

function labelOf(button: HTMLElement): HTMLElement {
  const label = Array.from(button.querySelectorAll<HTMLElement>('span')).find((span) => span.textContent !== '' && span.getAttribute('aria-hidden') !== 'true')
  if (label === undefined) {
    throw new Error(`Expected the category button to hold a visible text label: ${button.outerHTML}`)
  }
  return label
}

function textSizeInRem(classNames: readonly string[]): number | null {
  let found: number | null = null
  for (const className of classNames) {
    const arbitrary = /^text-\[([\d.]+)rem\]$/.exec(className)
    if (arbitrary !== null) {
      found = Number(arbitrary[1])
    } else if (className in namedTextSizesInRem) {
      found = namedTextSizesInRem[className]
    }
  }
  return found
}

function sizeOfLabel(button: HTMLElement): number {
  const size = textSizeInRem([...Array.from(button.classList), ...Array.from(labelOf(button).classList)])
  if (size === null) {
    throw new Error(`Found no text size class on the category button or its label: ${button.className} / ${labelOf(button).className}`)
  }
  return size
}

describe('the Category sheet', () => {
  it('shows every category as its own button with its name', async () => {
    const sheet = await renderedSheet()

    expect(categoryButtons(sheet)).toHaveLength(testCategories.length)
    for (const category of testCategories) {
      expect(within(sheet).getByText(categoryName('en', category))).toBeTruthy()
    }
  })

  it('writes the category names at 12 px or larger, not 11 px', async () => {
    const sheet = await renderedSheet()

    for (const button of categoryButtons(sheet)) {
      expect(sizeOfLabel(button), `the label of ${button.textContent}`).toBeGreaterThanOrEqual(minimumLabelSizeInRem)
    }
  })

  it('lays the categories out in 4 columns so the names fit', async () => {
    const sheet = await renderedSheet()

    const grid = categoryButtons(sheet)[0].parentElement

    expect(Array.from(grid?.classList ?? [])).toContain('grid-cols-4')
    expect(Array.from(grid?.classList ?? [])).not.toContain('grid-cols-5')
  })

  it.each<Language>(['en', 'mk'])('breaks a long name at word edges, with break-words and not wrap-anywhere (%s)', async (language) => {
    const sheet = await renderedSheet(language)

    for (const button of categoryButtons(sheet)) {
      const labelClasses = Array.from(labelOf(button).classList)
      expect(labelClasses, `the label of ${button.textContent}`).toContain('break-words')
      expect(labelClasses, `the label of ${button.textContent}`).not.toContain('wrap-anywhere')
    }
  })

  it('keeps a tile of 44 px inside every category button, so each one stays easy to tap', async () => {
    const sheet = await renderedSheet()

    for (const button of categoryButtons(sheet)) {
      expect(button.querySelector('.size-11'), `the button of ${button.textContent}`).not.toBeNull()
    }
  })
})
