import { describe, expect, it } from 'vitest'
import { categoryColor, categoryNameKey } from './categoryLooks'

describe('categoryNameKey', () => {
  it('gives the translation key of a built-in category under categories', () => {
    expect(categoryNameKey({ key: 'groceries' })).toBe('categories.groceries')
  })
})

describe('categoryColor', () => {
  it.each(['orange', 'teal', 'gray'])('gives the CSS colour token of %s', (color) => {
    expect(categoryColor({ color })).toBe(`var(--category-${color})`)
  })
})
